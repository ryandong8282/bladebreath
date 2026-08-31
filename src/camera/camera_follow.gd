class_name CameraFollow
extends Camera3D


var target: Node3D
var follow_offset := Vector3(8.8, 11.8, 10.4)
var look_height: float = 0.82
var follow_speed: float = 8.0
var shake_remaining: float = 0.0
var shake_duration: float = 0.0
var shake_strength: float = 0.0
var shake_clock: float = 0.0


func _ready() -> void:
	projection = Camera3D.PROJECTION_ORTHOGONAL
	size = 13.8
	near = 0.1
	far = 80.0
	current = true


func set_follow_target(value: Node3D) -> void:
	target = value
	if is_instance_valid(target):
		global_position = target.global_position + follow_offset
		look_at(target.global_position + Vector3.UP * look_height, Vector3.UP)


func add_impulse(strength: float) -> void:
	shake_duration = 0.13 + strength * 0.07
	shake_remaining = shake_duration
	shake_strength = maxf(shake_strength, 0.08 + strength * 0.14)


func _process(delta: float) -> void:
	if not is_instance_valid(target):
		return

	var focus := target.global_position + Vector3.UP * look_height
	var desired := target.global_position + follow_offset
	var shake_offset := Vector3.ZERO
	if shake_remaining > 0.0:
		shake_remaining = maxf(0.0, shake_remaining - delta)
		shake_clock += delta * 74.0
		var fade := shake_remaining / maxf(shake_duration, 0.001)
		shake_offset = Vector3(
			sin(shake_clock * 1.17),
			cos(shake_clock * 0.91) * 0.42,
			cos(shake_clock * 1.43)
		) * shake_strength * fade
	else:
		shake_strength = 0.0

	global_position = global_position.lerp(
		desired + shake_offset,
		1.0 - exp(-follow_speed * delta)
	)
	look_at(focus, Vector3.UP)
