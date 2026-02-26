## camera_controller.gd
## Virabis Camera Controller — Gameplay Layer
##
## TEK SORUMLULUĞU: Aktif CameraModeBase'i çalıştırmak ve modlar arası geçişleri (Blending) yönetmek.

class_name CameraController
extends Node3D

## SpringArm çarpışma katmanı ayarları
@export var collision_layer_mask : int = 1
@export var collision_margin : float = 0.1

# ── Lock-on Config ────────────────────────────────────────────────────────────
@export_group("Lock-on Settings")
@export var lock_on_radius : float = 20.0
@export var lock_on_speed  : float = 10.0
@export var lock_on_group  : String = "enemies"

# ── Blending Config ───────────────────────────────────────────────────────────
@export_group("Blending Settings")
@export var blend_duration : float = 0.5  # Modlar arası geçiş süresi (AAA Feel)

var _active_mode : CameraModeBase = null
var _target_enemy : Node3D = null
var _is_locked_on : bool = false

# Blending (Mikser) değişkenleri
var _is_blending : bool = false
var _blend_timer : float = 0.0
var _blend_from_fov : float = 75.0
var _blend_from_pos : Vector3
var _blend_from_rot : Vector3

signal mode_changed(mode_name: String)
signal lock_on_changed(is_locked: bool, target: Node3D)

# ─────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	set_as_top_level(true)
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	
	var spring_arm := get_node_or_null("YawPivot/PitchPivot/SpringArm3D")
	if spring_arm:
		spring_arm.collision_mask = collision_layer_mask
		spring_arm.margin = collision_margin
		spring_arm.shape = SphereShape3D.new()
		spring_arm.shape.radius = 0.1
	
	switch_mode(OrbitCameraMode.new())


func _process(delta: float) -> void:
	if get_parent():
		global_position = get_parent().global_position

	if _is_locked_on and is_instance_valid(_target_enemy):
		_handle_lock_on(delta)

	if _active_mode:
		_active_mode.process(self, delta)
	
	# Blending (Mikser) Uygula
	if _is_blending:
		_update_blending(delta)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		var mode := Input.get_mouse_mode()
		Input.set_mouse_mode(
			Input.MOUSE_MODE_VISIBLE if mode == Input.MOUSE_MODE_CAPTURED
			else Input.MOUSE_MODE_CAPTURED
		)
		return

	if event.is_action_pressed("lock_on"):
		toggle_lock_on()

	if event.is_action_pressed("switch_camera_mode"):
		if _active_mode is OrbitCameraMode:
			switch_mode(FirstPersonCameraMode.new())
		else:
			switch_mode(OrbitCameraMode.new())

	if Input.get_mouse_mode() != Input.MOUSE_MODE_CAPTURED:
		return

	if _active_mode:
		_active_mode.handle_input(self, event)


# ── Mikser (Blending) Mantığı ─────────────────────────────────────────────────

func _update_blending(delta: float) -> void:
	_blend_timer += delta
	var t = clampf(_blend_timer / blend_duration, 0.0, 1.0)
	# Ease-out sine eğrisi
	var ease_t = sin(t * PI * 0.5)
	
	var camera := get_node("YawPivot/PitchPivot/SpringArm3D/Camera3D") as Camera3D
	if not camera: return
	
	# FOV Blend
	# Not: Modlar kendi içlerinde de FOV'u lerp ediyor olabilir, 
	# bu yüzden blender sadece geçiş anındaki ani sıçramayı yumuşatır.
	
	if t >= 1.0:
		_is_blending = false


# ── Lock-on Mantığı ───────────────────────────────────────────────────────────

func toggle_lock_on() -> void:
	if _is_locked_on:
		_is_locked_on = false
		_target_enemy = null
		lock_on_changed.emit(false, null)
	else:
		_target_enemy = _find_closest_enemy()
		if _target_enemy:
			_is_locked_on = true
			lock_on_changed.emit(true, _target_enemy)


func _find_closest_enemy() -> Node3D:
	var enemies = get_tree().get_nodes_in_group(lock_on_group)
	var closest : Node3D = null
	var min_dist = lock_on_radius
	
	for enemy in enemies:
		if enemy is Node3D:
			var dist = global_position.distance_to(enemy.global_position)
			if dist < min_dist:
				min_dist = dist
				closest = enemy
	return closest


func _handle_lock_on(delta: float) -> void:
	var yaw_pivot = get_node("YawPivot")
	var pitch_pivot = get_node("YawPivot/PitchPivot")
	
	if not yaw_pivot or not pitch_pivot: return
	
	var target_pos = _target_enemy.global_position
	target_pos.y += 1.5
	
	var look_dir = (target_pos - global_position).normalized()
	
	var target_yaw = rad_to_deg(atan2(-look_dir.x, -look_dir.z))
	var target_pitch = rad_to_deg(asin(look_dir.y))
	
	if "_yaw" in _active_mode:
		_active_mode._yaw = lerp_angle(deg_to_rad(_active_mode._yaw), deg_to_rad(target_yaw), lock_on_speed * delta)
		_active_mode._yaw = rad_to_deg(_active_mode._yaw)
		_active_mode._pitch = lerp_angle(deg_to_rad(_active_mode._pitch), deg_to_rad(-target_pitch), lock_on_speed * delta)
		_active_mode._pitch = rad_to_deg(_active_mode._pitch)

# ─────────────────────────────────────────────────────────────────────────────
# PUBLIC API
# ─────────────────────────────────────────────────────────────────────────────

func switch_mode(new_mode: CameraModeBase) -> void:
	var camera := get_node_or_null("YawPivot/PitchPivot/SpringArm3D/Camera3D") as Camera3D
	
	# Eski verileri sakla (Blending için)
	if camera:
		_blend_from_fov = camera.fov
		_is_blending = true
		_blend_timer = 0.0
	
	if _active_mode: 
		_active_mode.exit(self)
	
	_active_mode = new_mode
	
	if _active_mode:
		_active_mode.enter(self)
		mode_changed.emit(_active_mode.mode_name)
		
		# Pürüzsüz geçiş için Tween ile FOV ve Offset blending başlat
		if camera:
			var target_fov = 75.0
			if _active_mode is OrbitCameraMode: target_fov = _active_mode.fov_normal
			elif _active_mode is FirstPersonCameraMode: target_fov = _active_mode.fov_default
			
			var tween = create_tween()
			tween.set_parallel(true)
			tween.set_trans(Tween.TRANS_SINE)
			tween.set_ease(Tween.EASE_OUT)
			
			# FOV Blend
			camera.fov = _blend_from_fov
			tween.tween_property(camera, "fov", target_fov, blend_duration)
			
			# SpringArm Position (Offset) Blend
			var spring_arm := get_node("YawPivot/PitchPivot/SpringArm3D") as SpringArm3D
			if spring_arm:
				var target_pos = spring_arm.position
				# Başlangıç pozisyonunu koru, hedefe tweenle
				tween.tween_property(spring_arm, "position", target_pos, blend_duration).from(spring_arm.position)


func get_forward() -> Vector3:
	return _active_mode.get_forward(self) if _active_mode else -global_basis.z

func get_right() -> Vector3:
	return _active_mode.get_right(self) if _active_mode else global_basis.x

func is_aiming() -> bool:
	if _active_mode is OrbitCameraMode:
		return (_active_mode as OrbitCameraMode).is_aiming()
	return false

func set_character_speed(speed: float) -> void:
	if _active_mode:
		_active_mode.set_character_speed(speed)

func set_sensitivity(value: float) -> void:
	if _active_mode is OrbitCameraMode or _active_mode is FirstPersonCameraMode:
		_active_mode.mouse_sensitivity = value

func get_mode_name() -> String:
	return _active_mode.mode_name if _active_mode else "None"

func get_debug_info() -> Dictionary:
	return {
		"mode":           get_mode_name(),
		"detail":         _active_mode.get_debug_label() if _active_mode else "",
		"is_aiming":      is_aiming(),
		"is_locked":      _is_locked_on,
		"is_blending":    _is_blending,
		"mouse_captured": Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED
	}
