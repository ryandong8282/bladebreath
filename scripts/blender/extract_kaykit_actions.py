"""Extract the small, approved KayKit action subset used by BladeBreath.

Usage (Blender 5.2 LTS):
    blender --background --factory-startup --python scripts/blender/extract_kaykit_actions.py -- \
        SourceArt/ThirdParty/KayKit/Adventurers-1.0/Knight.glb \
        Assets/_BladeBreath/ThirdParty/KayKit/Animations/Editor/KayKit_Selected.fbx

The output contains one armature, no KayKit character mesh or material, and only
the nine actions named below. Unity retargets them to the project-owned visual.
"""

from __future__ import annotations

import argparse
import pathlib
import sys

import bpy


SELECTED_ACTIONS = (
    "1H_Melee_Attack_Slice_Diagonal",
    "1H_Melee_Attack_Slice_Horizontal",
    "1H_Melee_Attack_Stab",
    "1H_Melee_Attack_Chop",
    "Block",
    "Blocking",
    "Block_Hit",
    "Dodge_Left",
    "Dodge_Right",
)


def parse_args() -> argparse.Namespace:
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("input_glb", type=pathlib.Path)
    parser.add_argument("output_fbx", type=pathlib.Path)
    return parser.parse_args(arguments)


def main() -> None:
    args = parse_args()
    input_path = args.input_glb.resolve()
    output_path = args.output_fbx.resolve()
    if not input_path.is_file():
        raise FileNotFoundError(input_path)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(input_path))

    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature in KayKit Knight.glb, found {len(armatures)}")
    armature = armatures[0]

    action_by_name = {action.name: action for action in bpy.data.actions}
    missing = [name for name in SELECTED_ACTIONS if name not in action_by_name]
    if missing:
        raise RuntimeError(f"KayKit source is missing approved actions: {missing}")

    for action in list(bpy.data.actions):
        if action.name not in SELECTED_ACTIONS:
            bpy.data.actions.remove(action)

    for name in SELECTED_ACTIONS:
        action = action_by_name[name]
        start, end = action.frame_range
        print(f"Selected action {name}: frames {start:.0f}-{end:.0f} ({end - start + 1:.0f} samples)")

    # Keep the source sampling rate deterministic. The FBX contains baked curves,
    # while combat contact timing remains owned by Unity's combat clock.
    bpy.context.scene.render.fps = 30
    bpy.context.scene.render.fps_base = 1.0
    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = action_by_name[SELECTED_ACTIONS[0]]

    bpy.ops.object.select_all(action="DESELECT")
    armature.hide_set(False)
    armature.hide_viewport = False
    armature.select_set(True)
    bpy.context.view_layer.objects.active = armature

    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"ARMATURE"},
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        use_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
        embed_textures=False,
    )

    size = output_path.stat().st_size
    if size >= 10 * 1024 * 1024:
        raise RuntimeError(f"Selected FBX is unexpectedly large: {size} bytes")
    print(f"Exported {len(SELECTED_ACTIONS)} KayKit actions to {output_path} ({size} bytes)")


if __name__ == "__main__":
    main()
