#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
python3 "$ROOT/scripts/static_validate.py"

if command -v node >/dev/null 2>&1; then
  node "$ROOT/scripts/validate_web.js"
else
  echo "Node.js not found; browser combat validation skipped."
fi

if [[ -n "${GODOT_BIN:-}" ]]; then
  GODOT="$GODOT_BIN"
elif command -v godot4 >/dev/null 2>&1; then
  GODOT="$(command -v godot4)"
elif command -v godot >/dev/null 2>&1; then
  GODOT="$(command -v godot)"
else
  echo "Static/browser checks passed; Godot runtime smoke test skipped."
  echo "Set GODOT_BIN=/absolute/path/to/Godot to run engine validation."
  if [[ "${STRICT_GODOT:-0}" == "1" ]]; then
    exit 2
  fi
  exit 0
fi

"$GODOT" --headless --path "$ROOT" --editor --quit
"$GODOT" --headless --path "$ROOT" --quit-after 180

echo "Wuming Zhangcheng engine validation passed."
