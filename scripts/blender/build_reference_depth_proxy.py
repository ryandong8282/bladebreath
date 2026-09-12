"""Turn a camera-space PLY into a standalone Blender reference scene."""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args() -> argparse.Namespace:
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--blend", required=True, type=Path)
    parser.add_argument("--render", required=True, type=Path)
    parser.add_argument("--camera-offset", type=float, default=1.25)
    return parser.parse_args(arguments)


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def main() -> None:
    args = parse_args()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    bpy.ops.wm.ply_import(filepath=str(args.input.resolve()))
    proxy = bpy.context.active_object
    proxy.name = "Target05_CameraSpaceDepthProxy"

    material = bpy.data.materials.new("ReferenceVertexColor")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    vertex_color = nodes.new("ShaderNodeVertexColor")
    vertex_color.layer_name = next(iter(proxy.data.color_attributes), None).name if proxy.data.color_attributes else "Col"
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Strength"].default_value = 0.9
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(vertex_color.outputs["Color"], emission.inputs["Color"])
    links.new(emission.outputs["Emission"], output.inputs["Surface"])
    proxy.data.materials.append(material)

    corners = [proxy.matrix_world @ Vector(corner) for corner in proxy.bound_box]
    center = sum(corners, Vector()) / 8.0

    camera_data = bpy.data.cameras.new("ReferenceCamera")
    camera = bpy.data.objects.new("ReferenceCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = Vector((args.camera_offset, 0.35, 0.0))
    camera_data.angle = math.radians(52.0)
    look_at(camera, center)
    bpy.context.scene.camera = camera

    world = bpy.data.worlds.new("ReferenceWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.008, 0.009, 0.012, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.15
    bpy.context.scene.world = world

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(args.render.resolve())
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"

    args.blend.parent.mkdir(parents=True, exist_ok=True)
    args.render.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.blend.resolve()))
    bpy.ops.render.render(write_still=True)
    print(
        f"proxy={proxy.name} vertices={len(proxy.data.vertices)} polygons={len(proxy.data.polygons)} "
        f"colors={len(proxy.data.color_attributes)}"
    )


if __name__ == "__main__":
    main()
