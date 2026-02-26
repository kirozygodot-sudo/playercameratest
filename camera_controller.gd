## camera_controller.gd
## Virabis Camera Controller — Gameplay Layer
##
## TEK SORUMLULUĞU: Aktif CameraModeBase'i çalıştırmak.
## Mod içini bilmez, umursamaz. Sadece enter/exit/process/handle_input çağırır.
##
## Sahne yapısı (Player altına):
##   Player (CharacterBody3D)
##   └── CameraRig (Node3D)         ← bu script
##       └── YawPivot (Node3D)
##           └── PitchPivot (Node3D)
##               └── SpringArm3D
##                   └── Camera3D

class_name CameraController
extends Node3D

## SpringArm çarpışma katmanı ayarları
@export var collision_layer_mask : int = 1  # Varsayılan: layer 1 (world/terrain)
@export var collision_margin : float = 0.5   # Çarpışma mesafe payı

var _active_mode : CameraModeBase = null

signal mode_changed(mode_name: String)

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

	if Input.get_mouse_mode() != Input.MOUSE_MODE_CAPTURED:
		return

	if _active_mode:
		_active_mode.handle_input(self, event)

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

func set_sensitivity(value: float) -> void:
	if _active_mode is OrbitCameraMode:
		(_active_mode as OrbitCameraMode).set_sensitivity(value)

func get_mode_name() -> String:
	return _active_mode.mode_name if _active_mode else "None"

func get_debug_info() -> Dictionary:
	return {
		"mode":           get_mode_name(),
		"detail":         _active_mode.get_debug_label() if _active_mode else "",
		"is_aiming":      is_aiming(),
		"mouse_captured": Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED
	}
