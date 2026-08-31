#!/usr/bin/env python3
"""Fast repository-level validation that does not require a Unity license."""

from __future__ import annotations

import json
from pathlib import Path
import re
import sys


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
    for relative in FORBIDDEN_RUNTIME_PATHS:
        if (ROOT / relative).exists():
            fail(f"legacy or case-conflicting runtime path still exists: {relative}")

    forbidden_suffixes = {".gd", ".tscn", ".tres", ".godot"}
    for path in ROOT.rglob("*"):
        if path.is_file() and path.suffix.lower() in forbidden_suffixes:
            fail(f"Godot runtime file still tracked: {path.relative_to(ROOT)}")


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


def main() -> None:
    validate_required_files()
    validate_engine_boundary()
    validate_manifest()
    validate_project_version()
    validate_meta_files()
    validate_csharp_structure()
    print("[OK] Unity project skeleton, metadata and C# source structure validated.")


if __name__ == "__main__":
    main()
