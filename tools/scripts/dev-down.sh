#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

if [[ "${1:-}" == "--remove-volumes" ]]; then
  echo "Stopping services and removing volumes..."
  docker compose down -v
else
  echo "Stopping services (volumes preserved)..."
  docker compose down
fi
