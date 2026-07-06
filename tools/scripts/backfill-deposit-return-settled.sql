-- backfill-deposit-return-settled.sql
-- =============================================================================
-- Deposit-Return Handshake — one-off historical backfill.
--
-- WHY: Before the handshake, a background job (DepositReturnJob) automatically
-- refunded the deposit via Stripe once a booking's billing account had been
-- Closed past the damage-claim deadline, and the deal simply showed as
-- "Closed". After the handshake ships, deal phase is computed as
-- "AwaitingDepositReturn" for any Closed booking that has a deposit but no
-- DepositReturnSettledAt. Without this backfill, historical bookings whose
-- deposits the old job already refunded would incorrectly reappear as
-- "awaiting deposit return".
--
-- This script stamps the deposit-return handshake as settled (host returned +
-- tenant received) on those historical bookings so they render as Closed again.
--
-- SAFETY:
--   * DRY RUN by default — it only SELECTs and reports what WOULD change.
--     Re-run with `-v apply=1` to actually write (the UPDATE runs in a
--     transaction and commits only on success).
--   * `cutoff` bounds the backfill to bookings CLOSED BEFORE the given time —
--     set it to your deploy time so only pre-handshake bookings are touched.
--     Defaults to 'now' (all currently-closed bookings) if not supplied.
--   * Idempotent: only rows with DepositReturnSettledAt IS NULL are touched.
--
-- Usage (psql):
--   -- preview only (no writes):
--   psql "$DATABASE_URL" -v ON_ERROR_STOP=1 \
--     -v cutoff='2026-07-03 00:00:00+00' \
--     -f backfill-deposit-return-settled.sql
--
--   -- apply:
--   psql "$DATABASE_URL" -v ON_ERROR_STOP=1 \
--     -v cutoff='2026-07-03 00:00:00+00' -v apply=1 \
--     -f backfill-deposit-return-settled.sql
--
-- or via the wrappers:
--   tools/scripts/backfill-deposit-return-settled.ps1  [-Cutoff <ts>] [-Apply]
--   tools/scripts/backfill-deposit-return-settled.sh   [--cutoff <ts>] [--apply]
-- =============================================================================

\pset pager off

-- Default the cutoff to the current transaction time if the caller didn't set
-- one. Postgres understands the special literal 'now' as "transaction start".
\if :{?cutoff}
\else
\set cutoff 'now'
\endif

\echo '== Deposit-return backfill: preview (bookings that WOULD be marked settled) =='
\echo '   cutoff (bookings closed strictly before this instant):'
\echo :'cutoff'

SELECT
    dpc."DealId"                                          AS deal_id,
    ba."EndDate"                                          AS closed_at,
    dpc."DepositAmountCents"                              AS deposit_cents,
    COALESCE((
        SELECT sum(dc."DepositDeductionCents")
        FROM activation_billing.damage_claims dc
        WHERE dc."DealId" = dpc."DealId"
          AND dc."Status" = 'Settled'
          AND dc."IsDeleted" = false
    ), 0)                                                 AS settled_deduction_cents,
    GREATEST(
        dpc."DepositAmountCents" - COALESCE((
            SELECT sum(dc."DepositDeductionCents")
            FROM activation_billing.damage_claims dc
            WHERE dc."DealId" = dpc."DealId"
              AND dc."Status" = 'Settled'
              AND dc."IsDeleted" = false
        ), 0), 0)                                         AS net_return_cents
FROM activation_billing.deal_payment_confirmations dpc
JOIN activation_billing.billing_accounts ba
       ON ba."DealId" = dpc."DealId"
WHERE dpc."IsDeleted" = false
  AND dpc."Status" = 'Confirmed'
  AND dpc."DepositAmountCents" > 0
  AND dpc."DepositReturnSettledAt" IS NULL
  AND ba."Status" = 'Closed'
  AND ba."EndDate" < :'cutoff'::timestamptz
ORDER BY ba."EndDate";

\echo ''

\if :{?apply}
\echo '== Applying backfill (writing DepositReturnSettledAt) =='
BEGIN;

UPDATE activation_billing.deal_payment_confirmations dpc
SET "DepositReturnSettledAt"           = COALESCE(ba."EndDate", now()),
    "HostConfirmedDepositReturnedAt"   = COALESCE(ba."EndDate", now()),
    "TenantConfirmedDepositReceivedAt" = COALESCE(ba."EndDate", now()),
    "MoveOutInitiatedAt"               = COALESCE(dpc."MoveOutInitiatedAt", ba."EndDate", now()),
    "DepositReturnMethod"              = 'PlatformStripe',
    "DepositReturnAmountCents"         = GREATEST(
        dpc."DepositAmountCents" - COALESCE((
            SELECT sum(dc."DepositDeductionCents")
            FROM activation_billing.damage_claims dc
            WHERE dc."DealId" = dpc."DealId"
              AND dc."Status" = 'Settled'
              AND dc."IsDeleted" = false
        ), 0), 0),
    "DepositReturnNote"                = 'Backfilled: deposit returned by the pre-handshake automatic refund job.'
FROM activation_billing.billing_accounts ba
WHERE ba."DealId" = dpc."DealId"
  AND dpc."IsDeleted" = false
  AND dpc."Status" = 'Confirmed'
  AND dpc."DepositAmountCents" > 0
  AND dpc."DepositReturnSettledAt" IS NULL
  AND ba."Status" = 'Closed'
  AND ba."EndDate" < :'cutoff'::timestamptz;

COMMIT;
\echo 'Backfill committed. Re-run the preview above; it should now return 0 rows.'
\else
\echo 'DRY RUN — no rows modified. Re-run with -v apply=1 to write the changes above.'
\endif
