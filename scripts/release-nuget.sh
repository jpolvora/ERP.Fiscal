#!/usr/bin/env bash
# Wrapper para o script de release NuGet (bash / Git Bash no Windows).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec bash "$SCRIPT_DIR/../.agents/skills/release-nuget-package/scripts/release.sh" "$@"
