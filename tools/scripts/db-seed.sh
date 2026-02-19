#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

echo "Running seed runner..."
dotnet run \
  --project "$ROOT/src/Lagedra.ApiGateway" \
  --launch-profile SeedRunner \
  -- seed

echo "Seed complete."
