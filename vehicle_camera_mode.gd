## vehicle_camera_mode.gd
## Araç / At / Tank / Helikopter kamerası için stub.
## Şu an logic boş — iskelet hazır, implemente edilmeyi bekliyor.
##
## Kullanım:
##   var cam = VehicleCameraMode.new()
##   cam.follow_target = $CarNode
##   camera_controller.switch_mode(cam)

class_name VehicleCameraMode
extends CameraModeBase

var follow_target    : Node3D = null
var follow_distance  : float  = 6.0
var follow_height    : float  = 2.5
var rotation_smooth  : float  = 6.0
var mouse_sensitivity: float  = 0.10
var fov              : float  = 80.0

var _yaw  : float = 0.0
var _pitch: float = -10.0

func _init() -> void:
	mode_name = "Vehicle"


func enter(rig: Node3D) -> void:
	var cam    = rig.get_node_or_null("YawPivot/PitchPivot/SpringArm3D/Camera3D")
	var spring = rig.get_node_or_null("YawPivot/PitchPivot/SpringArm3D")
	if cam:    cam.fov              = fov
	if spring: spring.spring_length = follow_distance


func handle_input(_rig: Node3D, event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		_yaw   -= event.relative.x * mouse_sensitivity
		_pitch  = clampf(_pitch - event.relative.y * mouse_sensitivity, -30.0, 15.0)


func process(_rig: Node3D, _delta: float) -> void:
	pass  # TODO: araç takip logic'i


func get_forward(_rig: Node3D) -> Vector3:
	if follow_target: return -follow_target.global_basis.z
	return Vector3.FORWARD


func get_debug_label() -> String:
	return "Vehicle | target=%s" % (follow_target.name if follow_target else "none")
