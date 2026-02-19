#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

failed=0

echo "── .NET format check ────────────────────────────────"
dotnet format Lagedra.sln --verify-no-changes --severity warn || failed=1

echo "── pnpm lint ────────────────────────────────────────"
pnpm --recursive lint || failed=1

if [ "$failed" -ne 0 ]; then
  echo "Lint failed." >&2
  exit 1
fi

echo "All lint checks passed."
