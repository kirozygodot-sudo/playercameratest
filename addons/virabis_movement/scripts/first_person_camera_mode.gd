## first_person_camera_mode.gd
## Birinci şahıs kamera modu.
##
## CameraRig altındaki node yapısı:
##   YawPivot (Node3D)
##     └── PitchPivot (Node3D)
##           └── SpringArm3D
##                 └── Camera3D

class_name FirstPersonCameraMode
extends CameraModeBase

# ── Config ────────────────────────────────────────────────────────────────────
var mouse_sensitivity  : float   = 0.12
var pitch_min          : float   = -85.0
var pitch_max          : float   =  85.0
var rotation_smooth    : float   = 25.0  # FPS'te daha tepkisel olmalı

var head_offset        : Vector3 = Vector3(0, 1.7, 0) # Göz hizası
var fov_default        : float   = 85.0

# ── İç durum ──────────────────────────────────────────────────────────────────
var _yaw               : float   = 0.0
var _pitch             : float   = 0.0
var _current_character_speed : float = 0.0

# ── Node refs ─────────────────────────────────────────────────────────────────
var _yaw_pivot    : Node3D      = null
var _pitch_pivot  : Node3D      = null
var _spring_arm   : SpringArm3D = null
var _camera       : Camera3D    = null

# ─────────────────────────────────────────────────────────────────────────────

func _init() -> void:
	mode_name = "FirstPerson"


func enter(rig: Node3D) -> void:
	_yaw_pivot   = rig.get_node("YawPivot")
	_pitch_pivot = rig.get_node("YawPivot/PitchPivot")
	_spring_arm  = rig.get_node("YawPivot/PitchPivot/SpringArm3D")
	_camera      = rig.get_node("YawPivot/PitchPivot/SpringArm3D/Camera3D")

	# FPS'te SpringArm devre dışı bırakılır (uzunluk 0)
	_spring_arm.spring_length = 0.0
	_spring_arm.position      = head_offset
	_camera.fov               = fov_default
	
	# Mevcut rotasyonu devral
	_yaw   = _yaw_pivot.rotation_degrees.y
	_pitch = _pitch_pivot.rotation_degrees.x


func exit(_rig: Node3D) -> void:
	pass


func handle_input(_rig: Node3D, event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		_yaw   -= event.relative.x * mouse_sensitivity
		_pitch -= event.relative.y * mouse_sensitivity
		_pitch  = clampf(_pitch, pitch_min, pitch_max)


func process(_rig: Node3D, delta: float) -> void:
	if _yaw_pivot == null: return

	# FPS'te genellikle daha hızlı bir lerp veya direkt atama tercih edilir
	var t := rotation_smooth * delta
	_yaw_pivot.rotation_degrees.y   = lerp_angle(_yaw_pivot.rotation_degrees.y,   _yaw,   t)
	_pitch_pivot.rotation_degrees.x = lerp_angle(_pitch_pivot.rotation_degrees.x, _pitch, t)


func get_forward(_rig: Node3D) -> Vector3:
	return -_yaw_pivot.global_basis.z if _yaw_pivot else Vector3.FORWARD


func get_right(_rig: Node3D) -> Vector3:
	return _yaw_pivot.global_basis.x if _yaw_pivot else Vector3.RIGHT


func set_character_speed(speed: float) -> void:
	_current_character_speed = speed


func get_debug_label() -> String:
	return "FPS | yaw=%.0f pitch=%.0f" % [_yaw, _pitch]
