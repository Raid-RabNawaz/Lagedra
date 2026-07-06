#!/usr/bin/env bash
# Generate and apply the EF Core migration for the arbitration pay-to-file flow.
#
# One DbContext changes. Every new column is nullable with no default, so
# existing rows are unaffected (behaviour-neutral):
#
#   - ArbitrationDbContext : arbitration_cases.FilingFeePaymentIntentId
#                            (varchar(255), null) + FilingFeePaidAt (timestamptz, null).
#
# A new ArbitrationStatus value ('PendingPayment') is added, but Status is
# persisted as a string, so no data back-fill is required for existing cases.
#
# Run AFTER the solution builds so the EF tooling reads the latest model
# snapshot (this script builds first).
#
# Usage:
#   tools/scripts/db-migrate-arbitration-filing-fee.sh
#   NAME=AddArbitrationFilingFeePayment tools/scripts/db-migrate-arbitration-filing-fee.sh
#   SKIP_ADD=1    tools/scripts/db-migrate-arbitration-filing-fee.sh   # apply only
#   SKIP_UPDATE=1 tools/scripts/db-migrate-arbitration-filing-fee.sh   # generate only
#   DRY_RUN=1     tools/scripts/db-migrate-arbitration-filing-fee.sh   # print only

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"
NAME="${NAME:-AddArbitrationFilingFeePayment}"
SKIP_ADD="${SKIP_ADD:-0}"
SKIP_UPDATE="${SKIP_UPDATE:-0}"
DRY_RUN="${DRY_RUN:-0}"

PROJECTS=(
  "src/Lagedra.Modules/Arbitration"
)
CONTEXTS=(
  "ArbitrationDbContext"
)

echo "Arbitration pay-to-file — EF Core migrations"
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

echo "Arbitration filing-fee migration complete."
