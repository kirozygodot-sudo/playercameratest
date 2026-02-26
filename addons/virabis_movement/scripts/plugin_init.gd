@tool
extends EditorPlugin

func _enter_tree() -> void:
	# Plugin yüklendiğinde yapılacak işlemler (Custom Node kayıtları vb.)
	add_custom_type("MovementNode", "Node", preload("../bridge/MovementNode.cs"), preload("res://icon.svg"))
	add_custom_type("MovementBridge", "Node", preload("../bridge/MovementBridge.cs"), preload("res://icon.svg"))

func _exit_tree() -> void:
	remove_custom_type("MovementNode")
	remove_custom_type("MovementBridge")
