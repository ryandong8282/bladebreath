#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REMOTE_URL="${BLADEBREATH_REMOTE_URL:-https://github.com/ryandong8282/bladebreath.git}"

cd "$ROOT"

if ! command -v git >/dev/null 2>&1; then
  echo "Git is required." >&2
  exit 2
fi

./scripts/validate.sh

git branch -M main
if git remote get-url origin >/dev/null 2>&1; then
  git remote set-url origin "$REMOTE_URL"
else
  git remote add origin "$REMOTE_URL"
fi

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  gh auth setup-git >/dev/null 2>&1 || true
fi

git fetch origin main

if ! git merge-base --is-ancestor origin/main HEAD; then
  echo "Remote main contains commits that are not in this package." >&2
  echo "Nothing was overwritten. Pull/merge those changes before publishing." >&2
  exit 3
fi

git push --set-upstream origin main
git push origin --tags

echo
echo "Published successfully: https://github.com/ryandong8282/bladebreath"
