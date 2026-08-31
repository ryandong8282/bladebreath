class_name VirtualJoystick
extends Node2D


signal vector_changed(value: Vector2)

var radius: float = 78.0
var knob_radius: float = 33.0
var active_touch: int = -1
var value := Vector2.ZERO
var base_color := Color(0.05, 0.09, 0.11, 0.58)
var edge_color := Color(0.70, 0.86, 0.87, 0.58)
var knob_color := Color(0.50, 0.82, 0.84, 0.78)


func _ready() -> void:
	set_process_input(true)
	queue_redraw()


func _input(event: InputEvent) -> void:
	if not is_visible_in_tree():
		return
	if event is InputEventScreenTouch:
		_handle_screen_touch(event)
	elif event is InputEventScreenDrag:
		_handle_screen_drag(event)


func reset() -> void:
	active_touch = -1
	_set_value(Vector2.ZERO)


func _handle_screen_touch(event: InputEventScreenTouch) -> void:
	if event.pressed and not event.canceled:
		if active_touch == -1 and to_local(event.position).length() <= radius * 1.45:
			active_touch = event.index
			_update_from_screen(event.position)
			get_viewport().set_input_as_handled()
	elif event.index == active_touch:
		reset()
		get_viewport().set_input_as_handled()


func _handle_screen_drag(event: InputEventScreenDrag) -> void:
	if event.index != active_touch:
		return
	_update_from_screen(event.position)
	get_viewport().set_input_as_handled()


func _update_from_screen(screen_position: Vector2) -> void:
	var local := to_local(screen_position)
	_set_value(local.limit_length(radius) / radius)


func _set_value(next_value: Vector2) -> void:
	value = next_value.limit_length(1.0)
	vector_changed.emit(value)
	queue_redraw()


func _draw() -> void:
	draw_circle(Vector2.ZERO, radius, base_color)
	draw_arc(Vector2.ZERO, radius - 2.0, 0.0, TAU, 64, edge_color, 3.0, true)
	draw_circle(value * radius, knob_radius, knob_color)
