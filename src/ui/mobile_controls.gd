class_name MobileControls
extends CanvasLayer


const ActionButtonScript := preload("res://src/ui/mobile_action_button.gd")
const JoystickScript := preload("res://src/ui/virtual_joystick.gd")

var player: PlayerController
var joystick: VirtualJoystick
var buttons: Dictionary = {}


func _ready() -> void:
	layer = 20
	add_to_group("touch_overlay")
	_build_controls()
	get_viewport().size_changed.connect(_layout_controls)
	visible = DisplayServer.is_touchscreen_available()
	_layout_controls()


func bind_player(value: PlayerController) -> void:
	player = value
	joystick.vector_changed.connect(player.set_mobile_move)
	player.style_changed.connect(_on_style_changed)
	_on_style_changed(player.current_style)


func toggle_overlay() -> void:
	visible = not visible
	if not visible and joystick != null:
		joystick.reset()


func _build_controls() -> void:
	joystick = JoystickScript.new()
	joystick.name = "VirtualJoystick"
	add_child(joystick)

	_add_button("attack", "攻", 58.0, Color(0.82, 0.34, 0.24, 0.94))
	_add_button("guard", "守", 52.0, Color(0.36, 0.72, 0.86, 0.94))
	_add_button("dodge", "闪", 49.0, Color(0.48, 0.76, 0.62, 0.94))
	_add_button("skill_1", "技一", 43.0, Color(0.52, 0.90, 1.0, 0.94))
	_add_button("skill_2", "技二", 43.0, Color(0.52, 0.90, 1.0, 0.94))
	_add_button("switch_style", "换式", 35.0, Color(0.68, 0.52, 0.92, 0.94))


func _add_button(action_name: String, label_text: String, radius: float, color: Color) -> void:
	var button: MobileActionButton = ActionButtonScript.new()
	button.name = action_name.capitalize()
	button.configure(label_text, action_name, radius, color)
	buttons[action_name] = button
	add_child(button)


func _layout_controls() -> void:
	if joystick == null or buttons.is_empty():
		return
	var viewport_size := get_viewport().get_visible_rect().size
	joystick.position = Vector2(132.0, viewport_size.y - 132.0)
	buttons["attack"].position = Vector2(viewport_size.x - 112.0, viewport_size.y - 126.0)
	buttons["guard"].position = Vector2(viewport_size.x - 238.0, viewport_size.y - 118.0)
	buttons["dodge"].position = Vector2(viewport_size.x - 174.0, viewport_size.y - 232.0)
	buttons["skill_1"].position = Vector2(viewport_size.x - 330.0, viewport_size.y - 198.0)
	buttons["skill_2"].position = Vector2(viewport_size.x - 286.0, viewport_size.y - 300.0)
	buttons["switch_style"].position = Vector2(viewport_size.x - 72.0, 76.0)


func _on_style_changed(style) -> void:
	if buttons.is_empty() or style == null:
		return
	buttons["skill_1"].set_label_text(style.skill_1_name)
	buttons["skill_2"].set_label_text(style.skill_2_name)
