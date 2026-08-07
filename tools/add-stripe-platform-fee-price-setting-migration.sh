#!/usr/bin/env bash
set -euo pipefail
# Prefer the PowerShell script (generate + optional apply):
#   pwsh tools/scripts/db-migrate-stripe-platform-fee-price.ps1 -SkipUpdate
#   pwsh tools/scripts/db-migrate-stripe-platform-fee-price.ps1 -SkipAdd
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
exec pwsh "$ROOT/tools/scripts/db-migrate-stripe-platform-fee-price.ps1" "$@"
