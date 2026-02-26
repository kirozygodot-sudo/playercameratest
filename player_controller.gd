## player_controller.gd
## Orkestratör. Camera + Movement + Input bağlar.
##
## SORUMLULUĞU:
##   - Input okumak
##   - Camera yönünden movement direction hesaplamak
##   - MovementNode API'sine iletmek
##   - Mount / dismount yönetmek
##
## BURAYA GELMEYENLer: animasyon, VFX, SFX, silah, UI — bunlar başka node'larda.
##
## SAHNE YAPISI:
##   Player (CharacterBody3D)
##   ├── PlayerController (Node)      ← bu script
##   ├── MovementNode (Node)          ← MovementNode.cs (C#)
##   └── CameraRig (Node3D)           ← camera_controller.gd

class_name PlayerController
extends Node

@onready var movement : MovementNode    = $"../MovementNode"
@onready var camera   : CameraController = $"../CameraRig"

signal jumped
signal landed

# ─────────────────────────────────────────────────────────────────────────────

func _process(_delta: float) -> void:
	movement.SetSprinting(Input.is_action_pressed("sprint"))

	# Uçuş toggle — geliştirme test tuşu. İleride ability sistemi tetikler.
	if Input.is_action_just_pressed("toggle_fly"):
		movement.SetFlying(!movement.IsFlying())


func _physics_process(_delta: float) -> void:
	_update_movement_direction()
	_handle_jump()


# ─────────────────────────────────────────────────────────────────────────────
# MOVEMENT INPUT
# ─────────────────────────────────────────────────────────────────────────────

func _update_movement_direction() -> void:
	var raw := Vector2(
		Input.get_axis("move_left",    "move_right"),
		Input.get_axis("move_forward", "move_back")
	)

	if raw.length_squared() < 0.01:
		movement.SetInputDirection(Vector3.ZERO)
		return

	var forward := camera.get_forward()
	var right   := camera.get_right()
	forward.y    = 0.0
	right.y      = 0.0

	var dir := forward * -raw.y + right * raw.x
	if dir.length_squared() > 0.0001:
		dir = dir.normalized()

	movement.SetInputDirection(dir)


func _handle_jump() -> void:
	if Input.is_action_just_pressed("jump"):
		movement.RequestJump()
		jumped.emit()


# ─────────────────────────────────────────────────────────────────────────────
# MOUNT / DISMOUNT
# ─────────────────────────────────────────────────────────────────────────────

## Araca / ata / ejderhaya bin.
## cam_mode: VehicleCameraMode, DragonCameraMode vb. — dışarıdan oluştur.
func mount(mount_node: Node3D, cam_mode: CameraModeBase) -> void:
	movement.SetEnabled(false)
	camera.switch_mode(cam_mode)
	# mount_node.take_control(self)  — mount sistemi implemente edilince açılır


## Mount'tan in, orbit kameraya geri dön.
func dismount() -> void:
	movement.SetEnabled(true)
	camera.switch_mode(OrbitCameraMode.new())


# ─────────────────────────────────────────────────────────────────────────────
# DEBUG
# ─────────────────────────────────────────────────────────────────────────────

func get_debug_info() -> Dictionary:
	var info := movement.GetDebugInfo()
	info.merge(camera.get_debug_info())
	return info
