"""Build the M0 project-owned Northern-Qi-inspired ring-pommel longblade.

The model is a mobile-readable fictional military transformation, not an
archaeological reconstruction. Its primary physical reference is The Met
30.65.2, a Chinese sword dated ca. 600 with a ring pommel and 102.2 cm total
length. Blender +Y is authored as Unity +Z so the existing right-hand socket,
trail anchors and combat timing do not change.
"""

import hashlib
import json
import math
import pathlib

import bpy
from mathutils import Vector


ROOT = pathlib.Path(r"D:\Dev\bladebreath")
SCRIPT_PATH = ROOT / "scripts/blender/build_northern_qi_longblade.py"
OUTPUT_DIR = ROOT / "Assets/_BladeBreath/Art/Weapons/Generated"
SOURCE_DIR = ROOT / "SourceArt/Weapons"
FBX_PATH = OUTPUT_DIR / "NorthernQiLongblade.fbx"
BLEND_PATH = SOURCE_DIR / "NorthernQiLongblade.blend"
STATS_PATH = OUTPUT_DIR / "NorthernQiLongblade.stats.json"
MANIFEST_PATH = OUTPUT_DIR / "SOURCE_MANIFEST.json"
HERO_PREVIEW = ROOT / "Logs/northern-qi-longblade-hero.png"
GAME_PREVIEW = ROOT / "Logs/northern-qi-longblade-game-angle.png"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def material(name, color, metallic=0.0, roughness=0.55):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return value


def mesh_object(name, vertices, faces, mat, collection, bevel=0.0):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    if bevel > 0.0:
        modifier = obj.modifiers.new("Forged edge softening", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def extruded_profile(name, profile, half_thickness, mat, collection, bevel=0.0):
    """Extrude an X/Y silhouette through Blender Z thickness."""
    vertices = [(x, y, -half_thickness) for x, y in profile]
    vertices += [(x, y, half_thickness) for x, y in profile]
    count = len(profile)
    faces = []
    # Broad faces use opposite winding. Concavity is mild and Blender's exporter
    # triangulates the n-gons deterministically.
    faces.append(tuple(range(count - 1, -1, -1)))
    faces.append(tuple(range(count, count * 2)))
    for index in range(count):
        next_index = (index + 1) % count
        faces.append((index, next_index, next_index + count, index + count))
    return mesh_object(name, vertices, faces, mat, collection, bevel)


def rounded_box(name, location, scale, mat, collection, bevel=0.004):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    modifier = obj.modifiers.new("Worn rounded edge", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    return obj


def cylinder_y(name, center_y, length, radius_start, radius_end, mat, collection, vertices=12):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_start,
        radius2=radius_end,
        depth=length,
        location=(0.0, center_y, 0.0),
        rotation=(math.pi * 0.5, 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Handled edge", "BEVEL")
    bevel.width = 0.0025
    bevel.segments = 2
    return obj


def torus_xy(name, center_y, major_radius, minor_radius, mat, collection, major_segments=20, minor_segments=6):
    bpy.ops.mesh.primitive_torus_add(
        align="WORLD",
        major_segments=major_segments,
        minor_segments=minor_segments,
        location=(0.0, center_y, 0.0),
        major_radius=major_radius,
        minor_radius=minor_radius,
    )
    obj = bpy.context.object
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def grip_wrap(name, start_y, end_y, radius, mat, collection):
    curve = bpy.data.curves.new(name + "Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 1
    curve.bevel_depth = 0.0024
    curve.bevel_resolution = 1
    curve.resolution_u = 1
    spline = curve.splines.new("POLY")
    point_count = 72
    turns = 5.25
    spline.points.add(point_count - 1)
    for index in range(point_count):
        t = index / (point_count - 1)
        angle = t * turns * math.tau
        spline.points[index].co = (
            math.cos(angle) * radius,
            start_y + (end_y - start_y) * t,
            math.sin(angle) * radius,
            1.0,
        )
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def apply_and_convert(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if obj.type == "CURVE":
        bpy.ops.object.convert(target="MESH")
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)
    return obj


def join_group(objects, name):
    if len(objects) == 1:
        objects[0].name = name
        return objects[0]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    return joined


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


# Start from an isolated clean file. The script is intended for background use
# so the user's open environment .blend is never touched.
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0

weapon = bpy.data.collections.new("NorthernQiLongblade_Runtime")
scene.collection.children.link(weapon)

steel = material("Blackened forged steel", (0.16, 0.18, 0.19), 0.88, 0.29)
edge = material("Polished cutting edge", (0.62, 0.67, 0.66), 0.96, 0.16)
bronze = material("Aged gilt bronze", (0.38, 0.24, 0.095), 0.76, 0.34)
grip = material("Indigo cord and leather", (0.055, 0.068, 0.075), 0.06, 0.78)

# Total length is 1.03 m. The straight single edge keeps the silhouette clear at
# the isometric game camera without borrowing the deep curve of later blades.
blade_profile = [
    (-0.034, 0.120),
    (0.039, 0.120),
    (0.036, 0.690),
    (0.029, 0.790),
    (0.013, 0.885),
    (-0.005, 0.948),
    (-0.031, 0.792),
]
steel_parts = [extruded_profile("BladeBody", blade_profile, 0.0115, steel, weapon, 0.0022)]

# A raised spine and shallow fuller create highlights during motion while using
# geometry instead of a large texture set.
steel_parts.append(rounded_box("SpineRidge", (-0.027, 0.490, 0.0), (0.005, 0.330, 0.013), steel, weapon, 0.002))
steel_parts.append(rounded_box("ShallowFuller", (-0.010, 0.500, -0.0122), (0.006, 0.285, 0.0012), steel, weapon, 0.001))
steel_parts.append(rounded_box("ShallowFullerBack", (-0.010, 0.500, 0.0122), (0.006, 0.285, 0.0012), steel, weapon, 0.001))

edge_profile = [
    (0.027, 0.145),
    (0.0395, 0.145),
    (0.0365, 0.690),
    (0.0295, 0.792),
    (0.013, 0.885),
    (-0.005, 0.948),
    (0.003, 0.900),
    (0.018, 0.806),
    (0.026, 0.685),
]
edge_parts = [extruded_profile("CuttingEdge", edge_profile, 0.0124, edge, weapon, 0.0011)]

bronze_parts = []
bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, location=(0.0, 0.103, 0.0))
guard = bpy.context.object
guard.name = "CompactOvalGuard"
for owner in list(guard.users_collection):
    owner.objects.unlink(guard)
weapon.objects.link(guard)
guard.scale = (0.080, 0.014, 0.030)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
guard.data.materials.append(bronze)
bronze_parts.append(guard)
bronze_parts.append(cylinder_y("BladeCollar", 0.126, 0.027, 0.031, 0.034, bronze, weapon, 16))
bronze_parts.append(cylinder_y("GripCollar", 0.084, 0.025, 0.030, 0.027, bronze, weapon, 16))
bronze_parts.append(cylinder_y("PommelCollar", -0.107, 0.022, 0.028, 0.031, bronze, weapon, 16))

# The ring is the strongest period-readable feature. The small opposing wedges
# are a mobile-scale abstraction of confronted-animal ring decoration.
bronze_parts.append(torus_xy("RingPommel", -0.153, 0.038, 0.0075, bronze, weapon, 20, 6))
bronze_parts.append(extruded_profile(
    "RingBridgeLeft", [(-0.032, -0.151), (-0.004, -0.151), (-0.015, -0.171)],
    0.0065, bronze, weapon, 0.0015,
))
bronze_parts.append(extruded_profile(
    "RingBridgeRight", [(0.032, -0.151), (0.004, -0.151), (0.015, -0.171)],
    0.0065, bronze, weapon, 0.0015,
))

# A blank plate connects the weapon to the project's erased-registry fiction.
# It has no invented inscription and does not pretend to be archaeological.
bronze_parts.append(rounded_box("BlankRegistryPlate", (-0.006, 0.188, -0.0132), (0.016, 0.026, 0.0016), bronze, weapon, 0.0012))
bronze_parts.append(rounded_box("BlankRegistryPlateBack", (-0.006, 0.188, 0.0132), (0.016, 0.026, 0.0016), bronze, weapon, 0.0012))

grip_parts = [
    cylinder_y("GripCore", -0.008, 0.178, 0.025, 0.027, grip, weapon, 12),
    grip_wrap("DiagonalGripWrap", -0.093, 0.077, 0.0275, grip, weapon),
]

for obj in list(weapon.objects):
    apply_and_convert(obj)

export_objects = [
    join_group(steel_parts, "Longblade_Steel"),
    join_group(edge_parts, "Longblade_Edge"),
    join_group(bronze_parts, "Longblade_Bronze"),
    join_group(grip_parts, "Longblade_Grip"),
]

for obj in export_objects:
    for polygon in obj.data.polygons:
        polygon.use_smooth = obj.name != "Longblade_Edge"

# Proof setup. Runtime objects remain in the same authored transform.
bpy.ops.mesh.primitive_plane_add(size=8, location=(0.0, 0.28, -0.22))
floor = bpy.context.object
floor.name = "Preview floor"
floor.data.materials.append(material("Preview charcoal", (0.016, 0.019, 0.022), 0.0, 0.94))

bpy.ops.object.light_add(type="AREA", location=(-1.1, -1.4, 1.45))
key = bpy.context.object
key.name = "Cool forge key"
key.data.energy = 260
key.data.shape = "DISK"
key.data.size = 1.1
key.data.color = (0.56, 0.71, 1.0)
look_at(key, (0.0, 0.38, 0.0))

bpy.ops.object.light_add(type="AREA", location=(1.0, 0.1, 0.6))
rim = bpy.context.object
rim.name = "Warm bronze rim"
rim.data.energy = 180
rim.data.size = 0.85
rim.data.color = (1.0, 0.49, 0.22)
look_at(rim, (0.0, 0.30, 0.0))

bpy.ops.object.light_add(type="AREA", location=(0.0, -0.8, -0.1))
fill = bpy.context.object
fill.data.energy = 70
fill.data.size = 0.6
look_at(fill, (0.0, 0.10, 0.0))

bpy.ops.object.camera_add(location=(0.20, 0.38, 2.55))
camera = bpy.context.object
camera.name = "Weapon proof camera"
camera.data.type = "ORTHO"
camera.data.ortho_scale = 1.23
look_at(camera, (0.0, 0.38, 0.0))
scene.camera = camera

scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new("Weapon proof world")
scene.world.color = (0.005, 0.007, 0.010)
scene.view_settings.look = "AgX - Medium High Contrast"
scene.view_settings.exposure = -0.8

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
SOURCE_DIR.mkdir(parents=True, exist_ok=True)
HERO_PREVIEW.parent.mkdir(parents=True, exist_ok=True)
scene.render.filepath = str(HERO_PREVIEW)
bpy.ops.render.render(write_still=True)

camera.location = (0.95, -1.55, 1.80)
camera.data.ortho_scale = 1.36
look_at(camera, (0.0, 0.37, 0.0))
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.filepath = str(GAME_PREVIEW)
bpy.ops.render.render(write_still=True)

bpy.ops.object.select_all(action="DESELECT")
for obj in export_objects:
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.fbx(
    filepath=str(FBX_PATH),
    use_selection=True,
    object_types={"MESH"},
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    path_mode="STRIP",
)

bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)

stats = {}
triangle_total = 0
vertex_total = 0
depsgraph = bpy.context.evaluated_depsgraph_get()
for obj in export_objects:
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    stats[obj.name] = {"vertices": len(mesh.vertices), "triangles": len(mesh.loop_triangles)}
    vertex_total += len(mesh.vertices)
    triangle_total += len(mesh.loop_triangles)
    evaluated.to_mesh_clear()
stats["TOTAL"] = {
    "objects": len(export_objects),
    "vertices": vertex_total,
    "triangles": triangle_total,
    "authored_bounds_m": {"x": 0.160, "y": 1.030, "z": 0.060},
}
STATS_PATH.write_text(json.dumps(stats, ensure_ascii=False, indent=2), encoding="utf-8")

manifest = {
    "asset": "Zhenjun ring-pommel longblade / 镇军环首长刀",
    "generated_on": "2026-09-02",
    "ownership": "Project-owned procedural mesh; no third-party model geometry included",
    "blender_version": bpy.app.version_string,
    "historical_anchor": {
        "source": "The Metropolitan Museum of Art, object 30.65.2",
        "url": "https://www.metmuseum.org/art/collection/search/23352",
        "record": "Chinese sword with scabbard mounts, ca. 600, Henan, 102.2 cm; ring pommel and P-shaped scabbard mounts",
        "image_license": "Public Domain; reference image kept only in ignored Logs/weapon-reference",
    },
    "fictional_transformation": "Straight single-edged military longblade with a blank erased-registry plate and simplified ring decoration for mobile readability.",
    "gameplay_purpose": "One-handed M0 weapon silhouette; preserves current light/heavy/guard/parry/clash timing and blade trail anchors.",
    "anachronism_check": "No katana curvature or tsuba, European cruciform hilt, fantasy runes, oversized greatsword proportions, or later Ming/Qing decoration. This remains an inspired game prop rather than an exact Northern Qi reconstruction.",
    "open_asset_audit": [
        {
            "source": "Quaternius Modular Weapons Pack",
            "url": "https://quaternius.com/packs/medievalweapons.html",
            "license": "CC0",
            "decision": "Rejected for runtime because the generic European-medieval silhouettes conflict with the period direction.",
        },
        {
            "source": "OpenGameArt Katana by Clint Bellanger",
            "url": "https://opengameart.org/content/katana",
            "license": "CC0",
            "decision": "Rejected for runtime because it is explicitly Japanese and would introduce the wrong guard, sheath and blade language.",
        },
    ],
    "geometry": stats["TOTAL"],
    "runtime_files": [],
    "source_files": [],
}
for path in (FBX_PATH, STATS_PATH):
    manifest["runtime_files"].append({"file": path.name, "bytes": path.stat().st_size, "sha256": sha256(path)})
for path in (BLEND_PATH, SCRIPT_PATH):
    manifest["source_files"].append({
        "file": str(path.relative_to(ROOT)).replace("\\", "/"),
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
    })
MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

print(json.dumps({
    "fbx": str(FBX_PATH),
    "blend": str(BLEND_PATH),
    "hero_preview": str(HERO_PREVIEW),
    "game_preview": str(GAME_PREVIEW),
    "objects": len(export_objects),
    "vertices": vertex_total,
    "triangles": triangle_total,
}, ensure_ascii=False))
