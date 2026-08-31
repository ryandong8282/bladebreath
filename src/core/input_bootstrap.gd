class_name InputBootstrap
extends RefCounted


const KEY_BINDINGS := {
	"move_left": [KEY_A, KEY_LEFT],
	"move_right": [KEY_D, KEY_RIGHT],
	"move_up": [KEY_W, KEY_UP],
	"move_down": [KEY_S, KEY_DOWN],
	"attack": [KEY_J],
	"guard": [KEY_K],
	"dodge": [KEY_SPACE, KEY_SHIFT, KEY_L],
	"skill_1": [KEY_U, KEY_Q],
	"skill_2": [KEY_I, KEY_E],
	"switch_style": [KEY_TAB],
	"restart_demo": [KEY_R, KEY_BACKSPACE],
	"toggle_touch_overlay": [KEY_F2],
}


static func ensure_actions() -> void:
	for action_name in KEY_BINDINGS.keys():
		_ensure_action(action_name)
		for keycode in KEY_BINDINGS[action_name]:
			_add_key(action_name, int(keycode))

	_add_mouse("attack", MOUSE_BUTTON_LEFT)
	_add_mouse("guard", MOUSE_BUTTON_RIGHT)
	_add_joy_axis("move_left", JOY_AXIS_LEFT_X, -1.0)
	_add_joy_axis("move_right", JOY_AXIS_LEFT_X, 1.0)
	_add_joy_axis("move_up", JOY_AXIS_LEFT_Y, -1.0)
	_add_joy_axis("move_down", JOY_AXIS_LEFT_Y, 1.0)
	_add_joy_button("attack", JOY_BUTTON_A)
	_add_joy_button("dodge", JOY_BUTTON_B)
	_add_joy_button("guard", JOY_BUTTON_LEFT_SHOULDER)
	_add_joy_button("skill_1", JOY_BUTTON_X)
	_add_joy_button("skill_2", JOY_BUTTON_Y)


static func _ensure_action(action_name: String) -> void:
	if not InputMap.has_action(action_name):
		InputMap.add_action(action_name)
	InputMap.action_erase_events(action_name)


static func _add_key(action_name: String, keycode: int) -> void:
	var event := InputEventKey.new()
	event.physical_keycode = keycode
	InputMap.action_add_event(action_name, event)


static func _add_mouse(action_name: String, button_index: int) -> void:
	var event := InputEventMouseButton.new()
	event.button_index = button_index
	InputMap.action_add_event(action_name, event)


static func _add_joy_button(action_name: String, button_index: int) -> void:
	var event := InputEventJoypadButton.new()
	event.button_index = button_index
	InputMap.action_add_event(action_name, event)


static func _add_joy_axis(action_name: String, axis: int, axis_value: float) -> void:
	var event := InputEventJoypadMotion.new()
	event.axis = axis
	event.axis_value = axis_value
	InputMap.action_add_event(action_name, event)
