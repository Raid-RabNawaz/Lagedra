#!/usr/bin/env bash
# Apply the three Phase 16 (Booking Flow Optimization) EF Core migrations.
#
# Migrations applied (idempotent — already-applied ones are skipped by EF):
#   - ListingsDbContext  : 20260512202411_AddDefaultDepositCentsToListings
#   - AuthDbContext      : 20260512202426_AddStripeCustomerIdToUsers
#   - BillingDbContext   : 20260512202439_AddStripePaymentMethodIdToApplications
#
# Run AFTER `dotnet build` so the EF tooling reads the latest model snapshot.
#
# Usage:
#   tools/scripts/db-migrate-phase16.sh
#   DRY_RUN=1 tools/scripts/db-migrate-phase16.sh   # print only

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
STARTUP="$ROOT/src/Lagedra.ApiGateway"
DRY_RUN="${DRY_RUN:-0}"

# Parallel arrays so this works on bash 3.x (macOS) without associative arrays.
PROJECTS=(
  "src/Lagedra.Modules/ListingAndLocation"
  "src/Lagedra.Auth"
  "src/Lagedra.Modules/ActivationAndBilling"
)
CONTEXTS=(
  "ListingsDbContext"
  "AuthDbContext"
  "BillingDbContext"
)
MIGRATIONS=(
  "20260512202411_AddDefaultDepositCentsToListings"
  "20260512202426_AddStripeCustomerIdToUsers"
  "20260512202439_AddStripePaymentMethodIdToApplications"
)

echo "Phase 16 — Booking Flow Optimization migrations"
echo "Startup project : $STARTUP"
echo

for i in "${!PROJECTS[@]}"; do
  proj_rel="${PROJECTS[$i]}"
  ctx="${CONTEXTS[$i]}"
  mig="${MIGRATIONS[$i]}"
  proj="$ROOT/$proj_rel"

  if [ ! -d "$proj" ]; then
    echo "Skipping $ctx — project not found at $proj"
    continue
  fi

  echo "→ $ctx :: $mig"

  if [ "$DRY_RUN" = "1" ]; then
    echo "  [dry-run] dotnet ef database update $mig --project $proj --startup-project $STARTUP --context $ctx"
  else
    dotnet ef database update "$mig" \
      --project "$proj" \
      --startup-project "$STARTUP" \
      --context "$ctx"
  fi

  echo
done

echo "Phase 16 migrations complete."
