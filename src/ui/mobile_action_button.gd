class_name MobileActionButton
extends TouchScreenButton


var radius: float = 48.0
var label_text: String = ""
var normal_color := Color(0.08, 0.12, 0.14, 0.74)
var pressed_color := Color(0.35, 0.75, 0.80, 0.90)
var outline_color := Color(0.78, 0.90, 0.90, 0.72)
var label: Label


func configure(
	text_value: String,
	action_value: String,
	radius_value: float,
	color_value: Color
) -> void:
	label_text = text_value
	action = action_value
	radius = radius_value
	pressed_color = color_value
	var circle := CircleShape2D.new()
	circle.radius = radius
	shape = circle
	shape_centered = true
	shape_visible = false
	passby_press = true
	visibility_mode = TouchScreenButton.VISIBILITY_ALWAYS


func _ready() -> void:
	pressed.connect(_on_pressed)
	released.connect(_on_released)
	_build_label()
	queue_redraw()


func set_label_text(value: String) -> void:
	label_text = value
	if label != null:
		label.text = value


func _build_label() -> void:
	label = Label.new()
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.position = Vector2(-radius, -radius)
	label.size = Vector2(radius * 2.0, radius * 2.0)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.text = label_text
	label.add_theme_font_size_override("font_size", maxi(15, int(radius * 0.39)))
	label.add_theme_color_override("font_color", Color(0.96, 0.98, 0.98, 0.97))
	add_child(label)


func _draw() -> void:
	var fill := pressed_color if is_pressed() else normal_color
	draw_circle(Vector2.ZERO, radius, fill)
	draw_arc(Vector2.ZERO, radius - 2.0, 0.0, TAU, 48, outline_color, 3.0, true)


func _on_pressed() -> void:
	queue_redraw()


func _on_released() -> void:
	queue_redraw()
