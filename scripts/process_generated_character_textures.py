#!/usr/bin/env python3
"""Prepare reproducible, mobile-sized character surface textures from ImageGen outputs."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = Path.home() / ".codex/generated_images/01a05872-9356-7de0-bf74-fa02c16ae7dd"
OUTPUT_ROOT = ROOT / "Assets/_BladeBreath/Art/Characters/Generated/Textures"

SOURCES = {
    "wuming_ceramic": {
        "file": "exec-9877bd12-7eaf-4acf-8568-bfa579bb2d18.png",
        "normal_strength": 2.2,
        "prompt": "Use case: stylized-concept\nAsset type: seamless production game material albedo texture for a low-poly iOS Unity hero, named Wuming\nPrimary request: a bold, highly readable aged ceramic armor surface: pale limestone-white glaze broken into broad irregular fired plates, visible dark charcoal chips and binding shadows expressed only as color, sparse hairline craquelure, ash abrasion, and restrained surviving mineral pigments in faded cinnabar red and ochre. The pigment should form broken painterly bands and fragments, never letters or symbols. It must look distinctive even when the character is only 100 pixels tall.\nStyle/medium: premium hand-painted PBR albedo, stylized historical fantasy, late-sixth-century northern Chinese stone-statue and kiln-fired ceramic mood, painterly clarity similar to a polished mobile action game\nComposition/framing: perfectly flat orthographic square material filling the canvas, broad macro variation plus medium detail, seamless and tileable on all four edges\nLighting/mood: neutral diffuse base color only; absolutely no directional light, cast shadow, ambient occlusion, highlight, reflection, or vignette\nColor palette: warm ivory, limestone grey, charcoal chips, faded cinnabar, muted ochre, tiny cold blue-grey ash accents\nConstraints: strong mobile readability; seamless repeat; no object silhouette; no armor shape; no face; no human body; no text; no watermark; no logo; no decorative emblem; no modern pattern; clean usable albedo\nAvoid: plain beige stone, uniform noise, marble, glossy porcelain, white studio highlights, photoreal object photography, Japanese motifs, dragons, runes, calligraphy, mosaics, deep black voids",
    },
    "wuming_cloth": {
        "file": "exec-26779db4-2b35-4c02-892a-f90b3e91a65f.png",
        "normal_strength": 2.6,
        "prompt": "Use case: stylized-concept\nAsset type: seamless production game material albedo texture for a low-poly iOS Unity hero's old military cloth\nPrimary request: a bold, mobile-readable woven cloth surface made from desaturated indigo and blue-black fabric, visibly repaired with irregular ash-grey patches, broken tan binding threads, short hand stitches, rubbed pale fold zones expressed only as color, soot, and a few faint rust-red stains. Use broad patch shapes and clear weave direction so the material is obvious on a character viewed from above.\nStyle/medium: premium hand-painted PBR albedo, stylized historical fantasy, practical sixth-century northern Chinese military textile mood, polished mobile action game readability\nComposition/framing: perfectly flat orthographic square material filling the canvas, broad asymmetric patchwork over medium weave detail, seamless and tileable on all four edges\nLighting/mood: neutral diffuse base color only; absolutely no directional light, cast shadow, ambient occlusion, highlight, reflection, folds, or vignette\nColor palette: charcoal blue-black, desaturated indigo, ash grey, raw tan thread, sparse muted rust red\nConstraints: strong readability at 100-pixel character height; seamless repeat; no garment silhouette; no body; no armor; no symbols; no text; no watermark; no logo; no embroidery; no modern machine stitching; clean usable albedo\nAvoid: denim, tartan, luxury brocade, glossy silk, flat uniform noise, modern camouflage, Japanese motifs, letters, runes, dramatic studio lighting",
    },
    "bladebearer_ceramic": {
        "file": "exec-5dbdbb64-2fe7-4700-baad-55e81199d001.png",
        "normal_strength": 2.1,
        "prompt": "Use case: stylized-concept\nAsset type: seamless production game material albedo texture for a low-poly iOS Unity enemy called the Statue Bureau Bladebearer\nPrimary request: a bold kiln-fired military ceramic surface: smoke-black and dark umber glaze in broad plates, controlled deep cinnabar-red fired sections, thin glowing-looking copper-red cracks expressed strictly as flat color (not emission or light), scraped ash-grey edges, soot bloom, and a few restrained dull bronze repair seams. It should feel standardized and severe, and remain readable when the enemy is only 100 pixels tall.\nStyle/medium: premium hand-painted PBR albedo, stylized historical fantasy, sixth-century northern Chinese kiln and military office mood, polished mobile action game readability\nComposition/framing: perfectly flat orthographic square material filling the canvas, broad organized plate rhythm with medium crack detail, seamless and tileable on all four edges\nLighting/mood: neutral diffuse base color only; absolutely no directional light, cast shadow, ambient occlusion, highlight, reflection, glow, or vignette\nColor palette: smoke black, dark umber, fired cinnabar, copper red, ash grey, sparse dull bronze\nConstraints: strong mobile readability; seamless repeat; no object silhouette; no armor shape; no face; no body; no symbols; no text; no watermark; no logo; no decorative emblem; no modern pattern; clean usable albedo\nAvoid: plain black stone, lava, glowing neon magma, sci-fi panels, shiny lacquer, Japanese motifs, dragons, runes, calligraphy, dramatic studio lighting",
    },
    "ceramic_glaze": {
        "file": "exec-7a7a41f9-dd16-4f35-a962-934ce1d3d97f.png",
        "normal_strength": 1.5,
        "prompt": "Use case: stylized-concept\nAsset type: seamless game material albedo texture for a low-poly iOS Unity character\nPrimary request: a seamless square texture of aged pale grey ceramic glaze used on a reconstructed warrior statue, with sparse hairline craquelure, worn edges implied only through color variation, faint ash staining, and a few restrained muted mineral pigment traces\nStyle/medium: hand-painted PBR albedo, stylized but materially believable, sixth-century northern Chinese stone-and-ceramic fantasy mood\nComposition/framing: perfectly flat orthographic surface texture, uniform scale across the full square, tileable on all four edges\nLighting/mood: neutral diffuse color only, no directional lighting, no baked shadows, no highlights\nColor palette: limestone grey, warm ash, tiny traces of faded ochre and muted cinnabar\nConstraints: seamless repeat; no objects; no armor silhouette; no face; no symbols; no text; no watermark; no strong cracks wider than a few pixels; mobile-readable medium-frequency detail\nAvoid: photoreal studio lighting, glossy reflections, black voids, modern porcelain patterns, Japanese motifs, decorative dragons, writing",
    },
    "old_cloth": {
        "file": "exec-56687d71-523f-46d6-8c11-5f10f73142ad.png",
        "normal_strength": 2.2,
        "prompt": "Use case: stylized-concept\nAsset type: seamless game material albedo texture for a low-poly iOS Unity character\nPrimary request: a seamless square texture of very old coarse military cloth, tightly woven but worn, with subtle frayed fibers, repaired patches implied through weave variation, ash dust, and uneven vegetable dye\nStyle/medium: hand-painted PBR albedo, stylized and readable at mobile distance, sixth-century northern Chinese military-fantasy mood\nComposition/framing: perfectly flat orthographic surface texture, uniform scale across the full square, tileable on all four edges\nLighting/mood: neutral diffuse color only, no directional lighting, no baked shadows, no highlights\nColor palette: deep blue-black, charcoal, desaturated indigo, tiny warm grey fibers\nConstraints: seamless repeat; no clothing object; no folds; no armor; no symbols; no text; no watermark; medium-frequency detail; dark but not crushed to black\nAvoid: denim, modern synthetic fabric, tartan, brocade, embroidery, logos, Japanese motifs, glossy fabric",
    },
    "worn_metal": {
        "file": "exec-516a6f1b-45e9-49bd-b600-cedd39405bbe.png",
        "normal_strength": 1.8,
        "prompt": "Use case: stylized-concept\nAsset type: seamless game material albedo texture for low-poly iOS Unity character armor and weapon fittings\nPrimary request: a seamless square texture of worn dark iron with restrained aged bronze oxidation, hammered irregularity, fine scratches, ash dust, and sparse muted green-grey patina in recess-like patches\nStyle/medium: hand-painted PBR albedo, stylized and readable at mobile distance, utilitarian sixth-century northern military equipment mood\nComposition/framing: perfectly flat orthographic surface texture, uniform scale across the full square, tileable on all four edges\nLighting/mood: neutral diffuse color only, no directional lighting, no baked shadows, no highlights or reflections\nColor palette: charcoal iron, dark umber bronze, muted olive-grey patina, warm worn edges expressed as color only\nConstraints: seamless repeat; no object silhouette; no weapon; no armor shape; no rivet layout; no symbols; no text; no watermark; medium-frequency detail\nAvoid: shiny gold, chrome, sci-fi panels, fantasy runes, modern machining, Japanese motifs, dramatic specular lighting",
    },
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def seamless_tile(image: Image.Image) -> Image.Image:
    """Feather opposing borders together while retaining the source's asymmetric center."""
    base = ImageOps.fit(image.convert("RGB"), (1024, 1024), method=Image.Resampling.LANCZOS)
    result = np.asarray(base, dtype=np.float32).copy()
    border = 96
    for index in range(border):
        amount = 1.0 - index / float(border - 1)
        amount = amount * amount * (3.0 - 2.0 * amount)
        left = result[:, index].copy()
        right = result[:, -1 - index].copy()
        average = (left + right) * 0.5
        result[:, index] = left * (1.0 - amount) + average * amount
        result[:, -1 - index] = right * (1.0 - amount) + average * amount
    for index in range(border):
        amount = 1.0 - index / float(border - 1)
        amount = amount * amount * (3.0 - 2.0 * amount)
        top = result[index].copy()
        bottom = result[-1 - index].copy()
        average = (top + bottom) * 0.5
        result[index] = top * (1.0 - amount) + average * amount
        result[-1 - index] = bottom * (1.0 - amount) + average * amount
    return Image.fromarray(np.clip(result, 0, 255).astype(np.uint8), "RGB")


def grade_albedo(name: str, image: Image.Image) -> Image.Image:
    image = seamless_tile(image)
    if name == "old_cloth":
        # A neutral cloth base can be tinted blue-black or soot-red by the shared materials.
        grey = ImageOps.autocontrast(ImageOps.grayscale(image), cutoff=1)
        values = np.asarray(grey, dtype=np.float32) / 255.0
        values = np.clip(0.50 + values * 0.40, 0.0, 1.0)
        rgb = np.repeat((values * 255.0).astype(np.uint8)[..., None], 3, axis=2)
        return Image.fromarray(rgb, "RGB")
    image = ImageEnhance.Contrast(image).enhance(0.96 if name.startswith(("wuming_", "bladebearer_")) else 0.92)
    brightness = 1.12 if name in {"worn_metal", "bladebearer_ceramic"} else 1.04
    image = ImageEnhance.Brightness(image).enhance(brightness)
    return image


def normal_from_albedo(image: Image.Image, strength: float) -> Image.Image:
    height = np.asarray(ImageOps.grayscale(image), dtype=np.float32) / 255.0
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * strength
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * strength
    normal = np.dstack((-dx, dy, np.ones_like(height)))
    normal /= np.maximum(np.linalg.norm(normal, axis=2, keepdims=True), 1e-6)
    encoded = ((normal * 0.5 + 0.5) * 255.0).clip(0, 255).astype(np.uint8)
    return Image.fromarray(encoded, "RGB")


def create_showcase() -> None:
    canvas = Image.new("RGB", (1920, 1080), (16, 17, 18))
    draw = ImageDraw.Draw(canvas)
    font_path = Path("C:/Windows/Fonts/msyh.ttc")
    if font_path.is_file():
        title_font = ImageFont.truetype(str(font_path), 54)
        label_font = ImageFont.truetype(str(font_path), 31)
        body_font = ImageFont.truetype(str(font_path), 21)
    else:
        title_font = label_font = body_font = ImageFont.load_default()

    draw.text((74, 48), "《无铭：漳城夜》角色贴图第一版", fill=(232, 220, 190), font=title_font)
    draw.text((76, 119), "真实工程输出 · 1K ALBEDO + NORMAL · iOS 压缩导入", fill=(151, 145, 132), font=body_font)
    cards = [
        ("wuming_ceramic", "无铭者 · 残釉陶甲", "灰白烧片 / 残存朱砂与赭石 / 暗灰缺口"),
        ("wuming_cloth", "无铭者 · 补丁旧布", "蓝黑粗布 / 灰补丁 / 手缝麻线 / 锈红污迹"),
        ("bladebearer_ceramic", "执刃者 · 烟黑烧陶", "烟黑制式烧片 / 土红釉 / 铜红裂缝 / 灰边"),
    ]
    for index, (name, label, note) in enumerate(cards):
        left = 68 + index * 612
        top = 178
        draw.rounded_rectangle((left, top, left + 560, 1008), radius=10,
                               fill=(25, 25, 24), outline=(104, 84, 52), width=2)
        albedo = Image.open(OUTPUT_ROOT / f"{name}_albedo_1k.jpg").convert("RGB")
        albedo = ImageOps.fit(albedo, (520, 600), method=Image.Resampling.LANCZOS)
        canvas.paste(albedo, (left + 20, top + 20))
        normal = Image.open(OUTPUT_ROOT / f"{name}_normal_1k.png").convert("RGB")
        normal = ImageOps.fit(normal, (126, 126), method=Image.Resampling.LANCZOS)
        canvas.paste(normal, (left + 414, top + 494))
        draw.rectangle((left + 410, top + 490, left + 544, top + 624), outline=(219, 196, 140), width=2)
        draw.text((left + 20, top + 646), label, fill=(238, 228, 204), font=label_font)
        draw.text((left + 20, top + 700), note, fill=(168, 160, 145), font=body_font)
        draw.text((left + 20, top + 760), "用途：角色远景辨识 + 近景材质细节", fill=(126, 140, 142), font=body_font)
        draw.text((left + 20, top + 797), "右下角：由颜色明度派生的法线图", fill=(126, 140, 142), font=body_font)
    output = ROOT / "Logs/character-texture-showcase.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output, "PNG", optimize=True)


def main() -> None:
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    manifest_files = []
    for name, spec in SOURCES.items():
        source = SOURCE_ROOT / str(spec["file"])
        if not source.is_file():
            raise FileNotFoundError(f"Missing ImageGen output: {source}")
        with Image.open(source) as opened:
            albedo = grade_albedo(name, opened)
        normal = normal_from_albedo(albedo, float(spec["normal_strength"]))

        albedo_path = OUTPUT_ROOT / f"{name}_albedo_1k.jpg"
        normal_path = OUTPUT_ROOT / f"{name}_normal_1k.png"
        albedo.save(albedo_path, "JPEG", quality=90, optimize=True, progressive=True)
        normal.save(normal_path, "PNG", optimize=True)
        manifest_files.append(
            {
                "name": name,
                "source_file": source.name,
                "source_sha256": sha256(source),
                "prompt": spec["prompt"],
                "outputs": [
                    {"file": albedo_path.name, "bytes": albedo_path.stat().st_size, "sha256": sha256(albedo_path)},
                    {"file": normal_path.name, "bytes": normal_path.stat().st_size, "sha256": sha256(normal_path)},
                ],
            }
        )

    manifest = {
        "generator": "OpenAI built-in ImageGen tool",
        "generated_date": "2026-09-01",
        "use": "Project-owned prototype character surface textures for the Unity iOS vertical slice.",
        "processing": "1024 resize with opposing-edge feathering; mild albedo grading; wrap-aware normal map derived from luminance; JPEG quality 90 albedo and optimized PNG normal.",
        "source_outputs_retained_at": str(SOURCE_ROOT),
        "files": manifest_files,
    }
    (OUTPUT_ROOT / "SOURCE_MANIFEST.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    create_showcase()
    print(f"Prepared {len(manifest_files)} generated surfaces in {OUTPUT_ROOT}")


if __name__ == "__main__":
    main()
