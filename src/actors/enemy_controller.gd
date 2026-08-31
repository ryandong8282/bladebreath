class_name EnemyController
extends Combatant


signal telegraph_started(title: String, hint: String, color: Color, duration: float)
signal attack_resolved(title: String, result: int)

const ATTACK_SEQUENCE := [
	{
		"name": "明斩",
		"hint": "可挡 · 可弹 · 可拼",
		"windup": 0.52,
		"active": 0.13,
		"recovery": 0.54,
		"damage": 18.0,
		"posture": 24.0,
		"range": 2.70,
		"arc": 105.0,
		"unblockable": false,
		"clash_level": 1,
		"from": -72.0,
		"to": 68.0,
		"color": Color(0.84, 0.82, 0.66, 1.0),
	},
	{
		"name": "迟锋",
		"hint": "延迟二拍 · 不要早按",
		"windup": 0.90,
		"active": 0.14,
		"recovery": 0.68,
		"damage": 25.0,
		"posture": 34.0,
		"range": 2.85,
		"arc": 92.0,
		"unblockable": false,
		"clash_level": 2,
		"from": 96.0,
		"to": -82.0,
		"color": Color(0.95, 0.72, 0.32, 1.0),
	},
	{
		"name": "回身斩",
		"hint": "短前摇 · 抢回合",
		"windup": 0.38,
		"active": 0.12,
		"recovery": 0.48,
		"damage": 16.0,
		"posture": 20.0,
		"range": 2.58,
		"arc": 128.0,
		"unblockable": false,
		"clash_level": 1,
		"from": -105.0,
		"to": 92.0,
		"color": Color(0.74, 0.86, 0.90, 1.0),
	},
	{
		"name": "裂地",
		"hint": "危 · 不可挡，只能闪",
		"windup": 0.84,
		"active": 0.18,
		"recovery": 0.92,
		"damage": 38.0,
		"posture": 42.0,
		"range": 3.25,
		"arc": 150.0,
		"unblockable": true,
		"clash_level": 3,
		"from": 0.0,
		"to": 0.0,
		"color": Color(1.0, 0.20, 0.12, 1.0),
	},
]

var preferred_range: float = 2.25
var attack_index: int = 0
var next_attack_at: float = 0.0
var current_attack: Dictionary = {}
var attack_execute_at: float = 0.0
var recover_until: float = 0.0


func _ready() -> void:
	super()
	configure_common("执刃者", 155.0, 120.0, 3.15)
	parry_window_seconds = 0.11
	parry_posture_damage = 28.0
	clash_posture_damage = 17.0
	build_debug_visuals(
		Color(0.39, 0.12, 0.105, 1.0),
		Color(1.0, 0.31, 0.16, 1.0)
	)
	next_attack_at = _now() + 0.85


func _physics_process(delta: float) -> void:
	tick_combat(delta)
	if state == CombatTypesScript.State.DEAD:
		return

	if state == CombatTypesScript.State.STAGGERED:
		velocity.x = move_toward(velocity.x, 0.0, acceleration * delta)
		velocity.z = move_toward(velocity.z, 0.0, acceleration * delta)
		apply_gravity(delta)
		move_and_slide()
		return

	if (
		not is_instance_valid(target)
		or target.state == CombatTypesScript.State.DEAD
	):
		velocity.x = move_toward(velocity.x, 0.0, acceleration * delta)
		velocity.z = move_toward(velocity.z, 0.0, acceleration * delta)
		apply_gravity(delta)
		move_and_slide()
		return

	if state == CombatTypesScript.State.WINDUP:
		velocity.x = 0.0
		velocity.z = 0.0
		face_point(target.global_position)
		if _now() >= attack_execute_at:
			_execute_current_attack()
		apply_gravity(delta)
		move_and_slide()
		return

	if state == CombatTypesScript.State.ATTACKING:
		velocity.x = 0.0
		velocity.z = 0.0
		apply_gravity(delta)
		move_and_slide()
		return

	if state == CombatTypesScript.State.RECOVERING:
		velocity.x = move_toward(velocity.x, 0.0, acceleration * delta)
		velocity.z = move_toward(velocity.z, 0.0, acceleration * delta)
		if _now() >= recover_until:
			current_attack = {}
			reset_sword_intent()
			if sword_pivot != null:
				sword_pivot.rotation = Vector3.ZERO
			_set_state(CombatTypesScript.State.IDLE)
			next_attack_at = _now() + 0.30
		apply_gravity(delta)
		move_and_slide()
		return

	var offset := target.global_position - global_position
	offset.y = 0.0
	var distance := offset.length()
	if distance > preferred_range:
		var direction := offset.normalized()
		var desired := direction * move_speed
		velocity.x = move_toward(velocity.x, desired.x, acceleration * delta)
		velocity.z = move_toward(velocity.z, desired.z, acceleration * delta)
		_set_state(CombatTypesScript.State.MOVING)
		face_point(target.global_position)
	else:
		velocity.x = move_toward(velocity.x, 0.0, acceleration * delta)
		velocity.z = move_toward(velocity.z, 0.0, acceleration * delta)
		face_point(target.global_position)
		if state == CombatTypesScript.State.MOVING:
			_set_state(CombatTypesScript.State.IDLE)
		if _now() >= next_attack_at:
			_begin_next_attack()

	apply_gravity(delta)
	move_and_slide()


func _begin_next_attack() -> void:
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING]):
		return
	var attack_template: Dictionary = ATTACK_SEQUENCE[attack_index]
	current_attack = attack_template.duplicate(true)
	attack_index = (attack_index + 1) % ATTACK_SEQUENCE.size()
	_set_state(CombatTypesScript.State.WINDUP)
	attack_execute_at = _now() + float(current_attack["windup"])
	face_point(target.global_position)

	var color: Color = current_attack["color"]
	if sword_pivot != null:
		sword_pivot.rotation = Vector3(
			0.0,
			deg_to_rad(float(current_attack["from"])),
			deg_to_rad(7.0)
		)
	set_sword_intent(color, 4.0 if bool(current_attack["unblockable"]) else 2.7)
	pulse_accent(1.34, float(current_attack["windup"]))
	telegraph_started.emit(
		str(current_attack["name"]),
		str(current_attack["hint"]),
		color,
		float(current_attack["windup"])
	)


func _execute_current_attack() -> void:
	if state != CombatTypesScript.State.WINDUP or current_attack.is_empty():
		return
	var attack := current_attack.duplicate(true)
	_set_state(CombatTypesScript.State.ATTACKING)
	mark_weapon_active(
		float(attack["active"]),
		int(attack["clash_level"])
	)

	if sword_pivot != null:
		var swing := create_tween()
		swing.tween_property(
			sword_pivot,
			"rotation:y",
			deg_to_rad(float(attack["to"])),
			float(attack["active"])
		).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

	var result := CombatTypesScript.HitResult.IGNORED
	if attack_target_in_cone(
		float(attack["range"]),
		float(attack["arc"])
	):
		var unblockable := bool(attack["unblockable"])
		result = target.receive_attack({
			"source": self,
			"name": str(attack["name"]),
			"damage": float(attack["damage"]),
			"posture_damage": float(attack["posture"]),
			"parryable": not unblockable,
			"unblockable": unblockable,
			"clashable": not unblockable,
			"clash_level": int(attack["clash_level"]),
		})
		if result in [
			CombatTypesScript.HitResult.HIT,
			CombatTypesScript.HitResult.BLOCKED,
			CombatTypesScript.HitResult.PARRIED,
			CombatTypesScript.HitResult.CLASHED,
		]:
			impact_requested.emit(0.78 if unblockable else 0.46)
	attack_resolved.emit(str(attack["name"]), result)

	if state == CombatTypesScript.State.ATTACKING:
		_set_state(CombatTypesScript.State.RECOVERING)
		recover_until = _now() + float(attack["recovery"])


func _on_action_interrupted() -> void:
	current_attack = {}
	attack_execute_at = 0.0
	recover_until = 0.0
	next_attack_at = _now() + 0.72
	if sword_pivot != null:
		sword_pivot.rotation = Vector3.ZERO
	reset_sword_intent()


func _after_stagger_recovered() -> void:
	next_attack_at = _now() + 0.66
