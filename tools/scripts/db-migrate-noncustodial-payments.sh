#!/usr/bin/env bash
# Generate and apply the EF Core migration for Option A (Non-Custodial
# Payments) — Workstream 0.
#
# One DbContext changes. Every new column has a safe default, so existing rows
# are unaffected (behaviour-neutral back-fill):
#
#   - IdentityDbContext : host_stripe_accounts.TaxStatus + BankAccountStatus
#                         (varchar(30), default 'Unknown'; populated on the next
#                         Stripe account status sync).
#
# Run AFTER the solution builds so the EF tooling reads the latest model
# snapshot (this script builds first).
#
# Usage:
#   tools/scripts/db-migrate-noncustodial-payments.sh
#   NAME=AddHostPayoutRequirementStatus tools/scripts/db-migrate-noncustodial-payments.sh
#   SKIP_ADD=1    tools/scripts/db-migrate-noncustodial-payments.sh   # apply only
#   SKIP_UPDATE=1 tools/scripts/db-migrate-noncustodial-payments.sh   # generate only
#   DRY_RUN=1     tools/scripts/db-migrate-noncustodial-payments.sh   # print only

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"
NAME="${NAME:-AddHostPayoutRequirementStatus}"
SKIP_ADD="${SKIP_ADD:-0}"
SKIP_UPDATE="${SKIP_UPDATE:-0}"
DRY_RUN="${DRY_RUN:-0}"

PROJECTS=(
  "src/Lagedra.Modules/IdentityAndVerification"
)
CONTEXTS=(
  "IdentityDbContext"
)

echo "Option A (Non-Custodial Payments) — EF Core migrations"
echo "Startup project : $STARTUP"
echo "Migration name  : $NAME"
echo

echo "Building solution..."
if [ "$DRY_RUN" = "1" ]; then
  echo "  [dry-run] dotnet build $ROOT/Lagedra.sln --nologo --verbosity quiet"
else
  dotnet build "$ROOT/Lagedra.sln" --nologo --verbosity quiet
fi
echo

if [ "$SKIP_ADD" != "1" ]; then
  for i in "${!PROJECTS[@]}"; do
    proj="$ROOT/${PROJECTS[$i]}"
    ctx="${CONTEXTS[$i]}"
    echo "→ add $NAME :: $ctx"
    if [ "$DRY_RUN" = "1" ]; then
      echo "  [dry-run] dotnet ef migrations add $NAME --project $proj --startup-project $STARTUP --context $ctx"
    else
      dotnet ef migrations add "$NAME" \
        --project "$proj" \
        --startup-project "$STARTUP" \
        --context "$ctx"
    fi
    echo
  done
fi

if [ "$SKIP_UPDATE" != "1" ]; then
  for i in "${!PROJECTS[@]}"; do
    proj="$ROOT/${PROJECTS[$i]}"
    ctx="${CONTEXTS[$i]}"
    echo "→ database update :: $ctx"
    if [ "$DRY_RUN" = "1" ]; then
      echo "  [dry-run] dotnet ef database update --project $proj --startup-project $STARTUP --context $ctx"
    else
      dotnet ef database update \
        --project "$proj" \
        --startup-project "$STARTUP" \
        --context "$ctx"
    fi
    echo
  done
fi

echo "Non-custodial payments migration complete."
