#!/usr/bin/env bash
# Deposit-Return Handshake — one-off historical backfill.
#
# Marks the deposit-return handshake as settled on bookings the pre-handshake
# automatic refund job (DepositReturnJob) already refunded, so they keep
# rendering as "Closed" instead of newly surfacing as "AwaitingDepositReturn".
#
# Runs backfill-deposit-return-settled.sql. DRY RUN by default (SELECTs only,
# prints what would change). Pass --apply to write. The write runs in a
# transaction and only touches Closed bookings with a deposit and no existing
# DepositReturnSettledAt (idempotent).
#
# Usage:
#   tools/scripts/backfill-deposit-return-settled.sh [--cutoff '<ts>'] [--apply]
#   CONN_STRING=postgresql://user:pw@host:5432/db tools/scripts/backfill-deposit-return-settled.sh --apply
#
# Options:
#   --cutoff <ts>   Only backfill bookings closed strictly before <ts> (set to
#                   your deploy time). Defaults to now.
#   --apply         Actually write the changes (default is a read-only preview).

set -euo pipefail

CONN_STRING="${CONN_STRING:-${DATABASE_URL:-postgresql://lagedra:lagedra_pw@localhost:5432/lagedra_db}}"
CUTOFF=""
APPLY=0

while [ $# -gt 0 ]; do
  case "$1" in
    --cutoff) CUTOFF="$2"; shift 2 ;;
    --apply)  APPLY=1; shift ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

if ! command -v psql >/dev/null 2>&1; then
  echo "psql not found on PATH. Install the PostgreSQL client or run the .sql file with your own tool." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL="$SCRIPT_DIR/backfill-deposit-return-settled.sql"
[ -f "$SQL" ] || { echo "Cannot find $SQL" >&2; exit 1; }

ARGS=("$CONN_STRING" -v ON_ERROR_STOP=1)
[ -n "$CUTOFF" ] && ARGS+=(-v "cutoff=$CUTOFF")
[ "$APPLY" = "1" ] && ARGS+=(-v apply=1)
ARGS+=(-f "$SQL")

if [ "$APPLY" = "1" ]; then
  echo "Deposit-return backfill — APPLYING changes"
else
  echo "Deposit-return backfill — DRY RUN (read-only preview)"
fi
echo

psql "${ARGS[@]}"
