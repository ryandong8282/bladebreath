extends Node3D


const InputBootstrapScript := preload("res://src/core/input_bootstrap.gd")
const ArenaBuilderScript := preload("res://src/world/arena_builder.gd")
const PlayerScript := preload("res://src/actors/player_controller.gd")
const EnemyScript := preload("res://src/actors/enemy_controller.gd")
const CameraScript := preload("res://src/camera/camera_follow.gd")
const HUDScript := preload("res://src/ui/combat_hud.gd")
const MobileControlsScript := preload("res://src/ui/mobile_controls.gd")

var player: PlayerController
var enemy: EnemyController
var combat_camera: CameraFollow
var hud: CombatHUD
var mobile_controls: MobileControls
var battle_finished: bool = false
var battle_finished_at: float = -100.0


func _ready() -> void:
	InputBootstrapScript.ensure_actions()
	RenderingServer.set_default_clear_color(Color(0.018, 0.017, 0.014, 1.0))
	ArenaBuilderScript.build(self)
	_spawn_combatants()
	_build_presentation()


func _process(_delta: float) -> void:
	var restart_from_result := (
		battle_finished
		and _now() - battle_finished_at > 0.45
		and Input.is_action_just_pressed("attack")
	)
	if Input.is_action_just_pressed("restart_demo") or restart_from_result:
		get_tree().reload_current_scene()


func _spawn_combatants() -> void:
	player = PlayerScript.new()
	add_child(player)
	player.global_position = Vector3(-2.6, 0.05, 2.1)
	player.rotation.y = deg_to_rad(-45.0)

	enemy = EnemyScript.new()
	add_child(enemy)
	enemy.global_position = Vector3(2.4, 0.05, -1.5)
	enemy.rotation.y = deg_to_rad(135.0)

	player.target = enemy
	enemy.target = player
	player.died.connect(_on_player_died)
	enemy.died.connect(_on_enemy_died)


func _build_presentation() -> void:
	combat_camera = CameraScript.new()
	add_child(combat_camera)
	combat_camera.set_follow_target(player)
	player.set_movement_basis(combat_camera)
	player.impact_requested.connect(_on_impact_requested)
	enemy.impact_requested.connect(_on_impact_requested)

	hud = HUDScript.new()
	add_child(hud)
	hud.bind_actors(player, enemy)

	mobile_controls = MobileControlsScript.new()
	add_child(mobile_controls)
	mobile_controls.bind_player(player)

	player.combat_log.emit("校刀院封门。读招、接刀、夺势、破势。")


func _on_impact_requested(strength: float) -> void:
	if is_instance_valid(combat_camera):
		combat_camera.add_impulse(strength)


func _on_player_died(_actor: Combatant) -> void:
	if battle_finished:
		return
	battle_finished = true
	battle_finished_at = _now()
	_freeze_combat()
	hud.show_result(
		"试刃失败",
		"不是数值不够。回想刚才是早挡、贪刀，还是把红招当成了普通刀。"
	)


func _on_enemy_died(_actor: Combatant) -> void:
	if battle_finished:
		return
	battle_finished = true
	battle_finished_at = _now()
	_freeze_combat()
	hud.show_result(
		"执刃者倒下",
		"铭牌背面刻着：我有名，不是这个。按 Tab 换式，再赢一次。"
	)


func _freeze_combat() -> void:
	_freeze_actor(player)
	_freeze_actor(enemy)


func _freeze_actor(actor: Combatant) -> void:
	if not is_instance_valid(actor):
		return
	actor.velocity = Vector3.ZERO
	actor.set_physics_process(false)


func _now() -> float:
	return Time.get_ticks_msec() / 1000.0
