# Lagedra Release Notes — Friday 4 July 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Cumulative changes on branch `dev` since the 21 May 2026 release (`RELEASE_NOTES_2026-05-21.md`), including commits `18db292`, `1bc1fa8`, and the pending working tree staged for this deployment. Single deployment covers API gateway, worker, modules, and web client.

**Program references:** Phase 16/17 booking & inquiry (baseline), predetermined-deposit booking, non-custodial payments, channel integration (OwnerRez), host platform billing, arbitration filing fees, host payout readiness.

---

## Executive summary

This release advances Lagedra from **operational arbitration and booking pre-flight** into **tier-based predetermined deposits**, **non-custodial move-out and deposit-return handshakes**, **OwnerRez PMS sync**, and **host platform-fee transparency**. Tenants receive upfront **reservation previews** with verification-tier deposit selection and Truth Surface consent at apply time. Hosts gain **payout-readiness gates**, **platform fee statements**, **OwnerRez connection and listing import**, and richer **application triage** with trust-level and profile context. Platform administrators receive **grouped platform settings**, **protocol-fee vs Stripe reconciliation**, and expanded listing review tools. Arbitration adds a **pay-to-file** gate before cases enter the evidence workflow. A **role-aware dashboard** separates traveling vs hosting modes for members who do both.

---

## Highlights

### Predetermined-deposit booking & reservation preview

- **Verification-tier deposits** — Listings define deposit amounts for unverified, background-verified, and partner-guaranteed tenants; server selects the applicable tier via `DepositSelectionService` with a human-readable rationale.
- **Reservation preview** — `GET /v1/applications/preview` returns itemised rent, deposit, insurance, tenant service fee, protocol fee disclosure, and total before the tenant commits.
- **Truth Surface consent at apply and approve** — Consent version `ts-consent-v3` with IP and user-agent capture on applications and sealed snapshots; host approval requires matching consent attestation.
- **Stale request expiry** — `ExpireStaleBookingRequestsJob` expires pending applications after the configured window (default 72 hours).
- **UI** — `BookingPanel` and two-step `ApplyDialog` on listing detail; `TrustLevelBadge`, `ApplicationProfilePanel`, and `ApplicationStatsSummary` on host application inbox; inline approve with payout-readiness gate.

### Non-custodial payments & deposit return

- **Host-held deposit model** — Platform does not custody security deposits; checkout and billing surfaces include non-custodial disclaimers.
- **Move-out handshake** — New deal phase **AwaitingDepositReturn**: host confirms deposit returned, tenant confirms receipt; admin force-settle path for disputes.
- **API routes:**
  - `POST /v1/deals/{dealId}/payment/begin-move-out`
  - `POST /v1/deals/{dealId}/payment/deposit-return/host-confirm`
  - `POST /v1/deals/{dealId}/payment/deposit-return/tenant-confirm`
  - `POST /v1/admin/deals/{dealId}/force-deposit-return`
- **UI** — `DepositReturnPanel` on `/app/deals/:dealId`; deposit-return category and penalty type in arbitration verdict flows.
- **Jobs** — `DepositReturnJob` sends reminder nudges on open handshakes.

### Channel integration / PMS (OwnerRez)

- **ChannelIntegration module** — Connections, listing maps, booking links, and sync cursors in the `channel_integration` schema.
- **OwnerRez provider** — HAXML content pull and HAOLB booking push on payment confirmation (Merchant-of-Record channel model).
- **Worker jobs** — `ChannelAvailabilitySyncJob`, `ChannelBookingUpdateJob`, `ChannelContentSyncJob`; `OnPaymentConfirmedPublishToChannelHandler` publishes confirmed bookings.
- **API routes:**
  - `GET /v1/channels/providers`
  - `GET|POST /v1/channels/`
  - `POST /v1/channels/{id}/enable|disable|sync`
  - `GET /v1/channels/{id}/listings`
  - `GET /channels/{provider}/listing/{externalId}` (anonymous redirect to marketplace listing)
- **UI** — `/app/channels` for connect, sync, and imported listing map; channel sync shortcut on hosting dashboard.

### Host billing & platform fee reconciliation

- **Host billing statement** — `GET /v1/me/billing/statement` returns active bookings, configured monthly protocol fee, projected monthly total, invoice history, and outstanding failed charges.
- **Protocol fee reconciliation** — `GET /v1/admin/protocol-fee-reconciliation` compares platform setting `protocol_fee.monthly_cents` against the configured Stripe price; drift surfaced to admins.
- **UI** — `/app/billing` (`HostBillingStatementPage`); `ProtocolFeeReconciliationBanner` on admin dashboard and platform settings.
- **Jobs** — `BillingReconciliationJob` flags failed host platform-fee invoices.

### Host payout readiness (Stripe Connect)

- **Payout requirement tracking** — `BankAccountStatus` and `TaxStatus` on host Stripe accounts, synced from Stripe.
- **Provider-agnostic payout routes** — `POST /v1/hosts/payouts/start`, refresh link, and status (legacy `/v1/hosts/stripe/*` retained).
- **`StripeConnectUrlValidator`** — Return and refresh URLs must resolve to `/app/payout-setup`.
- **UI** — Redesigned `/app/payout-setup`; `HostPayoutReadinessNotice` blocks host approve and listing publish until payout-ready.

### Arbitration filing fee

- **Pay-to-file gate** — New status **PendingPayment**; case remains inert until the filer completes Stripe checkout (zero-fee cases skip).
- **API** — `POST /v1/arbitration/cases/{caseId}/filing-fee/checkout`; webhook handler `OnArbitrationFilingFeePaidHandler` transitions case to **Filed**.
- **UI** — `ArbitrationFeeCheckout` on case detail; case list filter for awaiting payment; workflow banner and timeline updates.
- **Settings** — `arbitration_fee.protocol_adjudication_cents` and `arbitration_fee.binding_arbitration_cents` editable in platform settings.

### Truth Surface & agreement document

- **`AgreementDocument`** — Human-readable rendering of sealed snapshot JSON (parties, stay, financials, deposit-return window) without exposing internal hash/schema plumbing.
- **`CreateAndSealTruthSurfaceCommand`** — Consent sealing, deposit-return window from platform settings, predetermined-deposit fields in canonical payload.
- **UI** — Integrated in `TruthSnapshotViewer` and confirmation pages at `/app/truth-surface/:snapshotId` and deal-scoped truth-surface routes.

### Dashboard, profile, and listing tools

- **Role-aware dashboard** at `/app` — `TravelingDashboard` / `HostingDashboard` with mode toggle for members who host and travel; `RoleDashboard` for arbitrators, admins, and partners.
- **Profile health** — `ProfileHealthCard` and completeness scoring (75% threshold mirrors server gate on submit-for-review).
- **Public profile** — `GET /v1/auth/users/{userId}/public-profile`; counterparty preview on application cards.
- **Listing import from URL** — `POST /v1/listings/import-from-url` (rate-limited); ScrapingAnt + OpenGraph/JSON-LD extraction; `ImportFromUrlPanel` on `/app/listings/new`.
- **Edit-listing geolocation** — Map pin vs address validation on `/app/listings/:id/edit`.
- **Tenant service fee** — Platform revenue line on checkout; settings `service_fee.tenant_bps`, `service_fee.tenant_flat_cents`, `service_fee.tenant_use_flat`.

### Admin & platform settings

- **Platform settings page** — Grouped editors for protocol fee, arbitration fees, tenant service fee, Stripe, payment timing, and host enforcement at `/app/admin/settings`.
- **Listing review** — Enhanced `/app/admin/listing-review` workflow.
- **Insurance flag removal** — Per-listing `InsuranceRequired` dropped; insurance remains fee-calculated at quote time.

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/applications/preview` | Authenticated | Reservation preview before apply. |
| `GET` | `/v1/me/billing/statement` | Member (host) | Host platform-fee statement. |
| `GET` | `/v1/admin/protocol-fee-reconciliation` | Platform admin | Protocol fee vs Stripe drift. |
| `GET` | `/v1/channels/providers` | Member | Available PMS providers. |
| `GET\|POST` | `/v1/channels/` | Member | List / connect channel accounts. |
| `POST` | `/v1/channels/{id}/sync` | Member | On-demand content sync. |
| `POST` | `/v1/deals/{dealId}/payment/begin-move-out` | Deal party | Start move-out / deposit-return flow. |
| `POST` | `/v1/deals/{dealId}/payment/deposit-return/host-confirm` | Landlord | Host confirms deposit returned. |
| `POST` | `/v1/deals/{dealId}/payment/deposit-return/tenant-confirm` | Tenant | Tenant confirms deposit received. |
| `POST` | `/v1/admin/deals/{dealId}/force-deposit-return` | Platform admin | Admin force-settle handshake. |
| `POST` | `/v1/arbitration/cases/{caseId}/filing-fee/checkout` | Filer | Stripe checkout for filing fee. |
| `POST` | `/v1/listings/import-from-url` | Member | Draft listing from external URL. |
| `GET` | `/v1/auth/users/{userId}/public-profile` | Authenticated | Counterparty profile preview. |
| `GET\|POST` | `/v1/hosts/payouts/*` | Member | Payout onboarding (Stripe Connect). |

*Extended endpoints:* `POST /v1/applications` and `POST /v1/applications/{id}/approve` carry consent and pricing snapshots; Truth Surface confirm/receipt routes render predetermined-deposit fields.*

---

## Frontend changes

### New or substantially new pages & components

| Route / surface | Component | Purpose |
|-----------------|-----------|---------|
| `/app` | `TravelingDashboard`, `HostingDashboard`, `RoleDashboard` | Role- and mode-aware home. |
| `/app/channels` | `ChannelsPage` | OwnerRez connect, sync, listing map. |
| `/app/billing` | `HostBillingStatementPage` | Host platform fee statement. |
| `/app/payout-setup` | `HostStripeOnboardingPage` | Stripe Connect onboarding & status. |
| `/app/deals/:dealId` | `DepositReturnPanel` | Move-out deposit handshake. |
| `/app/arbitration/:caseId` | `ArbitrationFeeCheckout` | Filing fee payment. |
| `/app/admin/settings` | `PlatformSettingsPage` | Grouped platform configuration. |
| `/app/listings/new` | `ImportFromUrlPanel` | Import draft from external listing URL. |
| Listing detail | `BookingPanel`, `ApplyDialog` | Date, quote, consent, apply with card on file. |
| Applications | `ApplicationCard`, `TrustLevelBadge`, `ApplicationProfilePanel` | Host triage with trust tier and profile. |
| Truth Surface | `AgreementDocument` | Readable agreement from sealed snapshot. |
| Shared | `HostPayoutReadinessNotice`, `FilterTabs`, `PageHeader` | Cross-cutting UX and gating. |

---

## Database & schema

Apply in dependency order. Use the scripts under `tools/scripts/` where noted.

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260608171013_AddTenantServiceFee` | Billing | `ServiceFeeCents` on payment confirmations. |
| `20260609230854_AddBookingRequestGuestCountAndMessage` | Billing | Guest count and message on applications. |
| `20260610195701_InitialCreateChannelIntegration` | Channel | PMS connections, maps, booking links. |
| `20260612183514_DropListingInsuranceRequired` | Listings | Remove per-listing insurance-required flag. |
| `20260619181334_AddPredeterminedDepositBooking` | Listings | Tier deposit columns on listings. |
| `20260619181352_AddPredeterminedDepositBooking` | Billing | Application pricing, consent, and snapshot fields. |
| `20260619181407_AddPredeterminedDepositBooking` | TruthSurface | Consent metadata; snapshot lock columns. |
| `20260619181422_AddPredeterminedDepositBooking` | AuditLog | Initial audit events and outbox schema. |
| `20260623191347_AddHostPayoutRequirementStatus` | Identity | Bank/tax payout readiness on host accounts. |
| `20260623221939_AddArbitrationFilingFeePayment` | Arbitration | Filing fee PaymentIntent tracking on cases. |
| **`AddDepositReturnHandshake`** *(generate before deploy)* | Billing | Move-out and deposit-return handshake columns. |

**Scripts:**

```powershell
tools/scripts/db-migrate-channel-integration.ps1
tools/scripts/db-migrate-predetermined-deposit.ps1
tools/scripts/db-migrate-noncustodial-payments.ps1
tools/scripts/db-migrate-arbitration-filing-fee.ps1
tools/scripts/db-migrate-deposit-return.ps1   # generate + apply
tools/scripts/backfill-deposit-return-settled.ps1   # post-deploy if needed
```

---

## Security & privacy posture

- **Non-custodial deposits** — Security deposits are host-held; platform surfaces disclaimers and two-party confirmation rather than automatic refunds.
- **Truth Surface consent** — Apply and approve flows capture consent version, IP, and user agent; snapshots can be locked after sealing.
- **Payout-readiness gates** — Hosts cannot approve bookings or publish listings until Stripe Connect requirements are satisfied, reducing payout-failure risk.
- **Public profile** — Counterparty preview exposes only fields intended for booking context; full profile remains on authenticated profile routes.
- **Channel credentials** — OwnerRez API keys stored per connection; sync endpoints require member auth on the owning account.
- **Secrets** — `appsettings.Development.json` is gitignored; use `appsettings.Development.json.example` locally. Rotate any keys previously committed to history.

---

## Configuration & dependencies

- **Stripe** — SetupIntent (apply), PaymentIntent (checkout, arbitration filing fee), Connect (host payouts), platform fee price ID for reconciliation.
- **OwnerRez** — Channel provider credentials per host connection; worker must be running for scheduled sync jobs.
- **Platform settings keys** — `protocol_fee.monthly_cents`, `service_fee.*`, `arbitration_fee.*`, payment timing, deposit-return window.
- **Feature flags** — `FeatureFlags:BookingFlow.V2` for progressive rollout where configured.
- **ScrapingAnt** — Listing import from URL (optional; rate-limited).

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests (`tests/Lagedra.Tests.Unit`).
2. **Migrate** databases in order (channel → predetermined deposit → identity payout status → arbitration filing fee → deposit return).
3. **Configure** Stripe, OwnerRez, platform settings, and feature flags per environment.
4. **Deploy** API + worker, then web assets.
5. **Smoke tests**
   - **Tenant:** Select dates on listing detail → preview quote → apply with SetupIntent → confirm Truth Surface consent captured.
   - **Host:** Complete payout setup → approve application (blocked until ready) → view `/app/billing` statement.
   - **Move-out:** Begin move-out on active deal → host confirm → tenant confirm → deal phase advances.
   - **Channels:** Connect OwnerRez test account → sync → verify listing map and redirect URL.
   - **Arbitration:** File case → pay filing fee → case moves to **Filed** → evidence phase opens.
   - **Admin:** Open protocol-fee reconciliation banner; edit platform settings; force deposit return on test deal.
   - **Regression:** One-tap host approve (`/host/approve`), pre-booking inquiry sessions, structured verdict issuance.

---

## Known limitations & follow-up (engineering)

- **Deposit-return migration** — Generate and apply `AddDepositReturnHandshake` before production; backfill script available for settled historical deals.
- **Penalty enforcement** — Arbitration penalties are stored and displayed; automatic financial enforcement from verdict issuance remains a follow-up.
- **Channel coverage** — OwnerRez is the first provider; additional PMS adapters follow the same `IChannelProvider` contract.
- **Pending commit** — A portion of this scope may still be uncommitted on `dev`; ensure working tree is committed and CI green before tagging.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-07-04 | Initial formal notes for predetermined-deposit, non-custodial, PMS, and host billing tranche. |

---

*End of release notes.*
