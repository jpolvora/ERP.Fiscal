#!/usr/bin/env bash
# Wrapper para diff-routes.py (bash / Git Bash no Windows).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec python "$SCRIPT_DIR/diff-routes.py" "$@"
