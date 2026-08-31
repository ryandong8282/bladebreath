#!/usr/bin/env python3
"""Fast structural validation for the Wuming Zhangcheng Godot project.

This is intentionally not a replacement for Godot's parser. It catches the
most common repository-generation failures before the engine smoke test runs:
missing res:// paths, duplicate classes/functions, malformed delimiters,
mixed indentation, stale main-scene references, and accidental conflict text.
"""

from __future__ import annotations

import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = [
    ROOT / "project.godot",
    ROOT / "scenes/main.tscn",
    ROOT / "src/game/main.gd",
    ROOT / "src/combat/combatant.gd",
    ROOT / "src/actors/player_controller.gd",
    ROOT / "src/actors/enemy_controller.gd",
    ROOT / "src/camera/camera_follow.gd",
    ROOT / "src/ui/combat_hud.gd",
    ROOT / "src/ui/mobile_action_button.gd",
    ROOT / "src/ui/mobile_controls.gd",
    ROOT / "src/ui/virtual_joystick.gd",
    ROOT / "resources/weapons/longblade.tres",
    ROOT / "resources/styles/hearing_blade.tres",
    ROOT / "resources/styles/flowing_shadow.tres",
]
TEXT_SUFFIXES = {".gd", ".tscn", ".tres", ".godot", ".md", ".yml", ".yaml", ".sh", ".py"}


def fail(message: str, errors: list[str]) -> None:
    errors.append(message)


def strip_strings_and_comments(text: str) -> str:
    out: list[str] = []
    quote: str | None = None
    escaped = False
    comment = False
    for char in text:
        if char == "\n":
            out.append(char)
            comment = False
            continue
        if comment:
            out.append(" ")
            continue
        if quote is not None:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == quote:
                quote = None
            out.append(" ")
            continue
        if char == "#":
            comment = True
            out.append(" ")
        elif char in {'"', "'"}:
            quote = char
            out.append(" ")
        else:
            out.append(char)
    return "".join(out)


def check_balanced(path: Path, text: str, errors: list[str]) -> None:
    clean = strip_strings_and_comments(text)
    pairs = {")": "(", "]": "[", "}": "{"}
    opening = set(pairs.values())
    stack: list[tuple[str, int]] = []
    line = 1
    for char in clean:
        if char == "\n":
            line += 1
        elif char in opening:
            stack.append((char, line))
        elif char in pairs:
            if not stack or stack[-1][0] != pairs[char]:
                fail(f"{path.relative_to(ROOT)}:{line}: unmatched {char}", errors)
                return
            stack.pop()
    if stack:
        char, line = stack[-1]
        fail(f"{path.relative_to(ROOT)}:{line}: unclosed {char}", errors)


def check_gdscript(path: Path, text: str, class_names: list[tuple[str, Path]], errors: list[str]) -> None:
    rel = path.relative_to(ROOT)
    functions = re.findall(r"^func\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(", text, flags=re.MULTILINE)
    duplicates = [name for name, count in Counter(functions).items() if count > 1]
    for name in duplicates:
        fail(f"{rel}: duplicate function {name}", errors)

    classes = re.findall(r"^class_name\s+([A-Za-z_][A-Za-z0-9_]*)\s*$", text, flags=re.MULTILINE)
    class_names.extend((name, path) for name in classes)

    for line_number, line in enumerate(text.splitlines(), start=1):
        leading = line[: len(line) - len(line.lstrip(" \t"))]
        if " " in leading and "\t" in leading:
            fail(f"{rel}:{line_number}: mixed tabs and spaces in indentation", errors)
        if leading and leading.strip("\t"):
            fail(f"{rel}:{line_number}: use tabs for GDScript indentation", errors)
        if line.rstrip().endswith("\\"):
            fail(f"{rel}:{line_number}: avoid backslash continuation", errors)

    suspicious = [
        (r"<<<<<<<|=======|>>>>>>>", "merge conflict marker"),
        (r"else:\s*\n\s*else:", "double else"),
        (r"super\.force_stagger\(", "same-method super call should use super(...)"),
    ]
    for pattern, label in suspicious:
        if re.search(pattern, text):
            fail(f"{rel}: {label}", errors)

    check_balanced(path, text, errors)


def main() -> int:
    errors: list[str] = []

    for path in REQUIRED:
        if not path.exists():
            fail(f"missing required file: {path.relative_to(ROOT)}", errors)

    project = (ROOT / "project.godot").read_text(encoding="utf-8")
    if 'run/main_scene="res://scenes/main.tscn"' not in project:
        fail("project.godot: unexpected or missing main scene", errors)

    class_names: list[tuple[str, Path]] = []
    resource_pattern = re.compile(r"[\"'](res://[^\"']+)[\"']")

    for path in sorted(ROOT.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in TEXT_SUFFIXES:
            continue
        text = path.read_text(encoding="utf-8")
        if "\r\n" in text:
            fail(f"{path.relative_to(ROOT)}: CRLF line endings", errors)
        if not text.endswith("\n"):
            fail(f"{path.relative_to(ROOT)}: missing final newline", errors)

        for ref in resource_pattern.findall(text):
            target = ROOT / ref.removeprefix("res://")
            if not target.exists():
                fail(f"{path.relative_to(ROOT)}: missing resource {ref}", errors)

        if path.suffix == ".gd":
            check_gdscript(path, text, class_names, errors)

    for class_name, count in Counter(name for name, _ in class_names).items():
        if count > 1:
            locations = ", ".join(str(path.relative_to(ROOT)) for name, path in class_names if name == class_name)
            fail(f"duplicate class_name {class_name}: {locations}", errors)

    if errors:
        print("Wuming Zhangcheng static validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    gd_count = len(list(ROOT.rglob("*.gd")))
    resource_count = len(list(ROOT.rglob("*.tres"))) + len(list(ROOT.rglob("*.tscn")))
    print(f"Static validation passed: {gd_count} GDScript files, {resource_count} Godot resources/scenes.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
