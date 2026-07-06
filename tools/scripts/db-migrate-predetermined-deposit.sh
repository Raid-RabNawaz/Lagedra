#!/usr/bin/env bash
# Generate and apply the EF Core migrations for the Predetermined-Deposit
# Booking refactor.
#
# Four DbContexts change. Every new column is nullable or has a safe default,
# so existing rows are unaffected (behaviour-neutral back-fill):
#
#   - ListingsDbContext     : 3 nullable tier-deposit columns on listings
#   - BillingDbContext      : deal_applications snapshot + tenant/host consent
#                             columns; extended status enums
#   - TruthSurfaceDbContext : snapshot consent metadata + IsLocked/LockedAt
#   - AuditDbContext        : initial migration (new context)
#
# Run AFTER the solution builds so the EF tooling reads the latest model
# snapshot (this script builds first).
#
# Usage:
#   tools/scripts/db-migrate-predetermined-deposit.sh
#   NAME=AddPredeterminedDepositBooking tools/scripts/db-migrate-predetermined-deposit.sh
#   SKIP_ADD=1    tools/scripts/db-migrate-predetermined-deposit.sh   # apply only
#   SKIP_UPDATE=1 tools/scripts/db-migrate-predetermined-deposit.sh   # generate only
#   DRY_RUN=1     tools/scripts/db-migrate-predetermined-deposit.sh   # print only

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"
NAME="${NAME:-AddPredeterminedDepositBooking}"
SKIP_ADD="${SKIP_ADD:-0}"
SKIP_UPDATE="${SKIP_UPDATE:-0}"
DRY_RUN="${DRY_RUN:-0}"

# Parallel arrays so this works on bash 3.x (macOS) without associative arrays.
PROJECTS=(
  "src/Lagedra.Modules/ListingAndLocation"
  "src/Lagedra.Modules/ActivationAndBilling"
  "src/Lagedra.TruthSurface"
  "src/Lagedra.Modules/AuditLog"
)
CONTEXTS=(
  "ListingsDbContext"
  "BillingDbContext"
  "TruthSurfaceDbContext"
  "AuditDbContext"
)

echo "Predetermined-Deposit Booking — EF Core migrations"
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

echo "Predetermined-deposit booking migrations complete."
