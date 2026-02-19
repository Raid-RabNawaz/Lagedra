#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

dotnet_only=false
frontend_only=false
coverage=false

for arg in "$@"; do
  case $arg in
    --dotnet-only)   dotnet_only=true ;;
    --frontend-only) frontend_only=true ;;
    --coverage)      coverage=true ;;
  esac
done

failed=0

if [ "$frontend_only" = false ]; then
  echo "── .NET tests ───────────────────────────────────────"
  if [ "$coverage" = true ]; then
    dotnet test Lagedra.sln --no-build -c Release \
      --collect:"XPlat Code Coverage" \
      --results-directory coverage/dotnet || failed=1
  else
    dotnet test Lagedra.sln --no-build -c Release || failed=1
  fi
fi

if [ "$dotnet_only" = false ]; then
  echo "── Frontend tests ───────────────────────────────────"
  pnpm --recursive test --run || failed=1
fi

if [ "$failed" -ne 0 ]; then
  echo "Tests failed." >&2
  exit 1
fi

echo "All tests passed."
