#!/usr/bin/env bash
# Option A (Non-Custodial Payments) — Workstream 7 backfill / reconciliation report.
#
# READ-ONLY: runs noncustodial-backfill-report.sql (SELECTs only) and prints the
# two operational lists for the custodial -> non-custodial cutover:
#
#   1. Hosts that are not Stripe-Connect-ready (must finish onboarding).
#   2. Succeeded booking payments whose host is not Connect-ready (rent + deposit
#      likely parked in the platform balance — reconcile in Stripe).
#
# See the WS7 runbook in PLAN.md for what to do with each list.
#
# Usage:
#   tools/scripts/noncustodial-backfill-report.sh
#   DATABASE_URL="postgresql://user:pw@host:5432/lagedra_db" tools/scripts/noncustodial-backfill-report.sh

set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL="$HERE/noncustodial-backfill-report.sql"
CONN="${DATABASE_URL:-postgresql://lagedra:lagedra_pw@localhost:5432/lagedra_db}"

if ! command -v psql >/dev/null 2>&1; then
  echo "psql not found on PATH. Install the PostgreSQL client or run the .sql file with your own tool." >&2
  exit 1
fi
if [ ! -f "$SQL" ]; then
  echo "Cannot find $SQL" >&2
  exit 1
fi

echo "Non-custodial backfill report (read-only)"
echo

psql "$CONN" -v ON_ERROR_STOP=1 -f "$SQL"
