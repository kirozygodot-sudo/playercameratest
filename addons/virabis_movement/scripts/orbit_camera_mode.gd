## orbit_camera_mode.gd
## Third-person orbit kamera. Virabis varsayılan modu.
## Over-the-shoulder + aim destekli.
##
## CameraRig altındaki node yapısı:
##   YawPivot (Node3D)
##     └── PitchPivot (Node3D)
##           └── SpringArm3D
##                 └── Camera3D

class_name OrbitCameraMode
extends CameraModeBase

# ── Config ────────────────────────────────────────────────────────────────────
var mouse_sensitivity : float   = 0.15
var aim_sensitivity_mult : float = 0.5   # Nişan alırken hassasiyet çarpanı
var pitch_min         : float   = -60.0
var pitch_max         : float   =  60.0
var rotation_smooth   : float   = 12.0

var default_distance  : float   = 4.0
var min_distance      : float   = 2.5
var max_distance      : float   = 6.0
var zoom_step         : float   = 0.5
var zoom_smooth       : float   = 8.0

# ── Sprint FOV Kick ───────────────────────────────────────────────────────────
var fov_sprint        : float   = 85.0   # Sprint'te geniş FOV
var fov_min_speed     : float   = 5.0    # Bu hızın altında FOV değişmez
var fov_max_speed     : float   = 20.0   # Bu hızın üstünde max FOV
var fov_min_value     : float   = 75.0   # Min hızda FOV değeri
var fov_max_value     : float   = 90.0   # Max hızda FOV değeri
var fov_speed_smooth  : float   = 4.0    # Hıza bağlı FOV geçiş hızı
var sprint_fov_smooth : float   = 6.0    # FOV geçiş hızı
var _is_sprinting     : bool    = false
var _current_character_speed : float = 0.0

var offset_normal     : Vector3 = Vector3(0.6, 1.6, 0.0)  # Over-the-shoulder
var offset_aiming     : Vector3 = Vector3(0.2, 1.5, 0.0)  # Nişan → merkeze
var fov_normal        : float   = 75.0
var fov_aiming        : float   = 60.0
var aim_distance      : float   = 2.8
var aim_smooth        : float   = 8.0

# ── İç durum ──────────────────────────────────────────────────────────────────
var _yaw              : float   = 0.0
var _pitch            : float   = 0.0
var _target_dist      : float
var _current_dist     : float
var _is_aiming        : bool    = false
var _target_fov       : float
var _target_offset    : Vector3

# ── Camera Shake ─────────────────────────────────────────────────────────────
var _shake_amount     : float   = 0.0
var _shake_decay      : float   = 5.0
var _shake_frequency  : float   = 25.0 # Hz
var _shake_amplitude  : float   = 0.1  # Max offset
var _shake_duration   : float   = 0.2  # Saniye
var _shake_timer      : float   = 0.0
var _noise            : FastNoiseLite = FastNoiseLite.new()

# ── Node refs (enter'da çözülür) ──────────────────────────────────────────────
var _yaw_pivot   : Node3D      = null
var _pitch_pivot : Node3D      = null
var _spring_arm  : SpringArm3D = null
var _camera      : Camera3D    = null

# ─────────────────────────────────────────────────────────────────────────────

func _init() -> void:
	mode_name = "Orbit"
	_noise.seed = randi()
	_noise.noise_type = FastNoiseLite.TYPE_PERLIN
	_noise.frequency = _shake_frequency


func enter(rig: Node3D) -> void:
	_yaw_pivot   = rig.get_node("YawPivot")
	_pitch_pivot = rig.get_node("YawPivot/PitchPivot")
	_spring_arm  = rig.get_node("YawPivot/PitchPivot/SpringArm3D")
	_camera      = rig.get_node("YawPivot/PitchPivot/SpringArm3D/Camera3D")

	_target_dist    = default_distance
	_current_dist   = default_distance
	_target_fov     = fov_normal
	_target_offset  = offset_normal

	_camera.fov               = fov_normal
	_spring_arm.spring_length = default_distance
	_spring_arm.position      = offset_normal


func exit(_rig: Node3D) -> void:
	pass  # Temizlenecek state yok


func handle_input(_rig: Node3D, event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		var sens := mouse_sensitivity * (aim_sensitivity_mult if _is_aiming else 1.0)
		_yaw   -= event.relative.x * sens
		_pitch -= event.relative.y * sens
		_pitch  = clampf(_pitch, pitch_min, pitch_max)

	if event is InputEventMouseButton and event.pressed:
		match event.button_index:
			MOUSE_BUTTON_WHEEL_UP:
				_target_dist = clampf(_target_dist - zoom_step, min_distance, max_distance)
			MOUSE_BUTTON_WHEEL_DOWN:
				_target_dist = clampf(_target_dist + zoom_step, min_distance, max_distance)

	# Aim mode — sağ tık
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_RIGHT:
		_set_aiming(event.pressed)


func process(_rig: Node3D, delta: float) -> void:
	if _yaw_pivot == null: return

	var t_rot  := rotation_smooth * delta
	var t_aim  := aim_smooth * delta
	var t_zoom := zoom_smooth * delta

	_yaw_pivot.rotation_degrees.y   = lerp_angle(_yaw_pivot.rotation_degrees.y,   _yaw,   t_rot)
	_pitch_pivot.rotation_degrees.x = lerp_angle(_pitch_pivot.rotation_degrees.x, _pitch, t_rot)

	_current_dist             = lerp(_current_dist, _target_dist, t_zoom)
	_spring_arm.spring_length = _current_dist

	_camera.fov          = lerp(_camera.fov,          _target_fov,    t_aim)
	_spring_arm.position = lerp(_spring_arm.position, _target_offset, t_aim)

	# Sprint FOV Kick uygula
	var target_fov_by_speed := fov_normal
	if not _is_aiming:
		var speed_ratio := clampf((_current_character_speed - fov_min_speed) / (fov_max_speed - fov_min_speed), 0.0, 1.0)
		target_fov_by_speed = lerpf(fov_min_value, fov_max_value, speed_ratio)

	var final_target_fov := target_fov_by_speed
	if _is_sprinting:
		final_target_fov = fov_sprint

	_camera.fov = lerp(_camera.fov, final_target_fov, fov_speed_smooth * delta)

	# Camera Shake uygula
	if _shake_amount > 0.0:
		var shake_offset := Vector3(
			randf_range(-_shake_amount, _shake_amount),
			randf_range(-_shake_amount, _shake_amount),
			randf_range(-_shake_amount, _shake_amount)
		)
		_camera.position = shake_offset
		_shake_amount = max(0.0, _shake_amount - _shake_decay * delta)
	else:
		_camera.position = Vector3.ZERO


func get_forward(_rig: Node3D) -> Vector3:
	return -_yaw_pivot.global_basis.z if _yaw_pivot else Vector3.FORWARD


func get_right(_rig: Node3D) -> Vector3:
	return _yaw_pivot.global_basis.x if _yaw_pivot else Vector3.RIGHT


func get_debug_label() -> String:
	return "Orbit | aim=%s | dist=%.1f | yaw=%.0f pitch=%.0f" % [
		_is_aiming, _current_dist, _yaw, _pitch
	]


func is_aiming() -> bool:
	return _is_aiming


func set_sensitivity(value: float) -> void:
	mouse_sensitivity = value


## Kamera titremesi ekle (jump, landing, damage vb. için)
func add_shake(amount: float) -> void:
	_shake_amount += amount


## Sprint durumunu ayarla (FOV kick için)
func set_character_speed(speed: float) -> void:
	_current_character_speed = speed

func set_sprinting(value: bool) -> void:
	_is_sprinting = value


# ── Private ───────────────────────────────────────────────────────────────────

func _set_aiming(value: bool) -> void:
	_is_aiming     = value
	_target_fov    = fov_aiming    if value else fov_normal
	_target_offset = offset_aiming if value else offset_normal
	_target_dist   = aim_distance  if value else default_distance
