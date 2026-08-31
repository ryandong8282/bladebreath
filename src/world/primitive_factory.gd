class_name PrimitiveFactory
extends RefCounted


static func material(
	color: Color,
	metallic: float = 0.0,
	roughness: float = 0.78,
	emission: Color = Color(0, 0, 0, 1),
	emission_energy: float = 0.0
) -> StandardMaterial3D:
	var value := StandardMaterial3D.new()
	value.albedo_color = color
	value.metallic = metallic
	value.roughness = roughness
	if color.a < 0.999:
		value.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		value.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	value.emission_enabled = emission_energy > 0.0
	if value.emission_enabled:
		value.emission = emission
		value.emission_energy_multiplier = emission_energy
	return value


static func box(
	size: Vector3,
	color: Color,
	metallic: float = 0.0,
	roughness: float = 0.78
) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	instance.mesh = mesh
	instance.material_override = material(color, metallic, roughness)
	return instance


static func cylinder(
	radius: float,
	height: float,
	color: Color,
	metallic: float = 0.0,
	roughness: float = 0.78
) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	instance.mesh = mesh
	instance.material_override = material(color, metallic, roughness)
	return instance


static func capsule(radius: float, height: float, color: Color) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	var mesh := CapsuleMesh.new()
	mesh.radius = radius
	mesh.height = height
	instance.mesh = mesh
	instance.material_override = material(color)
	return instance


static func ring(
	outer_radius: float,
	inner_radius: float,
	color: Color
) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	var mesh := TorusMesh.new()
	mesh.outer_radius = outer_radius
	mesh.inner_radius = inner_radius
	mesh.rings = 32
	mesh.ring_segments = 8
	instance.mesh = mesh
	instance.material_override = material(color)
	return instance


static func add_static_box(
	parent: Node,
	name_value: String,
	size: Vector3,
	position_value: Vector3,
	color: Color,
	visible: bool = true
) -> StaticBody3D:
	var body := StaticBody3D.new()
	body.name = name_value
	body.position = position_value
	parent.add_child(body)

	var shape := CollisionShape3D.new()
	var box_shape := BoxShape3D.new()
	box_shape.size = size
	shape.shape = box_shape
	body.add_child(shape)

	if visible:
		var mesh := box(size, color)
		body.add_child(mesh)
	return body
