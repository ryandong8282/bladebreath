#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${1:-$ROOT/../Wuming-Zhangcheng-source.zip}"
cd "$(dirname "$ROOT")"
rm -f "$OUT"
zip -qr "$OUT" "$(basename "$ROOT")" \
  -x '*/.git/*' '*/.git/' \
  '*/.godot/*' '*/.godot/' \
  '*/build/*' '*/builds/*' '*/exports/*' \
  '*/__pycache__/*' '*.pyc'
echo "$OUT"
