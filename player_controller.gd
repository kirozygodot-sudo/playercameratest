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

@export var input_dead_zone : float = 0.1

@export var sprint_mode : SprintMode = SprintMode.HOLD
@export var double_tap_time : float = 0.3

enum SprintMode { HOLD, TOGGLE, DOUBLE_TAP }

var _sprint_toggle_state := false
var _last_forward_time := 0.0

@onready var movement : Node = $"../MovementNode"
@onready var camera   : Node = $"../CameraRig"
@onready var _character : CharacterBody3D = get_parent()

signal jumped
signal landed
signal slide_started
signal slide_attacked
signal grapple_fired
signal grapple_released
signal momentum_transferred

# ─────────────────────────────────────────────────────────────────────────────

func _process(delta: float) -> void:
	var is_sprinting := _get_sprint_input(delta)
	movement.call("SetSprinting", is_sprinting)
	
	# Sprint FOV Kick için kameraya bildir
	if camera._active_mode is OrbitCameraMode:
		(camera._active_mode as OrbitCameraMode).set_sprinting(is_sprinting)

	# Uçuş toggle — geliştirme test tuşu. İleride ability sistemi tetikler.
	if Input.is_action_just_pressed("toggle_fly"):
		movement.call("SetFlying", !movement.call("IsFlying"))


func _get_sprint_input(delta: float) -> bool:
	match sprint_mode:
		SprintMode.HOLD:
			return Input.is_action_pressed("sprint")
		SprintMode.TOGGLE:
			if Input.is_action_just_pressed("sprint"):
				_sprint_toggle_state = !_sprint_toggle_state
			return _sprint_toggle_state
		SprintMode.DOUBLE_TAP:
			if Input.is_action_just_pressed("move_forward"):
				var now := Time.get_unix_time_from_system()
				if now - _last_forward_time < double_tap_time:
					_sprint_toggle_state = !_sprint_toggle_state
				_last_forward_time = now
			return _sprint_toggle_state
	return false


func _physics_process(_delta: float) -> void:
	_update_movement_direction()
	_handle_jump()
	_check_landing()
	_handle_slide()
	_handle_crouch()
	_handle_wall_jump()
	_update_grapple()


# ─────────────────────────────────────────────────────────────────────────────
# LANDING & SHAKE
# ─────────────────────────────────────────────────────────────────────────────

var _was_on_floor := true

func _check_landing() -> void:
	var is_on_floor : bool = _character.is_on_floor()
	if not _was_on_floor and is_on_floor:
		# Yere iniş titremesi
		if camera._active_mode is OrbitCameraMode:
			(camera._active_mode as OrbitCameraMode).add_shake(0.15)
			landed.emit()
	_was_on_floor = is_on_floor


# ─────────────────────────────────────────────────────────────────────────────
# MOVEMENT INPUT
# ─────────────────────────────────────────────────────────────────────────────

func _update_movement_direction() -> void:
	var raw := Vector2(
		Input.get_axis("move_left",    "move_right"),
		Input.get_axis("move_forward", "move_back")
	)

	if raw.length_squared() < input_dead_zone * input_dead_zone:
		movement.call("SetInputDirection", Vector3.ZERO)
		return

	var forward : Vector3 = camera.get_forward()
	var right   : Vector3 = camera.get_right()
	forward.y    = 0.0
	right.y      = 0.0

	var dir : Vector3 = forward * -raw.y + right * raw.x
	if dir.length_squared() > 0.0001:
		dir = dir.normalized()

	movement.call("SetInputDirection", dir)


func _handle_jump() -> void:
	if Input.is_action_just_pressed("jump"):
		movement.call("RequestJump")
		jumped.emit()
		# Zıplama titremesi
		if camera._active_mode is OrbitCameraMode:
			(camera._active_mode as OrbitCameraMode).add_shake(0.08)
	
	# Ground Slam - Ctrl tuşu ile hızlı düşüş
	if Input.is_action_just_pressed("crouch") and not _character.is_on_floor():
		movement.call("RequestGroundSlam")
		# Ground Slam titremesi
		if camera._active_mode is OrbitCameraMode:
			(camera._active_mode as OrbitCameraMode).add_shake(0.05)


# ─────────────────────────────────────────────────────────────────────────────
# SLIDE MECHANIC
# ─────────────────────────────────────────────────────────────────────────────

var _is_sliding := false
var _slide_timer := 0.0

func _handle_slide() -> void:
	# Slide başlat (çömelme + sprint)
	if Input.is_action_just_pressed("crouch") and _get_sprint_input(get_physics_process_delta_time()) and not _is_sliding:
		_is_sliding = true
		_slide_timer = 2.0  # Max slide süresi
		slide_started.emit()
		movement.call("ApplySlide", 3.0, 2.0, 0.6, 2.0, 1.5)
		
		# Slide kamera shake
		if camera._active_mode is OrbitCameraMode:
			(camera._active_mode as OrbitCameraMode).add_shake(0.1)
	
	# Slide melee attack
	if _is_sliding and Input.is_action_just_pressed("attack"):
		slide_attacked.emit()
		# Attack boost
		if camera._active_mode is OrbitCameraMode:
			(camera._active_mode as OrbitCameraMode).add_shake(0.12)
	
	# Slide kontrolü
	if _is_sliding:
		_slide_timer -= get_physics_process_delta_time()
		if _slide_timer <= 0.0 or not Input.is_action_pressed("crouch"):
			_is_sliding = false


# ─────────────────────────────────────────────────────────────────────────────
# CROUCH JUMP MECHANIC
# ─────────────────────────────────────────────────────────────────────────────

var _is_crouching := false
var _crouch_charge := 0.0

func _handle_crouch() -> void:
	# Çömelme charge
	if Input.is_action_pressed("crouch"):
		_is_crouching = true
		_crouch_charge += get_physics_process_delta_time()
		_crouch_charge = min(_crouch_charge, 0.3)  # Max charge
	else:
		if _is_crouching:
			# Çömelme bırakıldı
			_is_crouching = false
			_crouch_charge = 0.0


# ─────────────────────────────────────────────────────────────────────────────
# WALL JUMP MECHANIC
# ─────────────────────────────────────────────────────────────────────────────

var _wall_jump_cooldown := 0.0

func _handle_wall_jump() -> void:
	if _wall_jump_cooldown > 0.0:
		_wall_jump_cooldown -= get_physics_process_delta_time()
		return
	
	# Wall detection ve jump (SpaceRay kullanarak duvar algılama)
	if Input.is_action_just_pressed("jump") and not _character.is_on_floor():
		var space_state : PhysicsDirectSpaceState3D = _character.get_world_3d().direct_space_state
		var query := PhysicsRayQueryParameters3D.new()
		query.from = _character.global_position
		query.to = _character.global_position + _character.velocity.normalized() * 1.5
		query.exclude = [_character]
		
		var result : Dictionary = space_state.intersect_ray(query)
		if result:
			_wall_jump_cooldown = 0.3
			momentum_transferred.emit()
			# Wall jump shake
			if camera._active_mode is OrbitCameraMode:
				(camera._active_mode as OrbitCameraMode).add_shake(0.06)


# ─────────────────────────────────────────────────────────────────────────────
# GRAPPLE HOOK MECHANIC
# ─────────────────────────────────────────────────────────────────────────────

var _grapple_active := false

func _update_grapple() -> void:
	# Grapple fire
	if Input.is_action_just_pressed("grapple") and not _grapple_active:
		var space_state : PhysicsDirectSpaceState3D = _character.get_world_3d().direct_space_state
		var query := PhysicsRayQueryParameters3D.new()
		query.from = camera.global_position
		query.to = camera.global_position + camera.call("get_forward") * 30.0
		query.exclude = [_character]
		
		var result : Dictionary = space_state.intersect_ray(query)
		if result:
			_grapple_active = true
			grapple_fired.emit()
			# Grapple start shake
			if camera._active_mode is OrbitCameraMode:
				(camera._active_mode as OrbitCameraMode).add_shake(0.05)
	
	# Grapple release
	if _grapple_active and Input.is_action_just_released("grapple"):
		_grapple_active = false
		grapple_released.emit()
		# Launch boost check
		if camera._active_mode is OrbitCameraMode:
			(camera._active_mode as OrbitCameraMode).add_shake(0.08)


# ─────────────────────────────────────────────────────────────────────────────
# MOUNT / DISMOUNT
# ─────────────────────────────────────────────────────────────────────────────

## Araca / ata / ejderhaya bin.
## cam_mode: VehicleCameraMode, DragonCameraMode vb. — dışarıdan oluştur.
func mount(mount_node: Node3D, cam_mode) -> void:
	movement.call("SetEnabled", false)
	camera.call("switch_mode", cam_mode)
	# mount_node.take_control(self)  — mount sistemi implemente edilince açılır


## Mount'tan in, orbit kameraya geri dön.
func dismount() -> void:
	movement.call("SetEnabled", true)
	camera.call("switch_mode", null)


# ─────────────────────────────────────────────────────────────────────────────
# DEBUG
# ─────────────────────────────────────────────────────────────────────────────

func get_debug_info() -> Dictionary:
	var info : Dictionary = movement.call("GetDebugInfo")
	info.merge(camera.call("get_debug_info"))
	return info
