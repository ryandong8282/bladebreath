"""Build the Grey Kiln courtyard landmark kit for the Unity vertical slice.

The kit is an original, low-cost environment composition rather than an
archaeological reconstruction.  It creates the north office gate, a broken
limestone colossus, a brick kiln workshop and a military-register stele group.
"""

import hashlib
import json
import math
import pathlib

import bpy
from mathutils import Vector


ROOT = pathlib.Path(r"D:\Dev\bladebreath")
SCRIPT_PATH = ROOT / "scripts/blender/build_grey_kiln_landmarks.py"
OUTPUT_DIR = ROOT / "Assets/_BladeBreath/Art/Environment/Generated/Models"
SOURCE_DIR = ROOT / "SourceArt/Environment"
FBX_PATH = OUTPUT_DIR / "GreyKilnLandmarks.fbx"
BLEND_PATH = SOURCE_DIR / "GreyKilnLandmarks.blend"
STATS_PATH = OUTPUT_DIR / "GreyKilnLandmarks.stats.json"
MANIFEST_PATH = OUTPUT_DIR / "SOURCE_MANIFEST.json"
PREVIEW_PATH = ROOT / "Logs/grey-kiln-landmarks.png"
AI_COLOSSUS_DIR = SOURCE_DIR / "GeneratedSource"
AI_COLOSSUS_OBJ = AI_COLOSSUS_DIR / "GreyKilnColossusAI.obj"
AI_COLOSSUS_TEXTURE = AI_COLOSSUS_DIR / "texture.png"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def make_material(name, color, roughness=0.72, metallic=0.0, emission=None):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    if emission:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 3.2
    return value


def make_textured_material(name, texture_path, roughness=0.84):
    value = make_material(name, (0.58, 0.54, 0.47), roughness)
    if not texture_path.exists():
        return value
    image = bpy.data.images.load(str(texture_path), check_existing=True)
    texture = value.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.location = (-600.0, 100.0)
    shader = value.node_tree.nodes.get("Principled BSDF")
    value.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return value


def finish(obj, name, material, collection, bevel=0.0):
    obj.name = name
    for owned in list(obj.users_collection):
        owned.objects.unlink(obj)
    collection.objects.link(obj)
    obj.data.materials.append(material)
    if bevel > 0.0:
        modifier = obj.modifiers.new("Worn edge", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def box(name, location, dimensions, material, collection, rotation=(0.0, 0.0, 0.0), bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, collection, bevel)


def cylinder(name, location, radius, depth, material, collection, rotation=(0.0, 0.0, 0.0), vertices=16, bevel=0.03):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    return finish(bpy.context.object, name, material, collection, bevel)


def ellipsoid(name, location, scale, material, collection, rotation=(0.0, 0.0, 0.0), segments=24, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return finish(obj, name, material, collection)


def torus(name, location, major_radius, minor_radius, material, collection, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=32,
        minor_segments=8,
        location=location,
        rotation=rotation,
    )
    return finish(bpy.context.object, name, material, collection)


def join(name, objects, material):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    result = bpy.context.object
    result.name = name
    result.data.materials.clear()
    result.data.materials.append(material)
    return result


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_gate(collection, stone, pale, timber, roof, bronze):
    stone_parts = []
    timber_parts = []
    roof_parts = []
    bronze_parts = []
    # The Unity combat camera looks north from the courtyard. Keep the gate on
    # the wall line so its plinth frames the arena instead of filling the shot.
    base_y = 10.75

    # A deep three-step plinth keeps the closed northern axis legible from the
    # isometric camera while the central opening promises the next area.
    for index, (width, depth, height) in enumerate(((8.8, 2.3, 0.26), (7.9, 1.9, 0.25), (7.0, 1.55, 0.24))):
        stone_parts.append(box(
            f"Gate plinth {index}", (0.0, base_y + index * 0.13, 0.13 + index * 0.24),
            (width, depth, height), stone if index < 2 else pale, collection, bevel=0.055))

    for x in (-3.05, -1.55, 1.55, 3.05):
        timber_parts.append(cylinder(f"Gate pillar {x}", (x, base_y, 2.45), 0.23, 4.2, timber, collection, vertices=12, bevel=0.025))
        stone_parts.append(cylinder(f"Pillar base {x}", (x, base_y, 0.56), 0.43, 0.32, pale, collection, vertices=12, bevel=0.035))
        timber_parts.append(box(f"Pillar collar {x}", (x, base_y, 4.43), (0.64, 0.58, 0.24), timber, collection, bevel=0.035))

    timber_parts.extend([
        box("Gate lower beam", (0.0, base_y, 3.72), (7.15, 0.42, 0.38), timber, collection, bevel=0.045),
        box("Gate upper beam", (0.0, base_y, 4.42), (7.75, 0.5, 0.34), timber, collection, bevel=0.045),
    ])

    # Compact bracket clusters evoke northern timber construction at mobile scale.
    for x in (-3.0, -1.5, 0.0, 1.5, 3.0):
        for tier in range(3):
            width = 0.75 + tier * 0.28
            timber_parts.append(box(
                f"Bracket {x} {tier}", (x, base_y, 4.67 + tier * 0.18),
                (width, 0.62 + tier * 0.14, 0.17), timber, collection,
                rotation=(0.0, 0.0, math.radians((tier - 1) * 2.0)), bevel=0.025))

    # Two broad roof planes plus repeated ribs produce a much richer silhouette
    # without importing high-resolution roof textures or hundreds of draw calls.
    roof_parts.extend([
        box("Gate roof south", (0.0, base_y - 0.45, 5.36), (9.0, 1.75, 0.22), roof, collection,
            rotation=(math.radians(-14.0), 0.0, 0.0), bevel=0.07),
        box("Gate roof north", (0.0, base_y + 0.45, 5.36), (9.0, 1.75, 0.22), roof, collection,
            rotation=(math.radians(14.0), 0.0, 0.0), bevel=0.07),
        cylinder("Gate roof ridge", (0.0, base_y, 5.61), 0.16, 9.25, roof, collection,
            rotation=(0.0, math.radians(90.0), 0.0), vertices=12, bevel=0.02),
    ])
    for x in [(-4.1 + i * 0.51) for i in range(17)]:
        roof_parts.append(box(f"Roof rib south {x}", (x, base_y - 0.48, 5.45), (0.10, 1.86, 0.10), roof, collection,
            rotation=(math.radians(-14.0), 0.0, 0.0), bevel=0.018))
        roof_parts.append(box(f"Roof rib north {x}", (x, base_y + 0.48, 5.45), (0.10, 1.86, 0.10), roof, collection,
            rotation=(math.radians(14.0), 0.0, 0.0), bevel=0.018))

    stone_parts.append(box("Gate plaque", (0.0, base_y - 0.25, 4.13), (1.9, 0.16, 0.72), pale, collection, bevel=0.08))
    for x in (-0.46, 0.0, 0.46):
        bronze_parts.append(box(f"Erased register mark {x}", (x, base_y - 0.345, 4.13), (0.12, 0.045, 0.42), bronze, collection, bevel=0.018))

    return [
        join("Landmark_Stone_NorthGate", stone_parts, stone),
        join("Landmark_Timber_NorthGate", timber_parts, timber),
        join("Landmark_Roof_NorthGate", roof_parts, roof),
        join("Landmark_Bronze_NorthGate", bronze_parts, bronze),
    ]


def add_colossus(collection, stone, pale, soot, pigment):
    pale_parts = []
    dark_parts = []
    pigment_parts = []
    # Author the colossus for the right third of the hero frame after Unity's
    # FBX axis conversion and the landmark-root half turn.
    center = (4.65, 9.15)

    pale_parts.append(box("Colossus buried torso", (center[0], center[1] + 0.35, 2.0), (4.25, 1.35, 3.35), pale, collection,
        rotation=(math.radians(-5.0), math.radians(7.0), math.radians(-5.0)), bevel=0.18))
    pale_parts.append(ellipsoid("Colossus head", (center[0] + 0.15, center[1] - 0.18, 4.72), (1.42, 0.82, 1.66), pale, collection,
        rotation=(math.radians(7.0), math.radians(-4.0), math.radians(-6.0)), segments=32, rings=16))
    pale_parts.append(ellipsoid("Colossus top knot", (center[0] + 0.03, center[1], 6.06), (0.52, 0.44, 0.58), pale, collection, segments=20, rings=10))
    pale_parts.append(box("Colossus nose", (center[0] + 0.15, center[1] - 1.02, 4.65), (0.30, 0.30, 0.77), pale, collection,
        rotation=(math.radians(-8.0), 0.0, math.radians(-6.0)), bevel=0.11))
    pale_parts.append(torus("Colossus halo", (center[0], center[1] + 0.58, 4.46), 2.12, 0.22, pale, collection,
        rotation=(math.radians(90.0), 0.0, 0.0)))
    for side in (-1.0, 1.0):
        pale_parts.append(ellipsoid(f"Colossus ear {side}", (center[0] + side * 1.30, center[1] - 0.12, 4.72), (0.26, 0.22, 0.62), pale, collection,
            rotation=(0.0, math.radians(side * 8.0), math.radians(side * 7.0)), segments=16, rings=8))
        dark_parts.append(box(f"Colossus eye {side}", (center[0] + side * 0.47, center[1] - 1.02, 4.96), (0.48, 0.08, 0.095), soot, collection,
            rotation=(math.radians(-3.0), math.radians(side * 3.0), math.radians(side * 5.0)), bevel=0.035))

    # Broken robe bands and a small amount of mineral pigment give the statue a
    # worked, weathered surface instead of an untextured sphere-and-box figure.
    for row in range(6):
        pale_parts.append(box(f"Robe fold {row}", (center[0] - 1.55 + row * 0.61, center[1] - 0.38, 1.75 - abs(row - 2.5) * 0.10),
            (0.22, 0.20, 2.15 - abs(row - 2.5) * 0.14), pale, collection,
            rotation=(math.radians(-8.0), math.radians((row - 3) * 2.0), math.radians((row - 2.5) * 3.5)), bevel=0.055))
    pigment_parts.extend([
        box("Colossus brow pigment", (center[0] + 0.13, center[1] - 1.10, 5.23), (1.18, 0.055, 0.12), pigment, collection,
            rotation=(0.0, 0.0, math.radians(-5.0)), bevel=0.02),
        box("Colossus robe pigment", (center[0] - 1.05, center[1] - 0.55, 2.30), (0.46, 0.055, 1.32), pigment, collection,
            rotation=(0.0, 0.0, math.radians(8.0)), bevel=0.03),
    ])
    for index in range(14):
        angle = index * 1.93
        radius = 2.3 + (index % 4) * 0.34
        pale_parts.append(box(
            f"Colossus fragment {index}",
            (center[0] + math.cos(angle) * radius, center[1] - 0.7 + math.sin(angle) * radius * 0.44, 0.18 + (index % 3) * 0.10),
            (0.42 + (index % 3) * 0.13, 0.36 + (index % 2) * 0.12, 0.25 + (index % 4) * 0.07),
            stone if index % 3 == 0 else pale, collection,
            rotation=(angle * 0.19, angle * 0.31, angle * 0.13), bevel=0.06))

    colossus_parts = [
        join("Landmark_Pale_BrokenColossus", pale_parts, pale),
        join("Landmark_Soot_BrokenColossus", dark_parts, soot),
        join("Landmark_Pigment_BrokenColossus", pigment_parts, pigment),
    ]
    # Share a ground-level pivot so the three material groups stay registered
    # while the landmark is reduced to the mobile combat camera's readable scale.
    bpy.context.scene.cursor.location = (center[0], center[1], 0.0)
    for obj in colossus_parts:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        obj.scale = (0.55, 0.55, 0.55)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return colossus_parts


def add_ai_colossus(collection, material):
    before = set(bpy.data.objects)
    bpy.ops.wm.obj_import(filepath=str(AI_COLOSSUS_OBJ))
    imported = [obj for obj in bpy.data.objects if obj not in before and obj.type == "MESH"]
    if len(imported) != 1:
        raise RuntimeError(f"Expected one generated colossus mesh, imported {len(imported)}")
    obj = imported[0]
    obj.name = "Landmark_AI_BrokenColossus"
    obj.data.name = "Landmark_AI_BrokenColossus_Mesh"
    for owned in list(obj.users_collection):
        owned.objects.unlink(obj)
    collection.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(material)
    return [obj]


def add_kiln_workshop(collection, stone, pale, soot, ember, timber, pottery):
    stone_parts = []
    soot_parts = []
    ember_parts = []
    timber_parts = []
    pottery_parts = []
    center_x = 10.25
    center_y = 4.65

    for kiln_index, offset in enumerate((-1.45, 1.15)):
        x = center_x + offset
        stone_parts.extend([
            cylinder(f"Kiln drum {kiln_index}", (x, center_y, 1.18), 1.36, 2.35, stone, collection, vertices=20, bevel=0.07),
            ellipsoid(f"Kiln dome {kiln_index}", (x, center_y, 2.31), (1.38, 1.30, 0.72), stone, collection, segments=24, rings=10),
            cylinder(f"Kiln chimney {kiln_index}", (x + 0.42, center_y + 0.08, 3.18), 0.34, 1.38, soot, collection, vertices=14, bevel=0.045),
        ])
        soot_parts.append(box(f"Kiln mouth dark {kiln_index}", (x, center_y - 1.34, 0.82), (1.12, 0.12, 1.26), soot, collection, bevel=0.31))
        ember_parts.append(box(f"Kiln mouth fire {kiln_index}", (x, center_y - 1.43, 0.78), (0.78, 0.055, 0.79), ember, collection, bevel=0.24))
        for brick_index in range(11):
            angle = math.pi * brick_index / 10.0
            bx = x + math.cos(angle) * 0.68
            bz = 0.72 + math.sin(angle) * 0.71
            stone_parts.append(box(
                f"Kiln arch brick {kiln_index}-{brick_index}", (bx, center_y - 1.47, bz),
                (0.28, 0.30, 0.20), pale, collection,
                rotation=(0.0, math.radians(90.0 - math.degrees(angle)), 0.0), bevel=0.035))
        for course in range(4):
            radius = 1.39 - course * 0.04
            for brick_index in range(12):
                angle = math.tau * brick_index / 12.0 + course * 0.13
                stone_parts.append(box(
                    f"Kiln brick {kiln_index}-{course}-{brick_index}",
                    (x + math.cos(angle) * radius, center_y + math.sin(angle) * radius, 0.52 + course * 0.48),
                    (0.56, 0.18, 0.19), stone, collection,
                    rotation=(0.0, 0.0, angle), bevel=0.025))

    # Charred work shed frames the kilns and reads as a distinct eastern zone.
    for x in (8.1, 10.25, 12.4):
        timber_parts.append(cylinder(f"Workshop post {x}", (x, center_y + 1.85, 1.65), 0.12, 3.3, timber, collection, vertices=10, bevel=0.02))
    timber_parts.extend([
        box("Workshop beam", (10.25, center_y + 1.85, 3.12), (4.65, 0.25, 0.25), timber, collection, bevel=0.035),
        box("Workshop roof", (10.25, center_y + 1.65, 3.48), (5.1, 2.2, 0.20), timber, collection,
            rotation=(math.radians(9.0), 0.0, 0.0), bevel=0.055),
    ])

    for index in range(9):
        x = 8.25 + (index % 3) * 0.58
        y = center_y - 2.20 - (index // 3) * 0.45
        body = ellipsoid(f"Kiln vessel body {index}", (x, y, 0.33), (0.27 + (index % 2) * 0.05, 0.27 + (index % 2) * 0.05, 0.35), pottery, collection, segments=16, rings=8)
        neck = cylinder(f"Kiln vessel neck {index}", (x, y, 0.62), 0.13, 0.22, pottery, collection, vertices=12, bevel=0.02)
        pottery_parts.extend((body, neck))

    return [
        join("Landmark_Stone_KilnWorkshop", stone_parts, stone),
        join("Landmark_Soot_KilnWorkshop", soot_parts, soot),
        join("Landmark_Ember_KilnWorkshop", ember_parts, ember),
        join("Landmark_Timber_KilnWorkshop", timber_parts, timber),
        join("Landmark_Pottery_KilnWorkshop", pottery_parts, pottery),
    ]


def add_steles(collection, stone, pale, bronze):
    stone_parts = []
    bronze_parts = []
    for index in range(5):
        x = -3.7 + index * 1.2
        y = -7.55 + (index % 2) * 0.22
        height = 1.5 + (index % 3) * 0.23
        stone_parts.append(box(f"Register stele {index}", (x, y, height * 0.5), (0.68, 0.28, height), pale if index % 2 else stone, collection,
            rotation=(math.radians((index % 2) * 2.0), math.radians((index - 2) * 2.5), math.radians((index % 3 - 1) * 2.0)), bevel=0.07))
        stone_parts.append(box(f"Register stele foot {index}", (x, y, 0.13), (0.92, 0.60, 0.26), stone, collection, bevel=0.055))
        for mark in range(3):
            bronze_parts.append(box(f"Erased tally {index}-{mark}", (x - 0.20 + mark * 0.20, y - 0.17, height * 0.58), (0.08, 0.035, 0.52), bronze, collection, bevel=0.012))
    return [
        join("Landmark_Stone_RegisterSteles", stone_parts, stone),
        join("Landmark_Bronze_RegisterSteles", bronze_parts, bronze),
    ]


# Build in a dedicated collection so the preview floor, camera and lights never
# leak into the FBX.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for collection in list(bpy.data.collections):
    if collection.name != "Collection":
        bpy.data.collections.remove(collection)
asset_collection = bpy.data.collections.get("Collection")
asset_collection.name = "GreyKilnLandmarks"

stone = make_material("Stone_Limestone", (0.36, 0.33, 0.28), 0.91)
pale = make_material("Stone_Pale", (0.62, 0.58, 0.49), 0.88)
soot = make_material("Soot", (0.035, 0.028, 0.023), 0.95)
timber = make_material("Timber_Charred", (0.20, 0.11, 0.055), 0.86)
roof = make_material("Roof_AshTile", (0.12, 0.13, 0.13), 0.93)
bronze = make_material("Bronze_Worn", (0.33, 0.20, 0.08), 0.66, 0.42)
pigment = make_material("Pigment_Cinnabar", (0.30, 0.035, 0.022), 0.88)
pottery = make_material("Pottery", (0.31, 0.13, 0.055), 0.82)
ember = make_material("Ember", (0.96, 0.10, 0.015), 0.45, emission=(1.0, 0.08, 0.008))
ai_colossus = make_textured_material("Stone_AI_GreyKilnColossus", AI_COLOSSUS_TEXTURE)

export_objects = []
export_objects.extend(add_gate(asset_collection, stone, pale, timber, roof, bronze))
if AI_COLOSSUS_OBJ.exists():
    export_objects.extend(add_ai_colossus(asset_collection, ai_colossus))
else:
    export_objects.extend(add_colossus(asset_collection, stone, pale, soot, pigment))
export_objects.extend(add_kiln_workshop(asset_collection, stone, pale, soot, ember, timber, pottery))
export_objects.extend(add_steles(asset_collection, stone, pale, bronze))

# Add a small preview scene outside the exported collection.
preview_collection = bpy.data.collections.new("PreviewOnly")
bpy.context.scene.collection.children.link(preview_collection)
preview_floor = box("Preview floor", (0.0, 1.0, -0.14), (31.0, 20.0, 0.22), stone, preview_collection, bevel=0.0)

bpy.ops.object.light_add(type="AREA", location=(0.0, -7.0, 18.0))
key = bpy.context.object
key.data.energy = 1900
key.data.shape = "DISK"
key.data.size = 12.0
key.data.color = (0.48, 0.62, 1.0)
look_at(key, (0.0, 2.0, 1.8))
bpy.ops.object.light_add(type="AREA", location=(12.0, -2.0, 7.0))
fill = bpy.context.object
fill.data.energy = 1400
fill.data.size = 7.0
fill.data.color = (1.0, 0.24, 0.055)
look_at(fill, (7.5, 4.2, 1.4))

bpy.ops.object.camera_add(location=(19.5, -25.0, 22.0))
camera = bpy.context.object
camera.data.type = "ORTHO"
camera.data.ortho_scale = 25.0
look_at(camera, (0.0, 2.1, 1.6))
bpy.context.scene.camera = camera

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("Grey kiln world")
scene.world.color = (0.012, 0.015, 0.022)

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
SOURCE_DIR.mkdir(parents=True, exist_ok=True)
PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
scene.render.filepath = str(PREVIEW_PATH)
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
    bake_space_transform=True,
    use_mesh_modifiers=True,
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
stats["TOTAL"] = {"objects": len(export_objects), "vertices": vertex_total, "triangles": triangle_total}
STATS_PATH.write_text(json.dumps(stats, indent=2), encoding="utf-8")

manifest = {
    "asset": "Grey Kiln courtyard landmark kit",
    "generated_on": "2026-09-02",
    "generator": "Blender 5.2.1 LTS procedural kit plus project-owned TripoSR colossus cleanup",
    "license_status": "Project-owned generated source; TripoSR code and model are MIT",
    "historical_anchor": {
        "real_anchor": "Late-sixth-century northern Chinese limestone image-making, residual mineral pigment, pottery kilns, timber/rammed-earth/brick construction and inscribed steles, constrained by Docs/04A_HISTORICAL_BIBLE.md.",
        "fictional_transformation": "The erased military-register marks, sealed Statue Office gate and courtyard layout belong to the fictional Great Heng regime; no real Northern Qi site plan is copied.",
        "gameplay_purpose": "Gate, colossus, kiln workshop and stele row split one combat arena into readable north, west, east and south landmarks while preserving the central fight sightline.",
        "anachronism_check": "The kit avoids Japanese castle roofs, mature Song/Ming bracket density, pagoda silhouettes, Gothic masonry, modern industrial chimneys and sacred figures framed as intrinsically evil. It is a broad historical-fantasy prototype, not an archaeological reconstruction."
    },
    "geometry": stats["TOTAL"],
    "runtime_files": [],
    "source_files": [],
}
for path in (FBX_PATH, STATS_PATH):
    manifest["runtime_files"].append({"file": path.name, "bytes": path.stat().st_size, "sha256": sha256(path)})
source_paths = [BLEND_PATH, SCRIPT_PATH]
if AI_COLOSSUS_OBJ.exists():
    source_paths.extend((AI_COLOSSUS_OBJ, AI_COLOSSUS_TEXTURE))
for path in source_paths:
    manifest["source_files"].append({"file": str(path.relative_to(ROOT)).replace("\\", "/"), "bytes": path.stat().st_size, "sha256": sha256(path)})
MANIFEST_PATH.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

print(json.dumps({
    "fbx": str(FBX_PATH),
    "blend": str(BLEND_PATH),
    "preview": str(PREVIEW_PATH),
    "objects": len(export_objects),
    "vertices": vertex_total,
    "triangles": triangle_total,
}, ensure_ascii=False))
