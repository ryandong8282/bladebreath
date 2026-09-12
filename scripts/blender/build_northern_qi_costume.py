"""Build the M0 Northern-Qi-inspired costume as lightweight fitted mesh parts.

The MakeHuman OBJ under Logs is a temporary CC0 fitting reference. Only the
procedural costume collection is exported to the Unity FBX.
"""

import json
import math
import pathlib

import bpy
from mathutils import Vector


ROOT = pathlib.Path(r"D:\Dev\bladebreath")
REFERENCE = ROOT / "Logs/makehuman-rest-reference.obj"
OUTPUT_DIR = ROOT / "Assets/_BladeBreath/Art/Characters/Generated/Models"
FBX_PATH = OUTPUT_DIR / "NorthernQiCostume.fbx"
SOURCE_DIR = ROOT / "SourceArt/Characters"
BLEND_PATH = SOURCE_DIR / "NorthernQiCostume.blend"
PLAYER_RENDER = ROOT / "Logs/northern-qi-costume-player.png"
ENEMY_RENDER = ROOT / "Logs/northern-qi-costume-enemy.png"


def material(name, color, metallic=0.0, roughness=0.65):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return value


def mesh_object(name, vertices, faces, mat, collection):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return obj


def loft(name, rings, mat, collection, segments=24, slit_segments=()):
    vertices = []
    faces = []
    for z, radius_x, radius_y in rings:
        for segment in range(segments):
            angle = segment / segments * math.tau
            vertices.append((math.sin(angle) * radius_x, -math.cos(angle) * radius_y, z))
    for row in range(len(rings) - 1):
        for segment in range(segments):
            if row == len(rings) - 2 and segment in slit_segments:
                continue
            next_segment = (segment + 1) % segments
            a = row * segments + segment
            b = row * segments + next_segment
            c = (row + 1) * segments + segment
            d = (row + 1) * segments + next_segment
            faces.extend(((a, b, c), (b, d, c)))
    obj = mesh_object(name, vertices, faces, mat, collection)
    solidify = obj.modifiers.new("Cloth thickness", "SOLIDIFY")
    solidify.thickness = 0.012
    solidify.offset = 0.0
    bevel = obj.modifiers.new("Soft cloth edges", "BEVEL")
    bevel.width = 0.008
    bevel.segments = 2
    return obj


def curved_panel(
    name, center, width, height, depth, bulge, mat, collection,
    columns=8, rows=8, bottom_scale=1.0, top_scale=1.0,
):
    vertices = []
    faces = []
    for row in range(rows + 1):
        v = row / rows
        z = center[2] + (v - 0.5) * height
        taper = ((1.0 - v) * bottom_scale + v * top_scale) * (0.96 + 0.04 * math.sin(v * math.pi))
        for column in range(columns + 1):
            u = column / columns
            x_norm = u * 2.0 - 1.0
            x = center[0] + x_norm * width * 0.5 * taper
            y = center[1] - bulge * math.cos(abs(x_norm) * math.pi * 0.5)
            vertices.append((x, y, z))
    stride = columns + 1
    for row in range(rows):
        for column in range(columns):
            a = row * stride + column
            b = a + 1
            c = a + stride
            d = c + 1
            faces.extend(((a, b, c), (b, d, c)))
    obj = mesh_object(name, vertices, faces, mat, collection)
    solidify = obj.modifiers.new("Panel thickness", "SOLIDIFY")
    solidify.thickness = depth
    solidify.offset = 0.0
    bevel = obj.modifiers.new("Rounded panel edge", "BEVEL")
    bevel.width = min(0.012, depth * 0.45)
    bevel.segments = 2
    return obj


def shoulder_shell(name, side, z_offset, mat, collection):
    columns = 10
    rows = 6
    vertices = []
    faces = []
    inner_x = 0.15 * side
    outer_x = 0.38 * side
    for row in range(rows + 1):
        v = row / rows
        y_norm = v * 2.0 - 1.0
        for column in range(columns + 1):
            u = column / columns
            x = inner_x + (outer_x - inner_x) * u
            z = 1.43 - u * 0.105 - abs(y_norm) ** 1.6 * 0.035 + z_offset
            y = y_norm * (0.12 - u * 0.012)
            vertices.append((x, y, z))
    stride = columns + 1
    for row in range(rows):
        for column in range(columns):
            a = row * stride + column
            b = a + 1
            c = a + stride
            d = c + 1
            faces.extend(((a, b, c), (b, d, c)))
    obj = mesh_object(name, vertices, faces, mat, collection)
    solidify = obj.modifiers.new("Shoulder plate thickness", "SOLIDIFY")
    solidify.thickness = 0.025
    solidify.offset = 0.0
    bevel = obj.modifiers.new("Hammered shoulder edge", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 3
    return obj


def face_mask(name, center, width, height, depth, mat, collection):
    columns = 14
    rows = 16
    vertices = []
    faces = []
    for row in range(rows + 1):
        v = row / rows * 2.0 - 1.0
        ring_width = width * 0.5 * math.sqrt(max(0.14, 1.0 - v * v * 0.72))
        for column in range(columns + 1):
            u = column / columns * 2.0 - 1.0
            x = center[0] + u * ring_width
            z = center[2] + v * height * 0.5
            y = center[1] + depth * (1.0 - u * u) * (1.0 - v * v * 0.35)
            vertices.append((x, y, z))
    stride = columns + 1
    for row in range(rows):
        for column in range(columns):
            a = row * stride + column
            b = a + 1
            c = a + stride
            d = c + 1
            faces.extend(((a, b, c), (b, d, c)))
    obj = mesh_object(name, vertices, faces, mat, collection)
    solidify = obj.modifiers.new("Ceramic thickness", "SOLIDIFY")
    solidify.thickness = 0.018
    solidify.offset = -0.25
    bevel = obj.modifiers.new("Chipped mask edge", "BEVEL")
    bevel.width = 0.007
    bevel.segments = 2
    return obj


def rounded_box(name, location, scale, mat, collection, rotation=(0.0, 0.0, 0.0), bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
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
    modifier.segments = 3
    return obj


def ellipsoid(name, location, scale, mat, collection, segments=24, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def cylinder_between(name, start, end, radius_start, radius_end, mat, collection, vertices=16):
    direction = Vector(end) - Vector(start)
    midpoint = (Vector(start) + Vector(end)) * 0.5
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_start,
        radius2=radius_end,
        depth=direction.length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0.0, 0.0, 1.0)).rotation_difference(direction.normalized())
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Soft edge", "BEVEL")
    bevel.width = 0.008
    bevel.segments = 2
    return obj


def join_objects(name, objects, mat=None):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = objects[0]
    result.name = name
    result.data.name = name + "Mesh"
    if mat is not None:
        result.data.materials.clear()
        result.data.materials.append(mat)
    return result


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.context.scene.unit_settings.system = "METRIC"
bpy.context.scene.unit_settings.scale_length = 1.0

reference_collection = bpy.data.collections.new("CC0 Fitting Reference")
bpy.context.scene.collection.children.link(reference_collection)
bpy.ops.wm.obj_import(filepath=str(REFERENCE), forward_axis="NEGATIVE_Z", up_axis="Y")
for obj in list(bpy.context.selected_objects):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    reference_collection.objects.link(obj)
    obj.scale = (0.1, 0.1, 0.1)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

cloth_player = material("Old indigo military cloth", (0.055, 0.075, 0.09), roughness=0.86)
cloth_enemy = material("Statue office cinnabar cloth", (0.22, 0.045, 0.032), roughness=0.82)
iron = material("Hammered dark iron", (0.13, 0.14, 0.14), metallic=0.72, roughness=0.48)
bronze = material("Worn bronze binding", (0.34, 0.22, 0.095), metallic=0.62, roughness=0.5)
ceramic = material("Ash ceramic mask", (0.48, 0.43, 0.34), metallic=0.0, roughness=0.72)
soot = material("Soot recess", (0.012, 0.009, 0.008), metallic=0.05, roughness=0.94)
skin = material("Reference skin", (0.42, 0.25, 0.18), roughness=0.8)
hair = material("Bound black hair", (0.012, 0.015, 0.018), roughness=0.9)

for obj in reference_collection.objects:
    obj.data.materials.clear()
    key = obj.name.lower()
    obj.data.materials.append(
        hair if "hair" in key else cloth_player if "suit" in key else iron if "shoe" in key else skin
    )

costume = bpy.data.collections.new("Northern Qi Costume")
bpy.context.scene.collection.children.link(costume)

torso = loft(
    "TunicTorso",
    ((0.93, 0.225, 0.145), (1.08, 0.205, 0.135), (1.29, 0.235, 0.15), (1.43, 0.175, 0.12)),
    cloth_player,
    costume,
)

# Four separate lower panels keep a knee-length robe silhouette while allowing
# Unity to parent the side panels to their corresponding thighs.
skirt_front_left = curved_panel(
    "SkirtFrontLeft", (0.105, 0.115, 0.73), 0.205, 0.52, 0.016, -0.035,
    cloth_player, costume, bottom_scale=1.08, top_scale=0.96)
skirt_front_left.rotation_euler[0] = math.radians(2)
skirt_front_left.rotation_euler[2] = math.radians(-1.5)
skirt_front_right = curved_panel(
    "SkirtFrontRight", (-0.105, 0.115, 0.73), 0.205, 0.52, 0.016, -0.035,
    cloth_player, costume, bottom_scale=1.08, top_scale=0.96)
skirt_front_right.rotation_euler[0] = math.radians(2)
skirt_front_right.rotation_euler[2] = math.radians(1.5)
skirt_back = curved_panel("SkirtBack", (0.0, -0.135, 0.75), 0.39, 0.48, 0.018, 0.03, cloth_player, costume)
skirt_back.rotation_euler[2] = math.pi
skirt_left = curved_panel("SkirtLeft", (0.185, 0.0, 0.76), 0.25, 0.46, 0.016, 0.025, cloth_player, costume, 6, 7)
skirt_left.rotation_euler[2] = math.radians(-90)
skirt_right = curved_panel("SkirtRight", (-0.185, 0.0, 0.76), 0.25, 0.46, 0.016, 0.025, cloth_player, costume, 6, 7)
skirt_right.rotation_euler[2] = math.radians(90)

# A long split mantle and shoulder streamers give the fighter a readable
# historical-military silhouette in the combat camera. They are broad cloth
# forms rather than skin-tight modern clothing and remain cheap enough for iOS.
mantle_back = curved_panel(
    "MantleBack", (0.0, -0.175, 1.02), 0.48, 0.72, 0.018, 0.045,
    cloth_player, costume, columns=10, rows=12, bottom_scale=1.12, top_scale=0.88)
mantle_shoulder_left = curved_panel(
    "MantleShoulderLeft", (0.285, 0.0, 1.235), 0.13, 0.22, 0.014, 0.02,
    cloth_player, costume, columns=5, rows=6, bottom_scale=0.86, top_scale=1.04)
mantle_shoulder_left.rotation_euler[2] = math.radians(-18)
mantle_shoulder_right = curved_panel(
    "MantleShoulderRight", (-0.285, 0.0, 1.235), 0.13, 0.22, 0.014, 0.02,
    cloth_player, costume, columns=5, rows=6, bottom_scale=0.86, top_scale=1.04)
mantle_shoulder_right.rotation_euler[2] = math.radians(18)
pendant_left = curved_panel(
    "Belt pendant left", (0.105, 0.155, 0.76), 0.12, 0.48, 0.012, -0.025,
    cloth_player, costume, columns=5, rows=8, bottom_scale=0.72, top_scale=1.0)
pendant_right = curved_panel(
    "Belt pendant right", (-0.105, 0.155, 0.76), 0.12, 0.48, 0.012, -0.025,
    cloth_player, costume, columns=5, rows=8, bottom_scale=0.72, top_scale=1.0)
belt_pendant = join_objects("BeltPendant", [pendant_left, pendant_right], cloth_player)

collar = loft("RoundCollar", ((1.405, 0.112, 0.09), (1.455, 0.098, 0.08)), bronze, costume, segments=24)
belt = loft("Belt", ((0.925, 0.224, 0.146), (0.985, 0.224, 0.146)), bronze, costume, segments=24)
placket = rounded_box("FrontPlacket", (0.0, 0.16, 1.19), (0.026, 0.012, 0.22), bronze, costume, bevel=0.01)
nameplate = rounded_box("BlankNameplate", (0.0, 0.185, 1.15), (0.07, 0.012, 0.11), ceramic, costume, bevel=0.014)

lamellar_front_parts = []
for row in range(5):
    z = 1.34 - row * 0.088
    columns = 6 if row < 3 else 5
    for column in range(columns):
        x = (column - (columns - 1) * 0.5) * 0.064
        plate = rounded_box(
            f"Front lamella {row + 1}-{column + 1}",
            (x, 0.185 + 0.01 * math.cos(x * 8.0), z),
            (0.026, 0.012, 0.037),
            iron,
            costume,
            rotation=(0.0, 0.0, math.radians((column - 2) * -2.0)),
            bevel=0.009,
        )
        lamellar_front_parts.append(plate)
lamellar_front = join_objects("LamellarFront", lamellar_front_parts, iron)

lamellar_back_parts = []
for row in range(4):
    z = 1.32 - row * 0.092
    for column in range(6):
        x = (column - 2.5) * 0.064
        lamellar_back_parts.append(rounded_box(
            f"Back lamella {row + 1}-{column + 1}", (x, -0.17, z),
            (0.026, 0.011, 0.038), iron, costume, bevel=0.008))
lamellar_back = join_objects("LamellarBack", lamellar_back_parts, iron)

# Small bronze lacing bosses break up the armor mass under close lighting and
# make each lamella read as an assembled plate instead of a flat box.
front_rivets = []
for row in range(5):
    z = 1.34 - row * 0.088
    columns = 6 if row < 3 else 5
    for column in range(columns):
        x = (column - (columns - 1) * 0.5) * 0.064
        front_rivets.append(ellipsoid(
            f"Front rivet {row}-{column}", (x, 0.201, z + 0.018),
            (0.011, 0.008, 0.011), bronze, costume, segments=12, rings=6))
back_rivets = []
for row in range(4):
    z = 1.32 - row * 0.092
    for column in range(6):
        x = (column - 2.5) * 0.064
        back_rivets.append(ellipsoid(
            f"Back rivet {row}-{column}", (x, -0.184, z + 0.018),
            (0.011, 0.008, 0.011), bronze, costume, segments=12, rings=6))
lamellar_rivets_front = join_objects("LamellarRivetsFront", front_rivets, bronze)
lamellar_rivets_back = join_objects("LamellarRivetsBack", back_rivets, bronze)

shoulder_left_parts = [shoulder_shell("Shoulder L upper", 1.0, 0.0, iron, costume),
                       shoulder_shell("Shoulder L lower", 1.0, -0.047, iron, costume)]
shoulder_right_parts = [shoulder_shell("Shoulder R upper", -1.0, 0.0, iron, costume),
                        shoulder_shell("Shoulder R lower", -1.0, -0.047, iron, costume)]
shoulder_left = join_objects("ShoulderGuardLeft", shoulder_left_parts, iron)
shoulder_right = join_objects("ShoulderGuardRight", shoulder_right_parts, iron)

sleeve_left = cylinder_between(
    "SleeveLeft", (0.205, 0.0, 1.375), (0.335, 0.01, 1.245),
    0.092, 0.076, cloth_player, costume, vertices=20)
sleeve_right = cylinder_between(
    "SleeveRight", (-0.205, 0.0, 1.375), (-0.335, 0.01, 1.245),
    0.092, 0.076, cloth_player, costume, vertices=20)

bracer_left = cylinder_between("BracerLeft", (0.335, 0.01, 1.225), (0.43, 0.12, 1.125), 0.058, 0.047, iron, costume)
bracer_right = cylinder_between("BracerRight", (-0.335, 0.01, 1.225), (-0.43, 0.12, 1.125), 0.058, 0.047, iron, costume)

knee_left = rounded_box(
    "KneeGuardLeft", (0.115, 0.115, 0.51), (0.095, 0.035, 0.13),
    iron, costume, rotation=(math.radians(-6), 0.0, math.radians(-4)), bevel=0.028)
knee_right = rounded_box(
    "KneeGuardRight", (-0.115, 0.115, 0.51), (0.095, 0.035, 0.13),
    iron, costume, rotation=(math.radians(-6), 0.0, math.radians(4)), bevel=0.028)

hair_knot = ellipsoid("HairKnot", (0.0, 0.015, 1.775), (0.052, 0.048, 0.068), hair, costume)
hair_band = loft("HairBand", ((1.72, 0.072, 0.068), (1.745, 0.072, 0.068)), bronze, costume, segments=20)

mask = face_mask("EnemyMask", (0.0, 0.102, 1.62), 0.215, 0.25, 0.048, ceramic, costume)
eye_left = rounded_box("EnemyEyeSlitLeft", (0.047, 0.15, 1.65), (0.034, 0.008, 0.009), soot, costume,
                       rotation=(math.radians(-4), 0.0, math.radians(-5)), bevel=0.006)
eye_right = rounded_box("EnemyEyeSlitRight", (-0.047, 0.15, 1.65), (0.034, 0.008, 0.009), soot, costume,
                        rotation=(math.radians(-4), 0.0, math.radians(5)), bevel=0.006)
nose_ridge = ellipsoid("EnemyMaskNose", (0.0, 0.164, 1.605), (0.018, 0.018, 0.044), ceramic, costume, 16, 8)
enemy_face = join_objects("EnemyFaceDetails", [mask, eye_left, eye_right, nose_ridge])
bpy.ops.object.select_all(action="DESELECT")
enemy_face.select_set(True)
bpy.context.view_layer.objects.active = enemy_face
bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")

for obj in costume.objects:
    obj["unity_bone"] = {
        "SkirtLeft": "thigh.L", "SkirtRight": "thigh.R",
        "ShoulderGuardLeft": "upper_arm.L", "ShoulderGuardRight": "upper_arm.R",
        "MantleShoulderLeft": "upper_arm.L", "MantleShoulderRight": "upper_arm.R",
        "SleeveLeft": "upper_arm.L", "SleeveRight": "upper_arm.R",
        "BracerLeft": "forearm.L", "BracerRight": "forearm.R",
        "KneeGuardLeft": "thigh.L", "KneeGuardRight": "thigh.R",
        "HairKnot": "head", "HairBand": "head", "EnemyFaceDetails": "head",
    }.get(obj.name, "hips" if obj.name.startswith("Skirt") or obj.name in ("Belt", "BeltPendant") else "chest")

# Apply the authored bevel/solidify modifiers and give every costume piece a
# real UV layout. Unity's worn-metal and patched-cloth surfaces then affect the
# geometry instead of falling back to a flat material color.
for obj in list(costume.objects):
    if obj.type != "MESH":
        continue
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.025)
    bpy.ops.object.mode_set(mode="OBJECT")

enemy_face.hide_render = True

# Presentation scene for proof renders.
bpy.ops.mesh.primitive_plane_add(size=20, location=(0.0, 0.0, 0.0))
floor = bpy.context.object
floor.name = "Preview floor"
floor.data.materials.append(material("Preview floor", (0.035, 0.04, 0.047), roughness=0.94))

bpy.ops.object.light_add(type="AREA", location=(2.4, 3.1, 4.1))
key = bpy.context.object
key.data.energy = 900
key.data.shape = "DISK"
key.data.size = 3.0
key.data.color = (0.65, 0.75, 1.0)
look_at(key, (0.0, 0.0, 1.0))
bpy.ops.object.light_add(type="AREA", location=(-2.0, 1.0, 2.1))
fill = bpy.context.object
fill.data.energy = 520
fill.data.size = 2.5
fill.data.color = (1.0, 0.38, 0.13)
look_at(fill, (0.0, 0.0, 1.0))
bpy.ops.object.light_add(type="AREA", location=(0.0, 2.5, 3.2))
rim = bpy.context.object
rim.data.energy = 700
rim.data.size = 2.0
rim.data.color = (0.38, 0.52, 1.0)
look_at(rim, (0.0, 0.0, 1.15))

bpy.ops.object.camera_add(location=(2.65, 4.7, 2.55))
camera = bpy.context.object
camera.data.type = "ORTHO"
camera.data.ortho_scale = 2.15
look_at(camera, (0.0, 0.0, 0.95))
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("Grey kiln preview world")
scene.world.color = (0.008, 0.01, 0.014)

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
SOURCE_DIR.mkdir(parents=True, exist_ok=True)
scene.render.filepath = str(PLAYER_RENDER)
bpy.ops.render.render(write_still=True)

enemy_face.hide_render = False
for obj in (torso, skirt_front_left, skirt_front_right, skirt_back, skirt_left, skirt_right):
    obj.data.materials.clear()
    obj.data.materials.append(cloth_enemy)
scene.render.filepath = str(ENEMY_RENDER)
bpy.ops.render.render(write_still=True)

# Export only the fitted costume pieces. The fitting body remains a documented
# reference in the .blend source and is excluded from the Unity FBX.
bpy.ops.object.select_all(action="DESELECT")
for obj in costume.objects:
    obj.hide_render = False
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
    bake_space_transform=True,
    use_mesh_modifiers=True,
    add_leaf_bones=False,
    path_mode="STRIP",
)

bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)

stats = {}
for obj in costume.objects:
    if obj.type == "MESH":
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        stats[obj.name] = {"vertices": len(mesh.vertices), "triangles": len(mesh.loop_triangles)}
        evaluated.to_mesh_clear()
(OUTPUT_DIR / "NorthernQiCostume.stats.json").write_text(json.dumps(stats, indent=2), encoding="utf-8")
print(FBX_PATH)
