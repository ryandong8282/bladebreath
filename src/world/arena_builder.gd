class_name ArenaBuilder
extends RefCounted


const PrimitiveFactoryScript := preload("res://src/world/primitive_factory.gd")


static func build(parent: Node3D) -> void:
	_build_environment(parent)
	_build_floor(parent)
	_build_boundaries(parent)
	_build_decor(parent)


static func _build_environment(parent: Node3D) -> void:
	var world := WorldEnvironment.new()
	world.name = "WorldEnvironment"
	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color(0.022, 0.021, 0.018, 1.0)
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color(0.38, 0.34, 0.27, 1.0)
	environment.ambient_light_energy = 0.72
	environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	environment.fog_enabled = true
	environment.fog_light_color = Color(0.15, 0.13, 0.10, 1.0)
	environment.fog_density = 0.008
	world.environment = environment
	parent.add_child(world)

	var key_light := DirectionalLight3D.new()
	key_light.name = "MoonKey"
	key_light.rotation_degrees = Vector3(-54.0, -38.0, 0.0)
	key_light.light_color = Color(0.84, 0.80, 0.70, 1.0)
	key_light.light_energy = 1.35
	key_light.shadow_enabled = true
	key_light.directional_shadow_max_distance = 32.0
	parent.add_child(key_light)

	var warm_light := OmniLight3D.new()
	warm_light.name = "WarmAccent"
	warm_light.position = Vector3(-5.5, 3.8, -5.5)
	warm_light.light_color = Color(1.0, 0.48, 0.28, 1.0)
	warm_light.light_energy = 2.2
	warm_light.omni_range = 9.0
	parent.add_child(warm_light)


static func _build_floor(parent: Node3D) -> void:
	PrimitiveFactoryScript.add_static_box(
		parent,
		"ArenaFloor",
		Vector3(22.0, 0.5, 18.0),
		Vector3(0.0, -0.25, 0.0),
		Color(0.105, 0.10, 0.085, 1.0)
	)

	for x in range(-9, 10, 3):
		var line := PrimitiveFactoryScript.box(
			Vector3(0.025, 0.012, 16.0),
			Color(0.21, 0.18, 0.13, 0.52)
		)
		line.position = Vector3(float(x), 0.012, 0.0)
		parent.add_child(line)

	for z in range(-7, 8, 3):
		var line := PrimitiveFactoryScript.box(
			Vector3(20.0, 0.012, 0.025),
			Color(0.21, 0.18, 0.13, 0.52)
		)
		line.position = Vector3(0.0, 0.014, float(z))
		parent.add_child(line)


static func _build_boundaries(parent: Node3D) -> void:
	PrimitiveFactoryScript.add_static_box(
		parent, "NorthWall", Vector3(22.0, 2.0, 0.5),
		Vector3(0.0, 1.0, -9.0), Color(0.078, 0.073, 0.062, 1.0)
	)
	PrimitiveFactoryScript.add_static_box(
		parent, "SouthWall", Vector3(22.0, 2.0, 0.5),
		Vector3(0.0, 1.0, 9.0), Color(0.078, 0.073, 0.062, 1.0)
	)
	PrimitiveFactoryScript.add_static_box(
		parent, "WestWall", Vector3(0.5, 2.0, 18.0),
		Vector3(-11.0, 1.0, 0.0), Color(0.078, 0.073, 0.062, 1.0)
	)
	PrimitiveFactoryScript.add_static_box(
		parent, "EastWall", Vector3(0.5, 2.0, 18.0),
		Vector3(11.0, 1.0, 0.0), Color(0.078, 0.073, 0.062, 1.0)
	)


static func _build_decor(parent: Node3D) -> void:
	var pillar_positions := [
		Vector3(-8.2, 1.3, -6.5), Vector3(8.2, 1.3, -6.5),
		Vector3(-8.2, 1.3, 6.5), Vector3(8.2, 1.3, 6.5),
	]
	for index in range(pillar_positions.size()):
		var pillar := PrimitiveFactoryScript.cylinder(
			0.42, 2.6, Color(0.12, 0.11, 0.092, 1.0), 0.0, 0.92
		)
		pillar.name = "BrokenPillar%d" % index
		pillar.position = pillar_positions[index]
		pillar.rotation_degrees.z = -6.0 if index % 2 == 0 else 5.0
		parent.add_child(pillar)

	var shrine_back := PrimitiveFactoryScript.box(
		Vector3(5.8, 3.2, 0.45), Color(0.11, 0.055, 0.048, 1.0), 0.0, 0.65
	)
	shrine_back.position = Vector3(0.0, 1.6, -8.55)
	parent.add_child(shrine_back)

	for x in [-2.5, 2.5]:
		var lantern := PrimitiveFactoryScript.box(
			Vector3(0.45, 0.75, 0.45), Color(0.44, 0.17, 0.09, 1.0)
		)
		lantern.position = Vector3(x, 1.25, -7.75)
		parent.add_child(lantern)

		var glow := OmniLight3D.new()
		glow.position = Vector3(x, 1.4, -7.7)
		glow.light_color = Color(1.0, 0.36, 0.18, 1.0)
		glow.light_energy = 1.35
		glow.omni_range = 4.0
		parent.add_child(glow)
