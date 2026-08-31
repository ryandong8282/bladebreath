class_name CombatHUD
extends CanvasLayer


var player: PlayerController
var enemy: EnemyController
var player_health_bar: ProgressBar
var player_posture_bar: ProgressBar
var enemy_health_bar: ProgressBar
var enemy_posture_bar: ProgressBar
var style_label: Label
var edge_label: Label
var telegraph_label: Label
var log_label: Label
var result_panel: PanelContainer
var result_label: Label
var telegraph_serial: int = 0
var log_serial: int = 0


func _ready() -> void:
	layer = 10
	_build_hud()


func bind_actors(player_value: PlayerController, enemy_value: EnemyController) -> void:
	player = player_value
	enemy = enemy_value

	player.health_changed.connect(_on_player_health)
	player.posture_changed.connect(_on_player_posture)
	player.edge_changed.connect(_on_player_edge)
	player.style_changed.connect(_on_style_changed)
	player.combat_log.connect(_on_combat_log)

	enemy.health_changed.connect(_on_enemy_health)
	enemy.posture_changed.connect(_on_enemy_posture)
	enemy.telegraph_started.connect(_on_telegraph_started)
	enemy.attack_resolved.connect(_on_attack_resolved)
	enemy.combat_log.connect(_on_combat_log)

	_on_player_health(player.health, player.max_health)
	_on_player_posture(player.posture, player.max_posture)
	_on_player_edge(player.edge, player.max_edge)
	_on_style_changed(player.current_style)
	_on_enemy_health(enemy.health, enemy.max_health)
	_on_enemy_posture(enemy.posture, enemy.max_posture)


func show_result(title: String, detail: String) -> void:
	result_label.text = "%s\n%s\n\n按 R 或攻击键重新试刃" % [title, detail]
	result_panel.visible = true


func _build_hud() -> void:
	var root := Control.new()
	root.name = "HUDRoot"
	root.anchor_right = 1.0
	root.anchor_bottom = 1.0
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)

	var title := _make_label("无铭：漳城夜 · 校刀院灰盒", 22, HORIZONTAL_ALIGNMENT_LEFT)
	title.position = Vector2(26.0, 20.0)
	title.size = Vector2(390.0, 34.0)
	title.add_theme_color_override("font_color", Color(0.82, 0.93, 0.92, 0.96))
	root.add_child(title)

	var enemy_panel := _make_panel(Vector2(430.0, 108.0))
	enemy_panel.anchor_left = 0.5
	enemy_panel.anchor_right = 0.5
	enemy_panel.offset_left = -215.0
	enemy_panel.offset_right = 215.0
	enemy_panel.offset_top = 22.0
	enemy_panel.offset_bottom = 130.0
	root.add_child(enemy_panel)
	var enemy_box := _make_vbox(enemy_panel, 12)
	enemy_box.add_child(_make_label("造像署执刃者", 20, HORIZONTAL_ALIGNMENT_CENTER))
	enemy_health_bar = _make_bar("生命", Color(0.72, 0.21, 0.15, 1.0))
	enemy_posture_bar = _make_bar("架势", Color(0.87, 0.64, 0.22, 1.0))
	enemy_box.add_child(enemy_health_bar)
	enemy_box.add_child(enemy_posture_bar)

	var player_panel := _make_panel(Vector2(356.0, 160.0))
	player_panel.anchor_top = 1.0
	player_panel.anchor_bottom = 1.0
	player_panel.offset_left = 26.0
	player_panel.offset_right = 382.0
	player_panel.offset_top = -186.0
	player_panel.offset_bottom = -26.0
	root.add_child(player_panel)
	var player_box := _make_vbox(player_panel, 12)
	style_label = _make_label("长刀 · 听刃", 21, HORIZONTAL_ALIGNMENT_LEFT)
	player_box.add_child(style_label)
	player_health_bar = _make_bar("生命", Color(0.72, 0.21, 0.15, 1.0))
	player_posture_bar = _make_bar("架势", Color(0.84, 0.67, 0.30, 1.0))
	player_box.add_child(player_health_bar)
	player_box.add_child(player_posture_bar)
	edge_label = _make_label("锋意 ◇◇◇", 18, HORIZONTAL_ALIGNMENT_LEFT)
	player_box.add_child(edge_label)

	telegraph_label = _make_label("", 25, HORIZONTAL_ALIGNMENT_CENTER)
	telegraph_label.anchor_left = 0.5
	telegraph_label.anchor_right = 0.5
	telegraph_label.offset_left = -310.0
	telegraph_label.offset_right = 310.0
	telegraph_label.offset_top = 152.0
	telegraph_label.offset_bottom = 198.0
	root.add_child(telegraph_label)

	var hint := _make_label(
		"WASD 移动 · J 攻 · K 守/弹 · Space 闪\nQ/U 技一 · E/I 技二 · Tab 换式 · R 重置 · F2 触屏",
		15,
		HORIZONTAL_ALIGNMENT_RIGHT
	)
	hint.anchor_left = 1.0
	hint.anchor_right = 1.0
	hint.offset_left = -520.0
	hint.offset_right = -24.0
	hint.offset_top = 22.0
	hint.offset_bottom = 76.0
	hint.add_theme_color_override("font_color", Color(0.78, 0.82, 0.82, 0.82))
	root.add_child(hint)

	log_label = _make_label("靠近执刃者，读他的刀。", 18, HORIZONTAL_ALIGNMENT_CENTER)
	log_label.anchor_left = 0.5
	log_label.anchor_right = 0.5
	log_label.anchor_top = 1.0
	log_label.anchor_bottom = 1.0
	log_label.offset_left = -380.0
	log_label.offset_right = 380.0
	log_label.offset_top = -62.0
	log_label.offset_bottom = -22.0
	root.add_child(log_label)

	result_panel = _make_panel(Vector2(520.0, 230.0))
	result_panel.anchor_left = 0.5
	result_panel.anchor_right = 0.5
	result_panel.anchor_top = 0.5
	result_panel.anchor_bottom = 0.5
	result_panel.offset_left = -260.0
	result_panel.offset_right = 260.0
	result_panel.offset_top = -115.0
	result_panel.offset_bottom = 115.0
	result_panel.visible = false
	root.add_child(result_panel)
	result_label = _make_label("", 28, HORIZONTAL_ALIGNMENT_CENTER)
	result_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	result_panel.add_child(result_label)


func _make_panel(minimum_size: Vector2) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.custom_minimum_size = minimum_size
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.025, 0.045, 0.052, 0.86)
	style.border_color = Color(0.34, 0.55, 0.56, 0.48)
	style.set_border_width_all(1)
	style.set_corner_radius_all(8)
	style.shadow_color = Color(0.0, 0.0, 0.0, 0.38)
	style.shadow_size = 8
	panel.add_theme_stylebox_override("panel", style)
	return panel


func _make_vbox(panel: PanelContainer, margin: int) -> VBoxContainer:
	var margin_container := MarginContainer.new()
	margin_container.add_theme_constant_override("margin_left", margin)
	margin_container.add_theme_constant_override("margin_right", margin)
	margin_container.add_theme_constant_override("margin_top", margin)
	margin_container.add_theme_constant_override("margin_bottom", margin)
	panel.add_child(margin_container)
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 5)
	margin_container.add_child(box)
	return box


func _make_label(text_value: String, font_size: int, alignment: int) -> Label:
	var label := Label.new()
	label.text = text_value
	label.horizontal_alignment = alignment
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", Color(0.93, 0.96, 0.95, 0.96))
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return label


func _make_bar(title: String, fill_color: Color) -> ProgressBar:
	var bar := ProgressBar.new()
	bar.custom_minimum_size = Vector2(0.0, 19.0)
	bar.show_percentage = false
	bar.tooltip_text = title
	var background := StyleBoxFlat.new()
	background.bg_color = Color(0.02, 0.025, 0.028, 0.92)
	background.set_corner_radius_all(4)
	var fill := StyleBoxFlat.new()
	fill.bg_color = fill_color
	fill.set_corner_radius_all(4)
	bar.add_theme_stylebox_override("background", background)
	bar.add_theme_stylebox_override("fill", fill)
	return bar


func _set_bar(bar: ProgressBar, current: float, maximum: float) -> void:
	bar.max_value = maximum
	bar.value = current


func _on_player_health(current: float, maximum: float) -> void:
	_set_bar(player_health_bar, current, maximum)


func _on_player_posture(current: float, maximum: float) -> void:
	_set_bar(player_posture_bar, current, maximum)


func _on_enemy_health(current: float, maximum: float) -> void:
	_set_bar(enemy_health_bar, current, maximum)


func _on_enemy_posture(current: float, maximum: float) -> void:
	_set_bar(enemy_posture_bar, current, maximum)
	if current <= 0.01:
		telegraph_serial += 1
		telegraph_label.text = "破势 · 靠近后按攻击处决"
		telegraph_label.add_theme_color_override("font_color", Color(1.0, 0.74, 0.26, 1.0))
	elif telegraph_label.text.begins_with("破势"):
		telegraph_serial += 1
		telegraph_label.text = ""


func _on_player_edge(current: int, maximum: int) -> void:
	var glyphs := ""
	for index in range(maximum):
		glyphs += "◆" if index < current else "◇"
	edge_label.text = "锋意 %s" % glyphs


func _on_style_changed(style) -> void:
	if style == null:
		return
	style_label.text = "长刀 · %s ｜ %s获锋意" % [style.display_name, style.edge_source_text]
	style_label.add_theme_color_override("font_color", style.accent_color)


func _on_telegraph_started(title: String, hint: String, color: Color, duration: float) -> void:
	telegraph_serial += 1
	var serial := telegraph_serial
	telegraph_label.text = "%s  ·  %s" % [title, hint]
	telegraph_label.add_theme_color_override("font_color", color)
	telegraph_label.modulate = Color.WHITE
	var tween := create_tween()
	tween.tween_property(telegraph_label, "modulate:a", 0.42, maxf(0.08, duration * 0.72))
	tween.tween_property(telegraph_label, "modulate:a", 1.0, maxf(0.05, duration * 0.28))
	await get_tree().create_timer(duration + 0.20).timeout
	if serial == telegraph_serial:
		telegraph_label.text = ""


func _on_attack_resolved(_title: String, _result: int) -> void:
	pass


func _on_combat_log(message: String) -> void:
	log_serial += 1
	var serial := log_serial
	log_label.text = message
	log_label.modulate = Color.WHITE
	await get_tree().create_timer(1.65).timeout
	if serial == log_serial:
		log_label.modulate = Color(1.0, 1.0, 1.0, 0.72)
