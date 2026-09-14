# Lagedra Release Notes — Saturday 12 September 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Working-tree changes on branch `dev` since commit `bb4b3b6` (“Ship the 5 Sep release”) / `RELEASE_NOTES_2026-09-05.md`. This document covers **net-new work only**. Items documented on 5 September (public legal pages, Truvi stay protection core, custom lease baseline, admin listing-review filters/bulk, A2P SMS campaign consent, Streamline docs, CI test gate) are **not repeated** here; see that note if this deploy also ships that backlog for the first time.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release introduces **rules-based trust tiers**, **host-paid tenant screening** (Checkr) on booking applications, and **richer stay-protection and admin-queue UX**. Guests are classified as **Unverified**, **Verified**, **Trusted**, **Elite**, or **Blocklisted**; blocklisted guests cannot apply. Hosts can **request a background/credit check** on an application, pay a platform screening fee via Stripe, and review the report. Stay protection on deals now has a dedicated card, damage-claim deep link, and clearer status headlines. Lease preview supports a **jurisdiction specimen** even when a host-provided file is attached. Admin fraud, compliance, and insurance-unknown queues gain **detail dialogs and resolve actions**. Inquiry threads show **guest profile context** for hosts; inquiry-volume abuse raises an integrity flag. Outbound mail uses a **shared branded layout**. After deploy, the SPA **recovers from stale Vite chunks** without a hard refresh loop.

---

## Highlights

### Trust tiers v2

| Tier | Meaning (summary) |
|------|-------------------|
| **Unverified** | Default; listing unverified deposit band. |
| **Verified** | Identity / KYC path without Trusted history. |
| **Trusted** | Multi-factor: completed stays, host reviews, optional recent screening Pass (TTL). Uses former “background verified” deposit column. |
| **Elite** | Trusted history **plus** partner endorsement; screening alone never grants Elite. Uses former “partner guaranteed” deposit column. |
| **Blocklisted** | Booking restriction or platform ban; **apply is rejected** (`Application.Blocklisted`). |

**Policy:** `TrustTiersPolicy` / `TrustTiers:*` in configuration. Resolver considers stays, host→guest reviews, violations, arbitration standing, partner endorsements, and screening pass age.

**Surfaces:**

| Route | Change |
|-------|--------|
| `/app/verification` | Self-serve background-check consent removed; shows **Lagedra trust tier** (`useMyVerificationTier`). |
| `/app/trust-ledger`, deal trust ledger | `TrustLevelBadge` can show **last screened** date. |
| Apply dialog | Blocks continue when preview tier is **Blocklisted**. |
| Listing create/edit | Deposit labels **Trusted** / **Elite**. |

**API:** `GET /v1/me/verification-tier` — semantic enum change; adds `lastScreenedAt`.

**Data:** `tools/scripts/db-migrate-trust-tiers.ps1` backfills `TenantVerificationTierAtRequest` (`BackgroundVerified` → `Trusted`, `PartnerGuaranteed` → `Elite`). No EF schema change.

### Host-paid tenant screening (Checkr)

Distinct from Sep 5 **Truvi stay protection** on deals. This is **application-scoped** background/credit screening, **host-paid**.

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/applications/{id}/screening/checkout` | Host / owner | Stripe PaymentIntent (`clientSecret`) to start screening. |
| `GET` | `/v1/applications/{id}/screening` | Authorized party | Status, guest apply URL, host fee, permissions. |
| `GET` | `/v1/applications/{id}/screening/report` | Authorized party | Report payload. |
| `GET` | `/v1/applications/{id}/screening/report/pdf` | Authorized party | PDF download. |
| `POST` | `/v1/webhooks/screening/{provider}` | Provider signature | Checkr (or configured provider) webhooks. |

**UI:** `ApplicationScreeningPanel` on `/app/applications/:id`; `ScreeningStatusBadge` on host application cards.

**Ops:** `RefundIncompleteScreeningOrdersJob` refunds unpaid/incomplete apply after configured days. Platform setting `screening.host_fee_cents` (default **4500**) seeded at API startup; editable under `/app/admin/settings`.

**Notifications:** `screening_apply_link_ready`, `screening_incomplete_refunded` (email + SMS variants).

**Config:** `Screening:*` (`Provider`, `HostFeeCents`, `IncompleteApplyRefundAfterDays`); `Checkr:*`. ECS helpers `deploy/aws/set-prod-checkr-keys.ps1`, `set-staging-checkr-keys.ps1`.

**Migrations:**

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260909184506_AddScreeningOrders` | Identity | `identity.screening_orders`. |
| `20260911171818_AddScreeningOrderFailureReason` | Identity | `LastFailureReason` on screening orders. |

**Scripts:** `db-migrate-tenant-screening.ps1`, `db-migrate-screening-failure-reason.ps1`.

### Stay protection — deal UX (Truvi core unchanged)

| Item | Change |
|------|--------|
| **Fee calculator** | Returns **$0 / none** when Truvi API is not callable (`CanCallApi` false) so quotes do not show a fee until screening can run. |
| **Verification request** | Includes `listingId` / `listingName` (title or street/city fallback). |
| **`DealStayProtectionCard`** | New card on `/app/deals/:dealId` — status, rescreen when flagged, claim guidance. |
| **Damage claim** | `/app/deals/:dealId/billing?claim=1` opens `FileDamageClaimDialog` for confirmed-payment hosts. |
| **Billing** | Stay-protection / claim path also available in **AwaitingDepositReturn**. |
| **Badges** | Shared `stayProtectionStatus()` headlines (Protected / flagged / etc.). |

### Lease specimen preview

**Sep 5:** custom lease upload + listing preview.

**This release:** `GET /v1/lease-agreements/listings/{listingId}/preview?specimen=true|false` — `specimen=true` forces the jurisdiction template specimen even when a host file is attached. Wizard and listing detail show a **standard lease specimen** block (`ListingLeaseSpecimenPreview`).

### Applications, deals & inquiry UX

| Surface | Change |
|---------|--------|
| `/app/applications` | Groups sorted by latest submission; request timestamps visible. |
| `/app/owner-consents` | Sorted by `submittedAt` descending. |
| Deal cards / dashboards | **Requested** datetime; stay-protection badge on deal header. |
| My deals / attention | Sort tie-breaker uses `createdAt` (newest first). |
| Inquiry (deal + listing session) | `InquiryGuestDetailsCard` — guest profile + reputation for host/partner. |
| Inquiry participants | Guest display name from public profile. |
| Inquiry / deposit / verdict / claim | Shared `NumberInput` for money fields. |

**Backend:** inquiry volume abuse → `InquiryVolumeAbuseDetectedEvent` → integrity flag type **`InquiryAbuse`**.

### Admin queues & analytics

| Route | Change |
|-------|--------|
| `/app/admin/fraud-flags` | Merges integrity + identity flags; detail dialog; resolve identity flags via `POST /v1/admin/identity/fraud-flags/{id}/resolve`. |
| `/app/admin/compliance-violations` | Row detail dialog; listing/deal context; richer `ViolationDto`. |
| `/app/admin/insurance-queue` | Enriched rows (tenant, listing, dates, screening); resolve dialog → `POST /v1/admin/insurance/{dealId}/resolve`. |
| `/app/admin/listing-analytics` | **Location** column + CSV city/state/country. |
| `/app/admin/settings` | Label for `screening.host_fee_cents`. |
| Definitions / blog / jurisdiction | Shared number inputs for numeric fields. |

**Shared:** `AdminDetailList`, `adminQueueActions`.

### Host payouts (Stripe Connect)

- `/app/payout-setup` surfaces **status load errors**; `GET` status treats **404** as no account.
- Onboard has a **30s timeout**; email required (`HostStripe.EmailRequired`).
- Non-NotFound status errors return **400**.

### Marketplace, email & reliability

| Area | Change |
|------|--------|
| Listing cards | 4:3 photo; mobile footer not clipped (no fixed 4:5 card). |
| Email | Shared `EmailLayout` + branded assets under `apps/web/public/email/`. |
| SPA | `staleChunk.ts` — one reload per 30s on Vite stale lazy chunks (`ErrorBoundary`, `PageBoundary`, `RouteErrorBoundary`, `vite:preloadError`). |
| Frontend deploy | `08-frontend.sh` — long-cache assets; `index.html` uploaded last with `must-revalidate`. |
| NAT | `deploy/aws/11-egress-nat.sh` for outbound provider APIs (Checkr / Truvi). |

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/applications/{id}/screening/checkout` | Host / owner | Start host-paid screening checkout. |
| `GET` | `/v1/applications/{id}/screening` | Authorized | Screening order status. |
| `GET` | `/v1/applications/{id}/screening/report` | Authorized | Screening report. |
| `GET` | `/v1/applications/{id}/screening/report/pdf` | Authorized | Report PDF. |
| `POST` | `/v1/webhooks/screening/{provider}` | Provider | Screening provider webhooks. |
| `GET` | `/v1/admin/identity/fraud-flags` | Platform admin | Identity fraud flags for merged queue. |
| `POST` | `/v1/admin/identity/fraud-flags/{id}/resolve` | Platform admin | Resolve identity fraud flag. |

*Extended:* `GET /v1/me/verification-tier` (`lastScreenedAt`, new tier values); lease preview `?specimen=`; insurance admin resolve + enriched queue DTO; application list fields `screeningOrderStatus`, `screeningResult`, `lastScreenedAt`; listing analytics location columns.*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| `/app/verification` | Trust tier display; no guest-initiated Checkr consent. |
| `/app/applications/:id` | Host screening panel + report viewer. |
| `/app/applications` | Screening badges; recency sort. |
| `/app/deals/:dealId` | Stay protection card; requested time; protection badge. |
| `/app/deals/:dealId/billing?claim=1` | Auto-open damage claim. |
| Inquiry threads | Guest details card; named participants. |
| Listing wizard / detail | Lease specimen preview. |
| Admin fraud / violations / insurance | Detail + resolve UX. |
| `/app/payout-setup` | Error and 404 handling. |
| Marketplace cards | 4:3 layout polish. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260909184506_AddScreeningOrders` | Identity | Host-paid screening orders. |
| `20260911171818_AddScreeningOrderFailureReason` | Identity | Last failure reason on orders. |

**Apply (after Sep 5 migrations if not already applied):**

```powershell
pwsh tools/scripts/db-migrate-tenant-screening.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-screening-failure-reason.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-trust-tiers.ps1
```

Confirm Sep 5 custom-lease, Truvi screening, and SMS consent migrations are already applied in the target environment.

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `TrustTiers:*` | Thresholds / TTL for Trusted and Elite. |
| `Screening:Provider` | `Checkr` or `NoOp`. |
| `Screening:HostFeeCents` / `screening.host_fee_cents` | Host checkout amount. |
| `Screening:IncompleteApplyRefundAfterDays` | Auto-refund window. |
| `Checkr:*` | Checkr API credentials (SSM / `set-*-checkr-keys.ps1`). |
| NAT egress | Required if Checkr/Truvi cannot be reached from private subnets. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests (screening, trust tiers, stale chunk).
2. **Migrate** screening orders → failure reason → trust-tier data backfill.
3. **Configure** Checkr keys, screening fee, TrustTiers, NAT if needed.
4. **Deploy** API + worker, then web (`index.html` last).
5. **Smoke tests — 12 September**
   - Verification page shows Unverified / Verified / Trusted / Elite; blocklisted guest cannot apply.
   - Host on application detail: checkout screening → guest apply link → webhook updates status → report/PDF.
   - Incomplete screening refunds after configured days (or job run).
   - Deal detail stay-protection card; billing `?claim=1` opens damage claim; AwaitingDepositReturn still shows billing card.
   - Listing detail: specimen download vs host-file preview.
   - Inquiry thread: host sees guest name and reputation card.
   - Admin: resolve identity fraud flag; resolve insurance-unknown; listing analytics location + CSV.
   - Hard-refresh after deploy: no infinite stale-chunk reload.
6. **Regression** — Legal pages, Truvi core, custom lease upload, listing-review bulk, SMS opt-in (5 September).

---

## Known limitations

- **Uncommitted scope** — Trust tiers, Checkr screening, and related files remain in the working tree; commit and pass CI before tagging.
- **Screening vs stay protection** — Checkr is **application** screening; Truvi remains **deal** stay protection. Do not conflate in support copy.
- **NoOp provider** — Non-production environments may use `Screening:Provider=NoOp` without Checkr keys.
- **Trust backfill** — Run `db-migrate-trust-tiers.ps1` once per environment; historical application rows use old enum names until backfilled.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-09-12 | Trust tiers v2, Checkr application screening, stay-protection UX, lease specimen, admin queues, inquiry guest card, email layout, stale-chunk recovery. |

---

*End of release notes.*
