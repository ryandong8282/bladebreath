class_name PlayerController
extends Combatant


const LONG_BLADE = preload("res://resources/weapons/longblade.tres")
const HEARING_BLADE = preload("res://resources/styles/hearing_blade.tres")
const FLOWING_SHADOW = preload("res://resources/styles/flowing_shadow.tres")

signal style_changed(style: StyleDefinition)

const COMBO_DAMAGE := [12.0, 14.0, 21.0]
const COMBO_POSTURE := [14.0, 17.0, 27.0]
const COMBO_WINDUP := [0.11, 0.13, 0.21]
const COMBO_RECOVERY := [0.17, 0.18, 0.30]
const COMBO_CLASH_LEVEL := [1, 1, 2]
const COMBO_SWING_FROM := [-72.0, 70.0, -96.0]
const COMBO_SWING_TO := [58.0, -62.0, 84.0]

var weapon: WeaponDefinition = LONG_BLADE
var current_style: StyleDefinition = HEARING_BLADE
var movement_basis: Node3D
var mobile_move := Vector2.ZERO

var action_data: Dictionary = {}
var action_active_at: float = 0.0
var action_active_until: float = 0.0
var action_end_at: float = 0.0
var action_started_active: bool = false
var attack_buffered: bool = false
var combo_index: int = 0
var combo_reset_at: float = 0.0

var dodge_direction := Vector3.ZERO
var dodge_end_at: float = 0.0


func _ready() -> void:
	super()
	configure_common("试刃者", 120.0, 100.0, 4.8)
	guard_posture_multiplier = weapon.guard_posture_multiplier
	parry_window_seconds = weapon.parry_window_seconds
	parry_posture_damage = weapon.parry_posture_damage
	clash_posture_damage = weapon.clash_posture_damage
	max_edge = current_style.max_edge
	edge = 1
	build_debug_visuals(
		Color(0.17, 0.32, 0.35, 1.0),
		current_style.accent_color
	)
	_apply_style_visual()


func _physics_process(delta: float) -> void:
	tick_combat(delta)
	_handle_action_input()

	if state == CombatTypesScript.State.DEAD:
		return

	match state:
		CombatTypesScript.State.ATTACKING:
			_update_action()
			_update_attack_motion(delta)
		CombatTypesScript.State.DODGING:
			_update_dodge(delta)
		CombatTypesScript.State.STAGGERED:
			velocity.x = move_toward(velocity.x, 0.0, acceleration * delta)
			velocity.z = move_toward(velocity.z, 0.0, acceleration * delta)
			apply_gravity(delta)
			move_and_slide()
		_:
			_update_ground_movement(delta)


func set_movement_basis(value: Node3D) -> void:
	movement_basis = value


func set_mobile_move(value: Vector2) -> void:
	mobile_move = value.limit_length(1.0)


func request_light_attack() -> void:
	if state == CombatTypesScript.State.ATTACKING:
		if str(action_data.get("kind", "")) == "light" and _now() >= action_active_at - 0.03:
			attack_buffered = true
		return
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING, CombatTypesScript.State.GUARDING]):
		return
	if _can_execute_target():
		_start_execution()
		return

	end_guard()
	var now := _now()
	if now > combo_reset_at:
		combo_index = 0
	var index := combo_index
	combo_index = (combo_index + 1) % COMBO_DAMAGE.size()
	_start_action({
		"kind": "light",
		"name": "长刀第%d式" % (index + 1),
		"damage": COMBO_DAMAGE[index],
		"posture_damage": COMBO_POSTURE[index],
		"range": weapon.light_range + (0.18 if index == 2 else 0.0),
		"arc": weapon.light_arc_degrees,
		"clash_level": COMBO_CLASH_LEVEL[index],
		"windup": COMBO_WINDUP[index],
		"active": 0.11,
		"recovery": COMBO_RECOVERY[index],
		"swing_from": COMBO_SWING_FROM[index],
		"swing_to": COMBO_SWING_TO[index],
		"lunge": 1.5 if index < 2 else 2.1,
	})
	combo_reset_at = action_end_at + 0.58


func request_guard_start() -> void:
	if begin_guard():
		if is_instance_valid(target):
			face_point(target.global_position)


func request_guard_end() -> void:
	end_guard()


func request_dodge() -> void:
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING, CombatTypesScript.State.GUARDING]):
		return
	end_guard()
	var move_direction := _read_world_move_direction()
	if move_direction.length_squared() <= 0.001:
		move_direction = -global_transform.basis.z
		move_direction.y = 0.0
		move_direction = move_direction.normalized()
	dodge_direction = move_direction
	if not begin_dodge(0.36, 0.13):
		return
	dodge_end_at = _now() + 0.36
	velocity.x = dodge_direction.x * 8.6
	velocity.z = dodge_direction.z * 8.6
	combat_log.emit("闪身：前 0.13 秒可触发完美闪避。")


func request_skill(slot: int) -> void:
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING, CombatTypesScript.State.GUARDING]):
		return
	if not spend_edge(2):
		combat_log.emit("锋意不足：武技消耗 2 格，先用本流派的正确操作夺回节奏。")
		return
	end_guard()

	if current_style.style_id == &"hearing_blade":
		if slot == 1:
			_start_hearing_shock()
		else:
			_start_hearing_counter()
	else:
		if slot == 1:
			_start_flowing_step()
		else:
			_start_flowing_chase()


func switch_style() -> void:
	if not (state in [CombatTypesScript.State.IDLE, CombatTypesScript.State.MOVING, CombatTypesScript.State.GUARDING]):
		return
	end_guard()
	if current_style.style_id == &"hearing_blade":
		current_style = FLOWING_SHADOW
	else:
		current_style = HEARING_BLADE
	max_edge = current_style.max_edge
	edge = mini(edge, max_edge)
	edge_changed.emit(edge, max_edge)
	_apply_style_visual()
	style_changed.emit(current_style)
	combat_log.emit("切换为「%s」：%s" % [current_style.display_name, current_style.description])


func _handle_action_input() -> void:
	if state == CombatTypesScript.State.DEAD:
		return
	if Input.is_action_just_pressed("toggle_touch_overlay"):
		get_tree().call_group("touch_overlay", "toggle_overlay")
	if Input.is_action_just_pressed("switch_style"):
		switch_style()
	if Input.is_action_just_pressed("guard"):
		request_guard_start()
	if Input.is_action_just_released("guard"):
		request_guard_end()
	if Input.is_action_just_pressed("dodge"):
		request_dodge()
	if Input.is_action_just_pressed("attack"):
		request_light_attack()
	if Input.is_action_just_pressed("skill_1"):
		request_skill(1)
	if Input.is_action_just_pressed("skill_2"):
		request_skill(2)


func _update_ground_movement(delta: float) -> void:
	var direction := _read_world_move_direction()
	var speed_multiplier := 0.48 if state == CombatTypesScript.State.GUARDING else 1.0
	var desired := direction * move_speed * speed_multiplier
	velocity.x = move_toward(velocity.x, desired.x, acceleration * delta)
	velocity.z = move_toward(velocity.z, desired.z, acceleration * delta)

	if state != CombatTypesScript.State.GUARDING:
		if direction.length_squared() > 0.001:
			_set_state(CombatTypesScript.State.MOVING)
		else:
			_set_state(CombatTypesScript.State.IDLE)

	if is_instance_valid(target) and global_position.distance_to(target.global_position) < 6.8:
		face_point(target.global_position)
	elif direction.length_squared() > 0.001:
		face_point(global_position + direction)

	apply_gravity(delta)
	move_and_slide()


func _read_world_move_direction() -> Vector3:
	var keyboard := Input.get_vector("move_left", "move_right", "move_up", "move_down")
	var input_vector := keyboard
	if mobile_move.length_squared() > input_vector.length_squared():
		input_vector = mobile_move
	if input_vector.length_squared() <= 0.001:
		return Vector3.ZERO

	var right := Vector3.RIGHT
	var forward := Vector3.FORWARD
	if is_instance_valid(movement_basis):
		right = movement_basis.global_transform.basis.x
		forward = -movement_basis.global_transform.basis.z
	right.y = 0.0
	forward.y = 0.0
	right = right.normalized()
	forward = forward.normalized()
	return (right * input_vector.x + forward * -input_vector.y).normalized()


func _start_action(data: Dictionary) -> void:
	action_data = data
	action_started_active = false
	attack_buffered = false
	_set_state(CombatTypesScript.State.ATTACKING)
	var now := _now()
	action_active_at = now + float(data.get("windup", 0.12))
	action_active_until = action_active_at + float(data.get("active", 0.11))
	action_end_at = action_active_until + float(data.get("recovery", 0.20))
	if is_instance_valid(target):
		face_point(target.global_position)
	_animate_sword(
		float(data.get("swing_from", -70.0)),
		float(data.get("swing_to", 65.0)),
		action_active_at - now,
		float(data.get("active", 0.11))
	)
	pulse_accent(1.20, maxf(0.12, action_end_at - now))


func _update_action() -> void:
	if state != CombatTypesScript.State.ATTACKING or action_data.is_empty():
		return
	var now := _now()
	if not action_started_active and now >= action_active_at:
		action_started_active = true
		mark_weapon_active(
			maxf(0.05, action_active_until - action_active_at),
			int(action_data.get("clash_level", 1))
		)
		_resolve_action_hit()

	if now >= action_end_at and state == CombatTypesScript.State.ATTACKING:
		var should_chain := attack_buffered and str(action_data.get("kind", "")) == "light"
		action_data = {}
		action_started_active = false
		reset_sword_intent()
		if sword_pivot != null:
			sword_pivot.rotation = Vector3.ZERO
		_set_state(CombatTypesScript.State.IDLE)
		if should_chain:
			request_light_attack()


func _update_attack_motion(delta: float) -> void:
	var lunge := float(action_data.get("lunge", 0.0))
	if action_started_active and _now() < action_active_until and lunge > 0.0:
		var forward := -global_transform.basis.z
		velocity.x = forward.x * lunge
		velocity.z = forward.z * lunge
	else:
		velocity.x = move_toward(velocity.x, 0.0, acceleration * 1.4 * delta)
		velocity.z = move_toward(velocity.z, 0.0, acceleration * 1.4 * delta)
	apply_gravity(delta)
	move_and_slide()


func _update_dodge(delta: float) -> void:
	if _now() >= dodge_end_at:
		velocity.x = 0.0
		velocity.z = 0.0
		_set_state(CombatTypesScript.State.IDLE)
		return
	velocity.x = dodge_direction.x * 8.6
	velocity.z = dodge_direction.z * 8.6
	apply_gravity(delta)
	move_and_slide()


func _resolve_action_hit() -> void:
	if action_data.is_empty() or not is_instance_valid(target):
		return

	var kind := str(action_data.get("kind", ""))
	if kind == "execution":
		if target.receive_execution(58.0):
			combat_log.emit("处决命中。")
			impact_requested.emit(1.15)
		return

	if kind == "flow_step":
		_move_through_target()
	elif kind == "flow_chase":
		_move_near_target(1.35)

	var in_range := false
	if bool(action_data.get("radial", false)):
		in_range = global_position.distance_to(target.global_position) <= float(action_data.get("range", 3.0))
	else:
		in_range = attack_target_in_cone(
			float(action_data.get("range", weapon.light_range)),
			float(action_data.get("arc", weapon.light_arc_degrees))
		)
	if not in_range:
		combat_log.emit("刀势落空。")
		return

	var result := target.receive_attack({
		"source": self,
		"name": str(action_data.get("name", "长刀攻击")),
		"damage": float(action_data.get("damage", 0.0)),
		"posture_damage": float(action_data.get("posture_damage", 0.0)),
		"parryable": true,
		"unblockable": false,
		"clashable": true,
		"clash_level": int(action_data.get("clash_level", 1)),
	})
	if result in [
		CombatTypesScript.HitResult.HIT,
		CombatTypesScript.HitResult.BLOCKED,
		CombatTypesScript.HitResult.PARRIED,
		CombatTypesScript.HitResult.CLASHED,
	]:
		impact_requested.emit(0.78 if kind != "light" else 0.46)


func _start_hearing_shock() -> void:
	combat_log.emit("震刃：近身爆发高额架势伤害。")
	_start_action({
		"kind": "skill",
		"name": "震刃",
		"damage": 7.0,
		"posture_damage": 38.0,
		"range": 3.15,
		"arc": 360.0,
		"radial": true,
		"clash_level": 2,
		"windup": 0.16,
		"active": 0.14,
		"recovery": 0.28,
		"swing_from": -105.0,
		"swing_to": 105.0,
		"lunge": 0.0,
	})


func _start_hearing_counter() -> void:
	combat_log.emit("回锋：向前抢回合，生命与架势同时受压。")
	_start_action({
		"kind": "skill",
		"name": "回锋",
		"damage": 25.0,
		"posture_damage": 28.0,
		"range": 3.05,
		"arc": 92.0,
		"clash_level": 3,
		"windup": 0.10,
		"active": 0.12,
		"recovery": 0.34,
		"swing_from": 92.0,
		"swing_to": -78.0,
		"lunge": 4.4,
	})


func _start_flowing_step() -> void:
	invulnerable_until = _now() + 0.30
	combat_log.emit("掠影：穿过攻击线，落到敌人另一侧。")
	_start_action({
		"kind": "flow_step",
		"name": "掠影",
		"damage": 14.0,
		"posture_damage": 14.0,
		"range": 2.9,
		"arc": 150.0,
		"clash_level": 1,
		"windup": 0.08,
		"active": 0.10,
		"recovery": 0.20,
		"swing_from": -52.0,
		"swing_to": 48.0,
		"lunge": 0.0,
	})


func _start_flowing_chase() -> void:
	combat_log.emit("追风斩：快速贴身并重斩。")
	_start_action({
		"kind": "flow_chase",
		"name": "追风斩",
		"damage": 23.0,
		"posture_damage": 22.0,
		"range": 3.25,
		"arc": 110.0,
		"clash_level": 2,
		"windup": 0.12,
		"active": 0.11,
		"recovery": 0.26,
		"swing_from": 100.0,
		"swing_to": -92.0,
		"lunge": 0.0,
	})


func _start_execution() -> void:
	combat_log.emit("架势已破——处决。")
	_start_action({
		"kind": "execution",
		"name": "处决",
		"clash_level": 3,
		"windup": 0.18,
		"active": 0.10,
		"recovery": 0.42,
		"swing_from": -118.0,
		"swing_to": 82.0,
		"lunge": 2.3,
	})


func _can_execute_target() -> bool:
	return (
		is_instance_valid(target)
		and target.is_execution_ready()
		and global_position.distance_to(target.global_position) <= 2.95
	)


func _move_through_target() -> void:
	var direction := target.global_position - global_position
	direction.y = 0.0
	if direction.length_squared() <= 0.001:
		return
	direction = direction.normalized()
	global_position = target.global_position + direction * 1.15
	face_point(target.global_position)


func _move_near_target(distance_value: float) -> void:
	var direction := target.global_position - global_position
	direction.y = 0.0
	if direction.length_squared() <= 0.001:
		return
	direction = direction.normalized()
	global_position = target.global_position - direction * distance_value
	face_point(target.global_position)


func _animate_sword(from_degrees: float, to_degrees: float, windup: float, active: float) -> void:
	if sword_pivot == null:
		return
	sword_pivot.rotation = Vector3(0.0, deg_to_rad(from_degrees), deg_to_rad(7.0))
	var tween := create_tween()
	tween.tween_interval(maxf(0.01, windup * 0.58))
	tween.tween_property(
		sword_pivot,
		"rotation:y",
		deg_to_rad(to_degrees),
		maxf(0.05, windup * 0.42 + active)
	).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)


func _apply_style_visual() -> void:
	accent_color = current_style.accent_color
	if accent_ring != null:
		accent_ring.material_override = PrimitiveFactoryScript.material(
			accent_color,
			0.0,
			0.56,
			accent_color,
			1.6
		)


func _after_parry_success(_source: Combatant) -> void:
	if current_style.style_id == &"hearing_blade":
		add_edge(1)
		combat_log.emit("听刃 · 精准弹反：获得 1 格锋意。")
	else:
		combat_log.emit("精准弹反成功，但流影不会因此获得锋意。")


func _after_perfect_dodge(source: Combatant) -> void:
	super(source)
	if current_style.style_id == &"flowing_shadow":
		add_edge(1)
		combat_log.emit("流影 · 完美闪避：获得 1 格锋意。")
	else:
		combat_log.emit("完美闪避成功，但听刃不会因此获得锋意。")


func _after_clash_won(_source: Combatant) -> void:
	if current_style.style_id == &"hearing_blade":
		add_edge(1)
		combat_log.emit("听刃 · 压刀成功：获得 1 格锋意。")
	else:
		combat_log.emit("压刀成功。")


func _on_action_interrupted() -> void:
	action_data = {}
	action_started_active = false
	attack_buffered = false
	combo_index = 0
	combo_reset_at = 0.0
	if sword_pivot != null:
		sword_pivot.rotation = Vector3.ZERO
	reset_sword_intent()
