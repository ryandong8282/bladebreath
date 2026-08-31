class_name Combatant
extends CharacterBody3D


const CombatTypesScript := preload("res://src/core/combat_types.gd")
const PrimitiveFactoryScript := preload("res://src/world/primitive_factory.gd")

signal health_changed(current: float, maximum: float)
signal posture_changed(current: float, maximum: float)
signal edge_changed(current: int, maximum: int)
signal state_changed(previous: int, current: int)
signal combat_log(message: String)
signal impact_requested(strength: float)
signal clash_resolved(outcome: String, other: Combatant)
signal died(actor: Combatant)

var combat_name: String = "Combatant"
var state: int = CombatTypesScript.State.IDLE
var target: Combatant

var max_health: float = 100.0
var health: float = 100.0
var max_posture: float = 100.0
var posture: float = 100.0
var max_edge: int = 3
var edge: int = 0

var move_speed: float = 4.6
var acceleration: float = 22.0
var guard_posture_multiplier: float = 0.72
var parry_window_seconds: float = 0.17
var parry_posture_damage: float = 36.0
var clash_posture_damage: float = 15.0

var parry_until: float = 0.0
var invulnerable_until: float = 0.0
var perfect_dodge_until: float = 0.0
var weapon_active_until: float = 0.0
var current_clash_level: int = 0
var stagger_until: float = 0.0
var last_pressure_at: float = 0.0
var gravity_strength: float = 22.0

var visual_root: Node3D
var sword_pivot: Node3D
var sword_blade: MeshInstance3D
var accent_ring: MeshInstance3D
var body_mesh: MeshInstance3D
var base_body_material: StandardMaterial3D
var base_sword_material: StandardMaterial3D
var accent_color: Color = Color(0.52, 0.90, 1.0, 1.0)


func _ready() -> void:
	gravity_strength = float(ProjectSettings.get_setting("physics/3d/default_gravity", 22.0))
	motion_mode = CharacterBody3D.MOTION_MODE_GROUNDED
	floor_snap_length = 0.35


func configure_common(
	name_value: String,
	health_value: float,
	posture_value: float,
	move_speed_value: float
) -> void:
	combat_name = name_value
	max_health = health_value
	health = max_health
	max_posture = posture_value
	posture = max_posture
	move_speed = move_speed_value


func build_debug_visuals(body_color: Color, accent_value: Color) -> void:
	accent_color = accent_value

	var collision := CollisionShape3D.new()
	collision.name = "Collision"
	var capsule_shape := CapsuleShape3D.new()
	capsule_shape.radius = 0.43
	capsule_shape.height = 1.72
	collision.shape = capsule_shape
	collision.position = Vector3(0.0, 0.88, 0.0)
	add_child(collision)

	visual_root = Node3D.new()
	visual_root.name = "VisualRoot"
	add_child(visual_root)

	body_mesh = PrimitiveFactoryScript.capsule(0.43, 1.72, body_color)
	body_mesh.name = "Body"
	body_mesh.position = Vector3(0.0, 0.88, 0.0)
	visual_root.add_child(body_mesh)
	base_body_material = body_mesh.material_override as StandardMaterial3D

	var head := PrimitiveFactoryScript.cylinder(0.29, 0.38, body_color.lightened(0.08))
	head.name = "Head"
	head.position = Vector3(0.0, 1.89, 0.0)
	visual_root.add_child(head)

	var shoulder := PrimitiveFactoryScript.box(
		Vector3(1.05, 0.18, 0.34),
		body_color.darkened(0.12),
		0.0,
		0.72
	)
	shoulder.name = "Shoulder"
	shoulder.position = Vector3(0.0, 1.35, 0.0)
	visual_root.add_child(shoulder)

	sword_pivot = Node3D.new()
	sword_pivot.name = "SwordPivot"
	sword_pivot.position = Vector3(0.48, 1.18, -0.06)
	visual_root.add_child(sword_pivot)

	var handle := PrimitiveFactoryScript.cylinder(
		0.065,
		0.46,
		Color(0.12, 0.075, 0.045, 1.0),
		0.0,
		0.82
	)
	handle.rotation_degrees.x = 90.0
	handle.position = Vector3(0.0, 0.0, -0.16)
	sword_pivot.add_child(handle)

	sword_blade = PrimitiveFactoryScript.box(
		Vector3(0.12, 0.055, 2.18),
		Color(0.63, 0.70, 0.72, 1.0),
		0.76,
		0.22
	)
	sword_blade.name = "Longblade"
	sword_blade.position = Vector3(0.0, 0.0, -1.36)
	sword_pivot.add_child(sword_blade)
	base_sword_material = sword_blade.material_override as StandardMaterial3D

	accent_ring = PrimitiveFactoryScript.ring(0.72, 0.62, accent_color)
	accent_ring.name = "AccentRing"
	accent_ring.position = Vector3(0.0, 0.045, 0.0)
	visual_root.add_child(accent_ring)


func tick_combat(delta: float) -> void:
	if state == CombatTypesScript.State.DEAD:
		return

	var now := _now()
	if now >= weapon_active_until:
		current_clash_level = 0

	if state == CombatTypesScript.State.STAGGERED and now >= stagger_until:
		if posture <= 0.01:
			posture = max_posture * 0.45
			posture_changed.emit(posture, max_posture)
		_set_state(CombatTypesScript.State.IDLE)
		_after_stagger_recovered()

	if (
		state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING]
		and now - last_pressure_at > 1.15
		and posture < max_posture
	):
		posture = minf(max_posture, posture + 12.0 * delta)
		posture_changed.emit(posture, max_posture)


func begin_guard() -> bool:
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING]):
		return false
	_set_state(CombatTypesScript.State.GUARDING)
	parry_until = _now() + parry_window_seconds
	pulse_accent(1.18, 0.12)
	return true


func end_guard() -> void:
	if state == CombatTypesScript.State.GUARDING:
		_set_state(CombatTypesScript.State.IDLE)


func begin_dodge(total_duration: float, perfect_window: float) -> bool:
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING]):
		return false
	_set_state(CombatTypesScript.State.DODGING)
	var now := _now()
	invulnerable_until = now + total_duration * 0.78
	perfect_dodge_until = now + perfect_window
	pulse_accent(1.28, total_duration * 0.45)
	return true


func mark_weapon_active(duration: float, clash_level: int) -> void:
	weapon_active_until = _now() + duration
	current_clash_level = maxi(1, clash_level)
	set_sword_intent(accent_color, 2.8)


func receive_attack(attack: Dictionary) -> int:
	if state == CombatTypesScript.State.DEAD:
		return CombatTypesScript.HitResult.IGNORED

	var source := attack.get("source", null) as Combatant
	if source == self:
		return CombatTypesScript.HitResult.IGNORED

	var now := _now()
	if now < invulnerable_until:
		if now < perfect_dodge_until:
			_after_perfect_dodge(source)
			return CombatTypesScript.HitResult.PERFECT_DODGE
		return CombatTypesScript.HitResult.DODGED

	var parryable := bool(attack.get("parryable", true))
	var unblockable := bool(attack.get("unblockable", false))
	var clashable := bool(attack.get("clashable", parryable))
	var incoming_clash_level := int(attack.get("clash_level", 1))

	if (
		clashable
		and parryable
		and now < weapon_active_until
		and current_clash_level > 0
		and is_instance_valid(source)
	):
		return _resolve_clash(source, incoming_clash_level)

	if state == CombatTypesScript.State.GUARDING and _is_source_in_front(source):
		if not unblockable:
			if now <= parry_until and parryable:
				_on_parry_success(source)
				return CombatTypesScript.HitResult.PARRIED
			var blocked_posture := float(attack.get("posture_damage", 0.0))
			apply_posture_damage(blocked_posture * guard_posture_multiplier)
			impact_requested.emit(0.24)
			combat_log.emit("%s 挡住了攻击，但架势承压。" % combat_name)
			return CombatTypesScript.HitResult.BLOCKED

	apply_health_damage(float(attack.get("damage", 0.0)))
	apply_posture_damage(float(attack.get("posture_damage", 0.0)) * 0.34)
	flash_body(Color(1.0, 0.24, 0.18, 1.0), 0.12)
	impact_requested.emit(0.48)
	return CombatTypesScript.HitResult.HIT


func receive_execution(damage: float) -> bool:
	if not is_execution_ready():
		return false
	apply_health_damage(damage)
	if state != CombatTypesScript.State.DEAD:
		posture = max_posture * 0.48
		posture_changed.emit(posture, max_posture)
		force_stagger(0.48)
	return true


func is_execution_ready() -> bool:
	return (
		state == CombatTypesScript.State.STAGGERED
		and posture <= 0.01
		and health > 0.0
	)


func apply_health_damage(amount: float) -> void:
	if amount <= 0.0 or state == CombatTypesScript.State.DEAD:
		return
	health = maxf(0.0, health - amount)
	health_changed.emit(health, max_health)
	if health <= 0.0:
		die()


func apply_posture_damage(amount: float) -> void:
	if amount <= 0.0 or state == CombatTypesScript.State.DEAD:
		return
	last_pressure_at = _now()
	posture = maxf(0.0, posture - amount)
	posture_changed.emit(posture, max_posture)
	if posture <= 0.01 and state != CombatTypesScript.State.STAGGERED:
		force_stagger(1.75)
		combat_log.emit("%s 架势崩溃，可处决！" % combat_name)


func force_stagger(duration: float) -> void:
	if state == CombatTypesScript.State.DEAD:
		return
	_on_action_interrupted()
	velocity = Vector3.ZERO
	weapon_active_until = 0.0
	current_clash_level = 0
	stagger_until = maxf(stagger_until, _now() + duration)
	_set_state(CombatTypesScript.State.STAGGERED)
	pulse_accent(1.45, 0.16)


func die() -> void:
	if state == CombatTypesScript.State.DEAD:
		return
	_on_action_interrupted()
	velocity = Vector3.ZERO
	_set_state(CombatTypesScript.State.DEAD)
	collision_layer = 0
	collision_mask = 0
	if visual_root != null:
		var tween := create_tween()
		tween.tween_property(visual_root, "rotation:z", deg_to_rad(78.0), 0.38)
		tween.parallel().tween_property(visual_root, "position:y", -0.34, 0.38)
	died.emit(self)


func add_edge(amount: int) -> void:
	if amount <= 0:
		return
	var previous := edge
	edge = mini(max_edge, edge + amount)
	if edge != previous:
		edge_changed.emit(edge, max_edge)


func spend_edge(amount: int) -> bool:
	if amount <= 0:
		return true
	if edge < amount:
		return false
	edge -= amount
	edge_changed.emit(edge, max_edge)
	return true


func attack_target_in_cone(range_value: float, arc_degrees: float) -> bool:
	if not is_instance_valid(target) or target.state == CombatTypesScript.State.DEAD:
		return false
	var offset := target.global_position - global_position
	offset.y = 0.0
	var distance := offset.length()
	if distance <= 0.001 or distance > range_value:
		return false
	var forward := -global_transform.basis.z
	forward.y = 0.0
	forward = forward.normalized()
	var direction := offset / distance
	return forward.dot(direction) >= cos(deg_to_rad(arc_degrees * 0.5))


func face_point(point: Vector3) -> void:
	var flat_point := Vector3(point.x, global_position.y, point.z)
	if flat_point.distance_squared_to(global_position) > 0.0001:
		look_at(flat_point, Vector3.UP)


func apply_gravity(delta: float) -> void:
	if is_on_floor():
		velocity.y = -0.35
	else:
		velocity.y -= gravity_strength * delta


func set_sword_intent(color: Color, emission_energy: float) -> void:
	if sword_blade == null:
		return
	sword_blade.material_override = PrimitiveFactoryScript.material(
		color,
		0.72,
		0.20,
		color,
		emission_energy
	)


func reset_sword_intent() -> void:
	if sword_blade != null and base_sword_material != null:
		sword_blade.material_override = base_sword_material


func pulse_accent(scale_value: float, duration: float) -> void:
	if accent_ring == null:
		return
	accent_ring.scale = Vector3.ONE
	var tween := create_tween()
	tween.tween_property(
		accent_ring,
		"scale",
		Vector3.ONE * scale_value,
		maxf(0.04, duration * 0.45)
	).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tween.tween_property(
		accent_ring,
		"scale",
		Vector3.ONE,
		maxf(0.04, duration * 0.55)
	).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)


func flash_body(color: Color, duration: float) -> void:
	if body_mesh == null or base_body_material == null:
		return
	body_mesh.material_override = PrimitiveFactoryScript.material(
		color,
		0.0,
		0.54,
		color,
		1.1
	)
	await get_tree().create_timer(duration).timeout
	if is_instance_valid(body_mesh) and state != CombatTypesScript.State.DEAD:
		body_mesh.material_override = base_body_material


func _resolve_clash(source: Combatant, incoming_level: int) -> int:
	var own_level := current_clash_level
	weapon_active_until = 0.0
	current_clash_level = 0

	if own_level > incoming_level:
		_on_clash_won(source)
		source._on_clash_lost(self)
		clash_resolved.emit("WIN", source)
	elif own_level == incoming_level:
		_on_clash_even(source)
		source._on_clash_even(self)
		clash_resolved.emit("EVEN", source)
	else:
		_on_clash_lost(source)
		source._on_clash_won(self)
		clash_resolved.emit("LOSE", source)
	return CombatTypesScript.HitResult.CLASHED


func _on_parry_success(source: Combatant) -> void:
	if is_instance_valid(source):
		source.apply_posture_damage(parry_posture_damage)
		source.force_stagger(0.46)
	pulse_accent(1.48, 0.16)
	set_sword_intent(accent_color, 4.2)
	impact_requested.emit(0.78)
	_after_parry_success(source)


func _on_clash_won(source: Combatant) -> void:
	pulse_accent(1.38, 0.13)
	set_sword_intent(accent_color, 3.6)
	impact_requested.emit(0.62)
	_after_clash_won(source)


func _on_clash_even(_source: Combatant) -> void:
	apply_posture_damage(clash_posture_damage * 0.58)
	force_stagger(0.16)
	combat_log.emit("双刃相抵，双方都被震开。")


func _on_clash_lost(_source: Combatant) -> void:
	apply_posture_damage(clash_posture_damage * 1.55)
	force_stagger(0.38)
	combat_log.emit("%s 拼刀失势。" % combat_name)


func _after_parry_success(_source: Combatant) -> void:
	pass


func _after_perfect_dodge(_source: Combatant) -> void:
	pulse_accent(1.36, 0.14)


func _after_clash_won(_source: Combatant) -> void:
	pass


func _on_action_interrupted() -> void:
	reset_sword_intent()


func _after_stagger_recovered() -> void:
	pass


func _is_source_in_front(source: Combatant) -> bool:
	if not is_instance_valid(source):
		return true
	var to_source := source.global_position - global_position
	to_source.y = 0.0
	if to_source.length_squared() <= 0.0001:
		return true
	var forward := -global_transform.basis.z
	forward.y = 0.0
	return forward.normalized().dot(to_source.normalized()) >= 0.0


func _set_state(next_state: int) -> void:
	if state == next_state:
		return
	var previous := state
	state = next_state
	state_changed.emit(previous, state)


func _now() -> float:
	return Time.get_ticks_msec() / 1000.0
