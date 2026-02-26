## debug_panel.gd
## Tek bir noktadan tüm debug bilgilerini yöneten panel.
## PlayerController, CameraController, MovementNode'dan bilgi toplar.

class_name DebugPanel
extends Control

@export var player_controller : PlayerController
@export var show_debug : bool = true:
	set(value):
		show_debug = value
		visible = value

@onready var _label := Label.new()

func _ready() -> void:
	add_child(_label)
	_label.add_theme_font_size_override("font_size", 14)
	visible = show_debug

func _process(_delta: float) -> void:
	if not show_debug or not player_controller:
		return
	
	var info := player_controller.get_debug_info()
	var text := ""
	
	for key in info.keys():
		text += "%s: %s\n" % [key, info[key]]
	
	_label.text = text
