#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
./scripts/push_existing_repo.sh
printf '\nPush finished. Press Return to close.\n'
read -r _
