#!/usr/bin/env python3
"""Fast repository-level validation that does not require a Unity license."""

from __future__ import annotations

import json
import hashlib
from pathlib import Path
import re
import sys
import zipfile


ROOT = Path(__file__).resolve().parents[1]

REQUIRED_FILES = (
    "Packages/manifest.json",
    "ProjectSettings/ProjectVersion.txt",
    "Assets/_BladeBreath/Scripts/Core/Combatant.cs",
    "Assets/_BladeBreath/Scripts/Input/PrototypeInput.cs",
    "Assets/_BladeBreath/Scripts/Characters/PlayerController.cs",
    "Assets/_BladeBreath/Scripts/Characters/EnemyController.cs",
    "Assets/_BladeBreath/Scripts/Camera/TopDownCamera.cs",
    "Assets/_BladeBreath/Scripts/UI/PrototypeHud.cs",
    "Assets/_BladeBreath/Scripts/Prototype/PrototypeBootstrap.cs",
    "Assets/_BladeBreath/Editor/BladeBreathProjectSetup.cs",
    "Assets/_BladeBreath/Scripts/Characters/FighterPresentation.cs",
    "Assets/_BladeBreath/Scripts/Characters/CombatAudio.cs",
    "Assets/_BladeBreath/Editor/HumanoidAssetImporter.cs",
    "Assets/_BladeBreath/Editor/HumanoidSandboxBuilder.cs",
    "Assets/_BladeBreath/Editor/HumanoidSetupValidation.cs",
    "Assets/_BladeBreath/ThirdParty/Quaternius/asset-manifest.json",
    "Assets/_BladeBreath/ThirdParty/KayKit/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/ThirdParty/KayKit/License/LICENSE.txt",
    "Assets/_BladeBreath/ThirdParty/KayKit/Animations/Editor/KayKit_Selected.fbx",
    "SourceArt/ThirdParty/KayKit/Adventurers-1.0/Knight.glb",
    "scripts/blender/extract_kaykit_actions.py",
    "Assets/_BladeBreath/ThirdParty/Quaternius/Animations2/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/ThirdParty/Quaternius/Animations2/License/LICENSE.txt",
    "Assets/_BladeBreath/ThirdParty/Quaternius/Animations2/UAL2_Standard.zip",
    "Assets/_BladeBreath/ThirdParty/MakeHuman/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/ThirdParty/PolyHaven/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/ThirdParty/Kenney/ImpactSounds/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/ThirdParty/Kenney/RpgAudio/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/ThirdParty/OpenGameArt/Swishes/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/Art/Characters/Generated/Textures/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/Art/Characters/Generated/Models/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/Art/Environment/Generated/Models/SOURCE_MANIFEST.json",
    "Assets/_BladeBreath/Art/Weapons/Generated/NorthernQiLongblade.fbx",
    "Assets/_BladeBreath/Art/Weapons/Generated/NorthernQiLongblade.stats.json",
    "Assets/_BladeBreath/Art/Weapons/Generated/SOURCE_MANIFEST.json",
    "SourceArt/Weapons/NorthernQiLongblade.blend",
    "scripts/blender/build_northern_qi_longblade.py",
)

FORBIDDEN_RUNTIME_PATHS = (
    "assets",
    "project.godot",
    "scenes",
    "resources",
    "src",
)

EXPECTED_PACKAGES = {
    "com.unity.inputsystem": "1.16.0",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.unity.modules.animation": "1.0.0",
}

GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)


def fail(message: str) -> None:
    print(f"[FAIL] {message}", file=sys.stderr)
    raise SystemExit(1)


def validate_required_files() -> None:
    for relative in REQUIRED_FILES:
        if not (ROOT / relative).is_file():
            fail(f"missing required file: {relative}")


def validate_engine_boundary() -> None:
    # Path.exists() treats Assets and assets as identical on Windows/macOS.
    # Inspect actual directory-entry names so the valid Unity Assets folder passes.
    root_entries = {path.name for path in ROOT.iterdir()}
    for relative in FORBIDDEN_RUNTIME_PATHS:
        if relative in root_entries:
            fail(f"legacy or case-conflicting runtime path still exists: {relative}")

    forbidden_suffixes = {".gd", ".tscn", ".tres", ".godot"}
    for path in ROOT.rglob("*"):
        if path.is_file() and path.suffix.lower() in forbidden_suffixes:
            fail(f"Godot runtime file still tracked: {path.relative_to(ROOT)}")


def validate_bundled_assets() -> None:
    asset_root = ROOT / "Assets/_BladeBreath/ThirdParty/Quaternius"
    manifest = json.loads((asset_root / "asset-manifest.json").read_text(encoding="utf-8"))
    for entry in manifest["files"]:
        path = asset_root / entry["path"]
        if not path.is_file():
            fail(f"missing bundled asset: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
            fail(f"bundled asset changed without updating provenance: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"bundled binary exceeds the 10 MB budget: {path.relative_to(ROOT)}")
    with zipfile.ZipFile(asset_root / "Animations/UAL1_Standard.zip") as archive:
        if archive.namelist() != ["UAL1_Standard.fbx"]:
            fail("animation archive must contain only the original in-place FBX")
        data = archive.read("UAL1_Standard.fbx")
        if hashlib.sha256(data).hexdigest() != manifest["animation_source_sha256"]:
            fail("archived FBX does not match the recorded original source")
        if not data.startswith(b"Kaydara FBX Binary"):
            fail("archived animation source is not binary FBX")

    kaykit_manifest_path = ROOT / "Assets/_BladeBreath/ThirdParty/KayKit/SOURCE_MANIFEST.json"
    kaykit_manifest = json.loads(kaykit_manifest_path.read_text(encoding="utf-8"))
    expected_actions = {
        "1H_Melee_Attack_Slice_Diagonal", "1H_Melee_Attack_Slice_Horizontal",
        "1H_Melee_Attack_Stab", "1H_Melee_Attack_Chop", "Block", "Blocking", "Block_Hit",
        "Dodge_Left", "Dodge_Right",
    }
    if kaykit_manifest.get("license") != "CC0 1.0" or \
            set(kaykit_manifest.get("selected_actions", [])) != expected_actions:
        fail("KayKit manifest must retain the approved CC0 nine-action subset")
    if kaykit_manifest.get("upstream_commit") != "672074b73ba276876a19e8816ecdc5241817ab47":
        fail("KayKit source commit changed without review")
    kaykit_entries = (
        (kaykit_manifest.get("source_path"), kaykit_manifest.get("source_bytes"),
         kaykit_manifest.get("source_sha256"), b"glTF"),
        (kaykit_manifest.get("output_path"), kaykit_manifest.get("output_bytes"),
         kaykit_manifest.get("output_sha256"), b"Kaydara FBX Binary"),
        (kaykit_manifest.get("license_path"), 891,
         kaykit_manifest.get("license_sha256"), None),
    )
    for relative, expected_bytes, expected_hash, signature in kaykit_entries:
        path = ROOT / str(relative)
        if not path.is_file():
            fail(f"missing KayKit source or derivative: {relative}")
        data = path.read_bytes()
        if len(data) != expected_bytes or hashlib.sha256(data).hexdigest() != expected_hash:
            fail(f"KayKit asset changed without updating provenance: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"KayKit asset exceeds the 10 MB budget: {path.relative_to(ROOT)}")
        if signature is not None and not data.startswith(signature):
            fail(f"unexpected KayKit file signature: {path.relative_to(ROOT)}")

    ual2_manifest_path = ROOT / "Assets/_BladeBreath/ThirdParty/Quaternius/Animations2/SOURCE_MANIFEST.json"
    ual2_manifest = json.loads(ual2_manifest_path.read_text(encoding="utf-8"))
    if ual2_manifest.get("license") != "CC0 1.0" or set(ual2_manifest.get("selected_actions", [])) != {
        "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C", "Sword_Dash_RM", "Sword_Block",
    }:
        fail("UAL2 manifest must retain the approved CC0 five-action sword subset")
    if not ual2_manifest.get("source_url", "").startswith("https://opengameart.org/") or \
            not ual2_manifest.get("official_page", "").startswith("https://quaternius.com/"):
        fail("UAL2 source provenance is incomplete")
    ual2_entries = (
        (ual2_manifest.get("archive_path"), ual2_manifest.get("archive_bytes"),
         ual2_manifest.get("archive_sha256"), b"PK"),
        (ual2_manifest.get("license_path"), ual2_manifest.get("license_bytes"),
         ual2_manifest.get("license_sha256"), None),
    )
    for relative, expected_bytes, expected_hash, signature in ual2_entries:
        path = ROOT / str(relative)
        if not path.is_file():
            fail(f"missing UAL2 derivative or provenance file: {relative}")
        data = path.read_bytes()
        if len(data) != expected_bytes or hashlib.sha256(data).hexdigest() != expected_hash:
            fail(f"UAL2 file changed without updating provenance: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"UAL2 derivative exceeds the 10 MB budget: {path.relative_to(ROOT)}")
        if signature is not None and not data.startswith(signature):
            fail(f"unexpected UAL2 file signature: {path.relative_to(ROOT)}")
    with zipfile.ZipFile(ROOT / ual2_manifest["archive_path"]) as archive:
        if archive.namelist() != ["UAL2_Standard.fbx"]:
            fail("UAL2 archive must contain only the exact source FBX")
        data = archive.read("UAL2_Standard.fbx")
        if len(data) != ual2_manifest.get("source_fbx_bytes") or \
                hashlib.sha256(data).hexdigest() != ual2_manifest.get("source_fbx_sha256"):
            fail("archived UAL2 FBX does not match the recorded official source")

    human_root = ROOT / "Assets/_BladeBreath/ThirdParty/MakeHuman"
    human_manifest = json.loads((human_root / "SOURCE_MANIFEST.json").read_text(encoding="utf-8"))
    human_files = human_manifest.get("files")
    if not isinstance(human_files, list) or len(human_files) != 7:
        fail("realistic human manifest must record the FBX and six source textures")
    for entry in human_files:
        human_path = human_root / entry["path"]
        if not human_path.is_file():
            fail(f"missing realistic human asset: {human_path.relative_to(ROOT)}")
        human_data = human_path.read_bytes()
        if len(human_data) != entry.get("bytes") or hashlib.sha256(human_data).hexdigest() != entry.get("sha256"):
            fail(f"realistic human asset changed without updating provenance: {human_path.relative_to(ROOT)}")
        if len(human_data) > 10 * 1024 * 1024:
            fail(f"realistic human asset exceeds the 10 MB budget: {human_path.relative_to(ROOT)}")
        if human_path.suffix == ".fbx" and not human_data.startswith(b"; FBX 7.3.0 project file"):
            fail("realistic human model is not the recorded MakeHuman FBX 7.3 export")
    model_settings = human_root / human_manifest.get("source_model_settings", "")
    if not model_settings.is_file() or "skeleton basic.json" not in model_settings.read_text(encoding="utf-8"):
        fail("realistic human source settings or basic rig provenance are missing")
    contents = human_manifest.get("contents", {})
    if contents.get("skinned_meshes", 0) < 5 or contents.get("runtime_texture_limit", 0) > 1024:
        fail("realistic human manifest no longer proves the approved meshes and mobile texture limit")
    if human_manifest.get("license", {}).get("character_and_makehuman_assets") != "CC0 1.0":
        fail("realistic human asset must retain its recorded CC0 provenance")

    texture_root = ROOT / "Assets/_BladeBreath/ThirdParty/PolyHaven"
    texture_manifest = json.loads((texture_root / "SOURCE_MANIFEST.json").read_text(encoding="utf-8"))
    for entry in texture_manifest["files"]:
        path = texture_root / entry["file"]
        if not path.is_file():
            fail(f"missing bundled texture: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
            fail(f"bundled texture changed without updating provenance: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"bundled texture exceeds the 10 MB budget: {path.relative_to(ROOT)}")
        if entry.get("license") != "CC0 1.0" or not entry.get("url", "").startswith("https://dl.polyhaven.org/"):
            fail(f"invalid Poly Haven provenance: {path.relative_to(ROOT)}")

    audio_manifests = (
        "Assets/_BladeBreath/ThirdParty/Kenney/ImpactSounds/SOURCE_MANIFEST.json",
        "Assets/_BladeBreath/ThirdParty/Kenney/RpgAudio/SOURCE_MANIFEST.json",
        "Assets/_BladeBreath/ThirdParty/OpenGameArt/Swishes/SOURCE_MANIFEST.json",
    )
    for relative_manifest in audio_manifests:
        manifest_path = ROOT / relative_manifest
        audio_manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        if audio_manifest.get("license") != "CC0-1.0" or not audio_manifest.get("source_page", "").startswith("https://"):
            fail(f"invalid audio provenance: {manifest_path.relative_to(ROOT)}")
        entries = audio_manifest.get("files")
        if not isinstance(entries, list) or not entries:
            fail(f"audio manifest has no files: {manifest_path.relative_to(ROOT)}")
        for entry in entries:
            path = manifest_path.parent / entry["path"]
            if not path.is_file():
                fail(f"missing bundled audio asset: {path.relative_to(ROOT)}")
            data = path.read_bytes()
            if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
                fail(f"bundled audio changed without updating provenance: {path.relative_to(ROOT)}")
            if len(data) > 10 * 1024 * 1024:
                fail(f"bundled audio exceeds the 10 MB budget: {path.relative_to(ROOT)}")


def validate_generated_character_textures() -> None:
    texture_root = ROOT / "Assets/_BladeBreath/Art/Characters/Generated/Textures"
    manifest = json.loads((texture_root / "SOURCE_MANIFEST.json").read_text(encoding="utf-8"))
    if manifest.get("generator") != "OpenAI built-in ImageGen tool" or manifest.get("generated_date") != "2026-09-01":
        fail("generated character texture provenance is incomplete")
    entries = manifest.get("files")
    if not isinstance(entries, list) or {entry.get("name") for entry in entries} != {
        "ceramic_glaze", "old_cloth", "worn_metal",
        "wuming_ceramic", "wuming_cloth", "bladebearer_ceramic",
    }:
        fail("generated character texture manifest must contain the six approved surfaces")
    for entry in entries:
        if not entry.get("prompt") or not entry.get("source_sha256"):
            fail(f"generated character texture prompt lineage is incomplete: {entry.get('name')}")
        outputs = entry.get("outputs")
        if not isinstance(outputs, list) or len(outputs) != 2:
            fail(f"generated character texture needs albedo and normal outputs: {entry.get('name')}")
        for output in outputs:
            path = texture_root / output["file"]
            if not path.is_file():
                fail(f"missing generated character texture: {path.relative_to(ROOT)}")
            data = path.read_bytes()
            if len(data) != output["bytes"] or hashlib.sha256(data).hexdigest() != output["sha256"]:
                fail(f"generated character texture changed without updating provenance: {path.relative_to(ROOT)}")
            if len(data) > 10 * 1024 * 1024:
                fail(f"generated character texture exceeds the 10 MB budget: {path.relative_to(ROOT)}")


def validate_generated_costume() -> None:
    model_root = ROOT / "Assets/_BladeBreath/Art/Characters/Generated/Models"
    manifest = json.loads((model_root / "SOURCE_MANIFEST.json").read_text(encoding="utf-8"))
    if manifest.get("generator") != "Blender 5.2.1 LTS procedural Python pipeline":
        fail("generated costume does not record the approved Blender pipeline")
    anchor = manifest.get("historical_anchor", {})
    for field in ("real_anchor", "fictional_transformation", "gameplay_purpose", "anachronism_check"):
        if not anchor.get(field):
            fail(f"generated costume is missing historical review field: {field}")
    geometry = manifest.get("geometry", {})
    # The close-camera costume gains separate mantle panels, knee guards and
    # readable front/back lamellae. Keep the new cap explicit so later Blender
    # passes cannot grow the mobile mesh silently.
    if geometry.get("objects") != 29 or not 0 < geometry.get("triangles", 0) <= 32000:
        fail("generated costume no longer meets the recorded mobile geometry budget")

    for entry in manifest.get("runtime_files", []):
        path = model_root / entry["file"]
        if not path.is_file():
            fail(f"missing generated costume output: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
            fail(f"generated costume output changed without updating provenance: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"generated costume output exceeds the 10 MB budget: {path.relative_to(ROOT)}")

    for entry in manifest.get("source_files", []):
        path = ROOT / entry["file"]
        if not path.is_file():
            fail(f"missing generated costume source: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
            fail(f"generated costume source changed without updating provenance: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"generated costume source exceeds the 10 MB budget: {path.relative_to(ROOT)}")


def validate_manifest() -> None:
    manifest_path = ROOT / "Packages/manifest.json"
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(f"invalid Packages/manifest.json: {exc}")

    dependencies = manifest.get("dependencies")
    if not isinstance(dependencies, dict):
        fail("Packages/manifest.json must contain a dependencies object")

    for package, expected_version in EXPECTED_PACKAGES.items():
        actual = dependencies.get(package)
        if actual != expected_version:
            fail(f"{package} expected {expected_version}, found {actual!r}")

    if "com.unity.modules.input" in dependencies:
        fail("remove com.unity.modules.input; the project uses com.unity.inputsystem")


def validate_project_version() -> None:
    text = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
    if "m_EditorVersion: 6000.3.23f1" not in text:
        fail("unexpected Unity editor version")


def validate_meta_files() -> None:
    assets_root = ROOT / "Assets"
    if not assets_root.is_dir():
        fail("missing Assets directory")

    seen_guids: dict[str, Path] = {}
    sources = [path for path in assets_root.rglob("*") if not path.name.endswith(".meta")]

    for source in sources:
        if source.name.startswith("."):
            continue

        meta_path = Path(f"{source}.meta")
        if not meta_path.is_file():
            fail(f"missing Unity meta file: {meta_path.relative_to(ROOT)}")

        text = meta_path.read_text(encoding="utf-8")
        match = GUID_PATTERN.search(text)
        if match is None:
            fail(f"invalid or missing guid in {meta_path.relative_to(ROOT)}")

        guid = match.group(1)
        previous = seen_guids.get(guid)
        if previous is not None:
            fail(
                "duplicate Unity guid "
                f"{guid}: {previous.relative_to(ROOT)} and {meta_path.relative_to(ROOT)}"
            )
        seen_guids[guid] = meta_path

        if source.is_dir() and "folderAsset: yes" not in text:
            fail(f"folder meta missing folderAsset: yes: {meta_path.relative_to(ROOT)}")

    for meta_path in assets_root.rglob("*.meta"):
        source_path = Path(str(meta_path)[:-5])
        if not source_path.exists():
            fail(f"orphan Unity meta file: {meta_path.relative_to(ROOT)}")


def strip_csharp_comments_and_strings(text: str) -> str:
    output: list[str] = []
    i = 0
    state = "code"

    while i < len(text):
        char = text[i]
        next_char = text[i + 1] if i + 1 < len(text) else ""

        if state == "code":
            if char == "/" and next_char == "/":
                state = "line_comment"
                output.extend((" ", " "))
                i += 2
                continue
            if char == "/" and next_char == "*":
                state = "block_comment"
                output.extend((" ", " "))
                i += 2
                continue
            if char == '"':
                state = "string"
                output.append(" ")
                i += 1
                continue
            if char == "'":
                state = "char"
                output.append(" ")
                i += 1
                continue
            output.append(char)
            i += 1
            continue

        if state == "line_comment":
            if char == "\n":
                state = "code"
                output.append("\n")
            else:
                output.append(" ")
            i += 1
            continue

        if state == "block_comment":
            if char == "*" and next_char == "/":
                state = "code"
                output.extend((" ", " "))
                i += 2
            else:
                output.append("\n" if char == "\n" else " ")
                i += 1
            continue

        if state in {"string", "char"}:
            quote = '"' if state == "string" else "'"
            if char == "\\":
                output.extend((" ", " "))
                i += 2
                continue
            if char == quote:
                state = "code"
            output.append(" ")
            i += 1

    return "".join(output)


def validate_csharp_structure() -> None:
    for path in (ROOT / "Assets").rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        if "<<<<<<<" in text or "=======" in text or ">>>>>>>" in text:
            fail(f"merge conflict marker in {path.relative_to(ROOT)}")

        stripped = strip_csharp_comments_and_strings(text)
        balance = 0
        for char in stripped:
            if char == "{":
                balance += 1
            elif char == "}":
                balance -= 1
                if balance < 0:
                    fail(f"closing brace before opening brace in {path.relative_to(ROOT)}")

        if balance != 0:
            fail(f"unbalanced braces in {path.relative_to(ROOT)}: {balance:+d}")


def validate_generated_environment() -> None:
    model_root = ROOT / "Assets/_BladeBreath/Art/Environment/Generated/Models"
    manifest = json.loads((model_root / "SOURCE_MANIFEST.json").read_text(encoding="utf-8"))
    geometry = manifest.get("geometry", {})
    if not 12 <= geometry.get("objects", 0) <= 18:
        fail("generated Grey Kiln landmark renderer count left its mobile budget")
    if not 45000 <= geometry.get("triangles", 0) <= 60000:
        fail("generated Grey Kiln landmark triangle count left its mobile budget")
    colossus = manifest.get("generated_colossus_geometry", {})
    if not 20000 <= colossus.get("triangles", 0) <= 30000:
        fail("generated Grey Kiln colossus left its approved mobile triangle budget")
    for entry in manifest.get("runtime_files", []):
        path = model_root / entry["file"]
        if not path.is_file():
            fail(f"missing generated environment output: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry.get("bytes") or hashlib.sha256(data).hexdigest() != entry.get("sha256"):
            fail(f"generated environment output changed without manifest update: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"generated environment output exceeds the 10 MB budget: {path.relative_to(ROOT)}")
    for entry in manifest.get("source_files", []):
        path = ROOT / entry["file"]
        if not path.is_file():
            fail(f"missing generated environment source: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry.get("bytes") or hashlib.sha256(data).hexdigest() != entry.get("sha256"):
            fail(f"generated environment source changed without manifest update: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"generated environment source exceeds the 10 MB budget: {path.relative_to(ROOT)}")


def validate_generated_longblade() -> None:
    model_root = ROOT / "Assets/_BladeBreath/Art/Weapons/Generated"
    manifest = json.loads((model_root / "SOURCE_MANIFEST.json").read_text(encoding="utf-8"))
    if manifest.get("ownership") != "Project-owned procedural mesh; no third-party model geometry included":
        fail("generated longblade ownership record is incomplete")
    anchor = manifest.get("historical_anchor", {})
    if "metmuseum.org/art/collection/search/23352" not in anchor.get("url", ""):
        fail("generated longblade is missing its ca. 600 Chinese physical anchor")
    for field in ("fictional_transformation", "gameplay_purpose", "anachronism_check"):
        if not manifest.get(field):
            fail(f"generated longblade is missing historical review field: {field}")
    audit = manifest.get("open_asset_audit", [])
    if len(audit) < 2 or not all(entry.get("license") == "CC0" and entry.get("decision") for entry in audit):
        fail("generated longblade does not record the rejected open-asset candidates")
    geometry = manifest.get("geometry", {})
    if geometry.get("objects") != 4 or not 2500 <= geometry.get("triangles", 0) <= 4000:
        fail("generated longblade left its M0 mobile geometry budget")

    for entry in manifest.get("runtime_files", []):
        path = model_root / entry["file"]
        if not path.is_file():
            fail(f"missing generated longblade output: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry.get("bytes") or hashlib.sha256(data).hexdigest() != entry.get("sha256"):
            fail(f"generated longblade output changed without manifest update: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"generated longblade output exceeds the 10 MB budget: {path.relative_to(ROOT)}")
    for entry in manifest.get("source_files", []):
        path = ROOT / entry["file"]
        if not path.is_file():
            fail(f"missing generated longblade source: {path.relative_to(ROOT)}")
        data = path.read_bytes()
        if len(data) != entry.get("bytes") or hashlib.sha256(data).hexdigest() != entry.get("sha256"):
            fail(f"generated longblade source changed without manifest update: {path.relative_to(ROOT)}")
        if len(data) > 10 * 1024 * 1024:
            fail(f"generated longblade source exceeds the 10 MB budget: {path.relative_to(ROOT)}")


def main() -> None:
    validate_required_files()
    validate_engine_boundary()
    validate_manifest()
    validate_project_version()
    validate_meta_files()
    validate_bundled_assets()
    validate_generated_character_textures()
    validate_generated_costume()
    validate_generated_environment()
    validate_generated_longblade()
    validate_csharp_structure()
    print("[OK] Unity project skeleton, metadata and C# source structure validated.")


if __name__ == "__main__":
    main()
