#!/usr/bin/env python3
"""Build depth maps and camera-space proxy meshes from concept screenshots.

The neural network checkout and weights stay outside the Unity repository.  The
generated mesh is a camera-matched 2.5D planning aid, not a game-ready level.
"""

from __future__ import annotations

import argparse
import json
import math
import struct
import sys
from pathlib import Path

import cv2
import numpy as np
import torch


MODEL_CONFIGS = {
    "vits": {"encoder": "vits", "features": 64, "out_channels": [48, 96, 192, 384]},
    "vitb": {"encoder": "vitb", "features": 128, "out_channels": [96, 192, 384, 768]},
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("inputs", nargs="+", type=Path)
    parser.add_argument("--depth-anything-root", required=True, type=Path)
    parser.add_argument("--checkpoint", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--encoder", choices=MODEL_CONFIGS, default="vits")
    parser.add_argument("--input-size", type=int, default=770)
    parser.add_argument("--mesh-width", type=int, default=256)
    parser.add_argument("--vertical-fov", type=float, default=52.0)
    parser.add_argument("--near", type=float, default=3.0)
    parser.add_argument("--far", type=float, default=22.0)
    parser.add_argument("--hud-mask", action="store_true")
    return parser.parse_args()


def hud_mask(width: int, height: int) -> np.ndarray:
    """Mask the stable HUD regions shared by the five 16:9 target images."""
    mask = np.ones((height, width), dtype=bool)
    regions = (
        (0.00, 0.00, 0.24, 0.15),
        (0.82, 0.00, 1.00, 0.13),
        (0.00, 0.66, 0.30, 1.00),
        (0.68, 0.69, 1.00, 1.00),
        (0.90, 0.42, 1.00, 0.82),
        (0.38, 0.84, 0.64, 1.00),
    )
    for x0, y0, x1, y1 in regions:
        mask[round(y0 * height) : round(y1 * height), round(x0 * width) : round(x1 * width)] = False
    return mask


def normalize_depth(raw_depth: np.ndarray) -> np.ndarray:
    low, high = np.percentile(raw_depth, (2.0, 98.0))
    if high <= low:
        raise ValueError("Depth prediction has no usable range")
    return np.clip((raw_depth - low) / (high - low), 0.0, 1.0)


def write_proxy_ply(
    path: Path,
    bgr: np.ndarray,
    depth_near_white: np.ndarray,
    mesh_width: int,
    vertical_fov: float,
    near: float,
    far: float,
    remove_hud: bool,
) -> dict[str, int | float]:
    source_height, source_width = depth_near_white.shape
    mesh_height = max(2, round(source_height * mesh_width / source_width))
    color = cv2.resize(bgr, (mesh_width, mesh_height), interpolation=cv2.INTER_AREA)
    depth = np.clip(
        cv2.resize(depth_near_white, (mesh_width, mesh_height), interpolation=cv2.INTER_CUBIC),
        0.0,
        1.0,
    )
    valid = hud_mask(mesh_width, mesh_height) if remove_hud else np.ones_like(depth, dtype=bool)

    # Depth Anything returns relative inverse depth: high values are nearer.
    z = near + np.power(1.0 - depth, 1.35) * (far - near)
    fy = 0.5 * mesh_height / math.tan(math.radians(vertical_fov) * 0.5)
    fx = fy
    cx = (mesh_width - 1) * 0.5
    cy = (mesh_height - 1) * 0.5

    vertex_ids = np.full((mesh_height, mesh_width), -1, dtype=np.int32)
    vertices: list[tuple[float, float, float, int, int, int]] = []
    for row in range(mesh_height):
        for column in range(mesh_width):
            if not valid[row, column]:
                continue
            distance = float(z[row, column])
            x = (column - cx) * distance / fx
            y = (cy - row) * distance / fy
            blue, green, red = (int(channel) for channel in color[row, column])
            vertex_ids[row, column] = len(vertices)
            vertices.append((x, y, -distance, red, green, blue))

    faces: list[tuple[int, int, int]] = []
    discontinuity_limit = (far - near) * 0.06
    for row in range(mesh_height - 1):
        for column in range(mesh_width - 1):
            ids = (
                int(vertex_ids[row, column]),
                int(vertex_ids[row, column + 1]),
                int(vertex_ids[row + 1, column]),
                int(vertex_ids[row + 1, column + 1]),
            )
            if min(ids) < 0:
                continue
            depths = (z[row, column], z[row, column + 1], z[row + 1, column], z[row + 1, column + 1])
            if float(max(depths) - min(depths)) > discontinuity_limit:
                continue
            faces.append((ids[0], ids[2], ids[1]))
            faces.append((ids[1], ids[2], ids[3]))

    header = (
        "ply\n"
        "format binary_little_endian 1.0\n"
        f"element vertex {len(vertices)}\n"
        "property float x\nproperty float y\nproperty float z\n"
        "property uchar red\nproperty uchar green\nproperty uchar blue\n"
        f"element face {len(faces)}\n"
        "property list uchar int vertex_indices\n"
        "end_header\n"
    ).encode("ascii")
    with path.open("wb") as handle:
        handle.write(header)
        for vertex in vertices:
            handle.write(struct.pack("<fffBBB", *vertex))
        for face in faces:
            handle.write(struct.pack("<Biii", 3, *face))

    return {"vertices": len(vertices), "triangles": len(faces), "vertical_fov": vertical_fov}


def main() -> None:
    args = parse_args()
    sys.path.insert(0, str(args.depth_anything_root))
    from depth_anything_v2.dpt import DepthAnythingV2

    device = "cuda" if torch.cuda.is_available() else "cpu"
    model = DepthAnythingV2(**MODEL_CONFIGS[args.encoder])
    model.load_state_dict(torch.load(args.checkpoint, map_location="cpu", weights_only=True))
    model = model.to(device).eval()
    args.output_dir.mkdir(parents=True, exist_ok=True)

    manifest: dict[str, object] = {
        "tool": "Depth Anything V2",
        "model": args.encoder,
        "device": device,
        "camera_space_proxy_only": True,
        "files": [],
    }
    for input_path in args.inputs:
        bgr = cv2.imread(str(input_path), cv2.IMREAD_COLOR)
        if bgr is None:
            raise FileNotFoundError(input_path)
        raw_depth = model.infer_image(bgr, args.input_size)
        depth = normalize_depth(raw_depth)
        stem = input_path.stem.replace(" ", "_").replace("-", "_")
        depth_path = args.output_dir / f"{stem}_depth.png"
        proxy_path = args.output_dir / f"{stem}_camera_proxy.ply"
        cv2.imwrite(str(depth_path), np.round(depth * 65535.0).astype(np.uint16))
        proxy_stats = write_proxy_ply(
            proxy_path,
            bgr,
            depth,
            args.mesh_width,
            args.vertical_fov,
            args.near,
            args.far,
            args.hud_mask,
        )
        manifest["files"].append(
            {
                "source": str(input_path),
                "depth": depth_path.name,
                "proxy": proxy_path.name,
                **proxy_stats,
            }
        )
        print(f"{input_path.name}: {proxy_stats['vertices']} vertices, {proxy_stats['triangles']} triangles")

    (args.output_dir / "manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )


if __name__ == "__main__":
    main()
