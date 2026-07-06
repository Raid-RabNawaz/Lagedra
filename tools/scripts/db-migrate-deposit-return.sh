#!/usr/bin/env bash
# Generate and apply the EF Core migration for the deposit-return handshake
# (non-custodial, host-held deposits).
#
# One DbContext changes. Every new column is nullable with no default, so
# existing rows are unaffected (behaviour-neutral):
#
#   - BillingDbContext : deal_payment_confirmations gains
#       MoveOutInitiatedAt, MoveOutInitiatedByUserId,
#       HostConfirmedDepositReturnedAt, TenantConfirmedDepositReceivedAt,
#       DepositReturnAmountCents, DepositReturnMethod (varchar(50)),
#       DepositReturnNote (varchar(2000)), DepositReturnSettledAt,
#       DepositReturnReminderSentAt.
#
# A new DealPhase value ('AwaitingDepositReturn') is computed at read time,
# not persisted, so no data back-fill is required for existing rows. See
# tools/scripts/backfill-deposit-return-settled.sql for optionally marking
# already-refunded closed deals as settled.
#
# Run AFTER the solution builds so the EF tooling reads the latest model
# snapshot (this script builds first).
#
# Usage:
#   tools/scripts/db-migrate-deposit-return.sh
#   NAME=AddDepositReturnHandshake tools/scripts/db-migrate-deposit-return.sh
#   SKIP_ADD=1    tools/scripts/db-migrate-deposit-return.sh   # apply only
#   SKIP_UPDATE=1 tools/scripts/db-migrate-deposit-return.sh   # generate only
#   DRY_RUN=1     tools/scripts/db-migrate-deposit-return.sh   # print only

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"
NAME="${NAME:-AddDepositReturnHandshake}"
SKIP_ADD="${SKIP_ADD:-0}"
SKIP_UPDATE="${SKIP_UPDATE:-0}"
DRY_RUN="${DRY_RUN:-0}"

PROJECTS=(
  "src/Lagedra.Modules/ActivationAndBilling"
)
CONTEXTS=(
  "BillingDbContext"
)

echo "Deposit-return handshake — EF Core migrations"
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

echo "Deposit-return handshake migration complete."
