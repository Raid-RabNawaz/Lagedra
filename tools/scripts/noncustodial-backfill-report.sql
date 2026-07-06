-- noncustodial-backfill-report.sql
-- =============================================================================
-- Option A (Non-Custodial Payments) — Workstream 7 backfill / reconciliation.
--
-- READ-ONLY. This script only runs SELECTs; it never modifies data. It produces
-- the two operational lists needed for the custodial -> non-custodial cutover:
--
--   Report 1  Hosts that are NOT Stripe-Connect-ready (charges + payouts
--             enabled) yet have saved free-text payout notes. Under the old
--             model these hosts could be paid out-of-band; under Option A a
--             booking charge is a destination charge that settles straight to
--             their connected account, so they must finish Stripe onboarding
--             before their next acceptance (instant-book + approval are gated
--             on this, but reach out proactively so they aren't blocked).
--
--   Report 2  Succeeded booking payments whose host is not Connect-ready. In
--             the old code path these settled to the PLATFORM balance (no
--             transfer_data / on_behalf_of), so the rent + deposit are parked
--             with the platform and are owed to the host. Use this list to
--             reconcile against Stripe (see the runbook in PLAN.md, WS7).
--             NOTE: this is a DB heuristic. The authoritative check is Stripe
--             side — a PaymentIntent with NO transfer_data (and/or metadata
--             payoutModel = 'direct') is the one whose funds are parked.
--
--   Report 3  Summary counts + total cents owed to hosts.
--
-- Usage (psql):
--   psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f noncustodial-backfill-report.sql
-- or via the wrappers:
--   tools/scripts/noncustodial-backfill-report.ps1
--   tools/scripts/noncustodial-backfill-report.sh
-- =============================================================================

\pset pager off

\echo '== Report 1: Hosts NOT Stripe-Connect-ready (have free-text payout notes) =='
SELECT
    hpd."HostUserId"                            AS host_user_id,
    hpd."CreatedAt"                             AS payout_notes_added,
    COALESCE(hsa."StripeAccountId", '(none)')   AS stripe_account_id,
    COALESCE(hsa."OnboardingStatus", '(none)')  AS connect_onboarding_status,
    COALESCE(hsa."ChargesEnabled", false)       AS charges_enabled,
    COALESCE(hsa."PayoutsEnabled", false)       AS payouts_enabled
FROM identity.host_payment_details hpd
LEFT JOIN identity.host_stripe_accounts hsa
       ON hsa."HostUserId" = hpd."HostUserId"
      AND hsa."IsDeleted"  = false
WHERE hpd."IsDeleted" = false
  AND (hsa."Id" IS NULL
       OR hsa."ChargesEnabled" = false
       OR hsa."PayoutsEnabled" = false)
ORDER BY hpd."CreatedAt";

\echo ''
\echo '== Report 2: Succeeded booking payments for non-Connect-ready hosts (funds likely parked in platform balance) =='
SELECT
    dpc."DealId"                                            AS deal_id,
    da."LandlordUserId"                                     AS host_user_id,
    dpc."StripePaymentIntentId"                             AS payment_intent_id,
    dpc."Status"                                            AS confirmation_status,
    dpc."FirstMonthRentCents"                               AS first_month_rent_cents,
    dpc."DepositAmountCents"                                AS deposit_cents,
    (dpc."FirstMonthRentCents" + dpc."DepositAmountCents")  AS host_owed_cents,
    dpc."CreatedAt"                                         AS paid_at
FROM activation_billing.deal_payment_confirmations dpc
JOIN activation_billing.deal_applications da
       ON da."DealId" = dpc."DealId"
LEFT JOIN identity.host_stripe_accounts hsa
       ON hsa."HostUserId" = da."LandlordUserId"
      AND hsa."IsDeleted"  = false
WHERE dpc."IsDeleted" = false
  AND dpc."StripePaymentStatus" = 'succeeded'
  AND (hsa."Id" IS NULL OR hsa."ChargesEnabled" = false)
ORDER BY dpc."CreatedAt";

\echo ''
\echo '== Report 3: Summary =='
SELECT
    (SELECT count(*)
       FROM identity.host_payment_details hpd
       LEFT JOIN identity.host_stripe_accounts hsa
              ON hsa."HostUserId" = hpd."HostUserId"
             AND hsa."IsDeleted"  = false
      WHERE hpd."IsDeleted" = false
        AND (hsa."Id" IS NULL
             OR hsa."ChargesEnabled" = false
             OR hsa."PayoutsEnabled" = false)
    )                                                        AS hosts_to_onboard,
    (SELECT count(*)
       FROM activation_billing.deal_payment_confirmations dpc
       JOIN activation_billing.deal_applications da
              ON da."DealId" = dpc."DealId"
       LEFT JOIN identity.host_stripe_accounts hsa
              ON hsa."HostUserId" = da."LandlordUserId"
             AND hsa."IsDeleted"  = false
      WHERE dpc."IsDeleted" = false
        AND dpc."StripePaymentStatus" = 'succeeded'
        AND (hsa."Id" IS NULL OR hsa."ChargesEnabled" = false)
    )                                                        AS payments_to_reconcile,
    (SELECT COALESCE(sum(dpc."FirstMonthRentCents" + dpc."DepositAmountCents"), 0)
       FROM activation_billing.deal_payment_confirmations dpc
       JOIN activation_billing.deal_applications da
              ON da."DealId" = dpc."DealId"
       LEFT JOIN identity.host_stripe_accounts hsa
              ON hsa."HostUserId" = da."LandlordUserId"
             AND hsa."IsDeleted"  = false
      WHERE dpc."IsDeleted" = false
        AND dpc."StripePaymentStatus" = 'succeeded'
        AND (hsa."Id" IS NULL OR hsa."ChargesEnabled" = false)
    )                                                        AS host_owed_cents_total;
