## camera_controller.gd
## Virabis Camera Controller — Gameplay Layer
##
## TEK SORUMLULUĞU: Aktif CameraModeBase'i çalıştırmak.
## Mod içini bilmez, umursamaz. Sadece enter/exit/process/handle_input çağırır.

class_name CameraController
extends Node3D

## SpringArm çarpışma katmanı ayarları
@export var collision_layer_mask : int = 1  # Varsayılan: layer 1 (world/terrain)
@export var collision_margin : float = 0.5   # Çarpışma mesafe payı

# ── Lock-on Config ────────────────────────────────────────────────────────────
@export_group("Lock-on Settings")
@export var lock_on_radius : float = 20.0
@export var lock_on_speed  : float = 10.0
@export var lock_on_group  : String = "enemies"

var _active_mode : CameraModeBase = null
var _target_enemy : Node3D = null
var _is_locked_on : bool = false

signal mode_changed(mode_name: String)
signal lock_on_changed(is_locked: bool, target: Node3D)

# ─────────────────────────────────────────────────────────────────────────────

func _ready() -> void:
	set_as_top_level(true)  # Player rotation'ından bağımsız döner
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	switch_mode(OrbitCameraMode.new())
	
	# SpringArm çarpışma katmanı ayarları
	var spring_arm := get_node_or_null("YawPivot/PitchPivot/SpringArm3D")
	if spring_arm:
		spring_arm.collision_mask = collision_layer_mask
		spring_arm.margin = collision_margin


func _process(delta: float) -> void:
	# Pozisyonu her frame player ile senkronize et
	if get_parent():
		global_position = get_parent().global_position

	# Lock-on mantığı
	if _is_locked_on and is_instance_valid(_target_enemy):
		_handle_lock_on(delta)

	if _active_mode:
		_active_mode.process(self, delta)


func _unhandled_input(event: InputEvent) -> void:
	# Debug: Escape ile mouse toggle
	if event.is_action_pressed("ui_cancel"):
		var mode := Input.get_mouse_mode()
		Input.set_mouse_mode(
			Input.MOUSE_MODE_VISIBLE if mode == Input.MOUSE_MODE_CAPTURED
			else Input.MOUSE_MODE_CAPTURED
		)
		return

	# Lock-on Toggle (Orta tık veya özel tuş)
	if event.is_action_pressed("lock_on"):
		toggle_lock_on()

	# Kamera modu değiştirme (Tab tuşu örneği)
	if event.is_action_pressed("switch_camera_mode"):
		if _active_mode is OrbitCameraMode:
			switch_mode(FirstPersonCameraMode.new())
		else:
			switch_mode(OrbitCameraMode.new())

	if Input.get_mouse_mode() != Input.MOUSE_MODE_CAPTURED:
		return

	if _active_mode:
		_active_mode.handle_input(self, event)


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
				# Görüş hattı kontrolü (Opsiyonel Raycast eklenebilir)
				min_dist = dist
				closest = enemy
	return closest


func _handle_lock_on(delta: float) -> void:
	var yaw_pivot = get_node("YawPivot")
	var pitch_pivot = get_node("YawPivot/PitchPivot")
	
	if not yaw_pivot or not pitch_pivot: return
	
	# Hedefe doğru bakış vektörü
	var target_pos = _target_enemy.global_position
	# Göz hizasına bak (varsayılan +1.5m)
	target_pos.y += 1.5
	
	var look_dir = (target_pos - global_position).normalized()
	
	# Hedef rotasyonları hesapla
	var target_yaw = rad_to_deg(atan2(-look_dir.x, -look_dir.z))
	var target_pitch = rad_to_deg(asin(look_dir.y))
	
	# Kamera modundaki iç değişkenleri güncelle ki input kesilince kamera sapmasın
	if _active_mode.has_method("set_rotation_degrees"):
		_active_mode.set_rotation_degrees(target_yaw, -target_pitch)
	elif "_yaw" in _active_mode:
		_active_mode._yaw = lerp_angle(deg_to_rad(_active_mode._yaw), deg_to_rad(target_yaw), lock_on_speed * delta)
		_active_mode._yaw = rad_to_deg(_active_mode._yaw)
		_active_mode._pitch = lerp_angle(deg_to_rad(_active_mode._pitch), deg_to_rad(-target_pitch), lock_on_speed * delta)
		_active_mode._pitch = rad_to_deg(_active_mode._pitch)

# ─────────────────────────────────────────────────────────────────────────────
# PUBLIC API
# ─────────────────────────────────────────────────────────────────────────────

## Kamera modunu değiştir. Eski mod exit(), yeni mod enter() alır.
func switch_mode(new_mode: CameraModeBase) -> void:
	if _active_mode: _active_mode.exit(self)
	_active_mode = new_mode
	if _active_mode:
		_active_mode.enter(self)
		mode_changed.emit(_active_mode.mode_name)

## PlayerController bu vektörü input hesabında kullanır.
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
		"mouse_captured": Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED
	}
