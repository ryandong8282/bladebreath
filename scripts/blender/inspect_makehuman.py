import json
import pathlib

import bpy
from mathutils import Vector


ROOT = pathlib.Path(r"D:\Dev\bladebreath")
SOURCE = ROOT / "Logs/makehuman-rest-reference.obj"
OUTPUT = ROOT / "Logs/blender-makehuman-inspection.json"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.wm.obj_import(filepath=str(SOURCE), forward_axis="NEGATIVE_Z", up_axis="Y")

objects = []
for obj in bpy.context.scene.objects:
    bounds = None
    if obj.type == "MESH":
        corners = [obj.matrix_world @ Vector(obj.bound_box[i]) for i in range(8)]
        bounds = {
            "minimum": [min(point[axis] for point in corners) for axis in range(3)],
            "maximum": [max(point[axis] for point in corners) for axis in range(3)],
        }
    objects.append({
        "name": obj.name,
        "type": obj.type,
        "parent": obj.parent.name if obj.parent else None,
        "vertex_count": len(obj.data.vertices) if obj.type == "MESH" else None,
        "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
        "bounds": bounds,
    })

armatures = []
for obj in bpy.context.scene.objects:
    if obj.type != "ARMATURE":
        continue
    armatures.append({
        "name": obj.name,
        "bones": [{"name": bone.name, "head": list(bone.head_local), "tail": list(bone.tail_local)} for bone in obj.data.bones],
    })

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
OUTPUT.write_text(json.dumps({"objects": objects, "armatures": armatures}, indent=2), encoding="utf-8")
print(OUTPUT)
