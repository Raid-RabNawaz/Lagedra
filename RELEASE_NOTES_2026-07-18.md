# Lagedra Release Notes — Saturday 18 July 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Deployment from branch `dev` at commit `1b066d7` ("Changes in inquiry") plus the pending working tree included in this release. Supersedes `RELEASE_NOTES_2026-07-11.md` for operational deployment purposes.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

Today's deployment ships the **July platform tranche** — pre-launch founding-partner program, inquiry offer negotiation, partner-org booking, lease agreements, stay reviews, SMS notifications, and deposit-return evidence — together with **July 18 refinements** that make the product usable for founding hosts before full launch.

Key additions for **18 July**:

- **Founding-host pre-launch surface** — Hosts who sign up during pre-launch can verify email, set a password, and manage **listings** and **channels** while the public marketplace and full app remain gated.
- **Listing editor overhaul** — Location map with pin/address reconciliation, dedicated photos editor, and **lease terms UI** wired to the backend.
- **Booking attention UX** — Host and guest dashboards surface payment failures, deposit-return actions, ending-soon stays, and denied listings.
- **Lease PDF download** — On-demand generation, Truth Surface download button, email attachment on deal activation, and stricter download authorization.
- **Hostaway PMS** — Second channel provider alongside OwnerRez; webhook ingestion and connect flow on `/app/channels`.
- **Partner identity display** — Members and endorsements show names and emails instead of raw user IDs.
- **Notification deep links** — In-app and email notifications route to the correct deal, inquiry, listing, and verification pages.

---

## Highlights — 18 July delta

### Pre-launch & founding-host access

| Change | Detail |
|--------|--------|
| **Host signup path** | Host registration creates a real account (not waitlist-only); email verify → first-time password setup → sign in. |
| **Partner signup** | Remains waitlist-only; cannot sign in until launch. |
| **Limited host app** | During pre-launch, hosts may access `/app/listings`, `/app/listings/*`, and `/app/channels` only; other `/app` routes redirect to listings home. |
| **Staff exemption** | `PlatformAdmin` and `Arbitrator` retain full product access. |
| **Login / join copy** | Updated messaging on `/join`, `/auth/login`, `/auth/verify-email`, and `/auth/reset-password?setup=1`. |
| **Verify-email API** | `POST /v1/auth/verify-email` may return `{ requiresPasswordSetup, passwordSetupToken }` for founding hosts. |

**New / updated guards:** `RequirePreLaunchHostSurface`, `preLaunchAccess.ts`, refined `RequireLaunchAccess`.

### Listings — location, photos, lease terms

| Surface | Purpose |
|---------|---------|
| **`ListingLocationEditor`** | Map pin + address cards; geocode sync; mismatch warning (>5 km) and block (>100 km) with override. |
| **`ListingPhotosEditor`** | Upload, URL add, cover selection, reorder, virtual-tour video. |
| **`ListingForm` lease section** | Full lease terms (rent due day, late/NSF fees, utilities, parking, keys, lead paint, etc.). |
| **`ListingWizard`** | New Location and Photos steps; draft created after Basics with incremental save. |
| **`/app/listings/new`** | Wizard-driven create through location and photos. |
| **`/app/listings/:id/edit`** | Sectioned edit using shared editors. |

**API / domain**

- `PUT /v1/listings/{id}` accepts `leaseTerms` on update.
- `POST /v1/listings/{id}/lock-address` — published listings can lock precise address; pin sync on lock.
- Published listings allow address lock / pin sync without opening full edit on non-draft statuses.

### Bookings — attention & triage

| Surface | Purpose |
|---------|---------|
| **`BookingAttentionBanner`** | Critical (payment failed, deposit return) and amber (ending soon ≤15 days) banners with CTAs. |
| **`/app/deals/mine`** | New **Needs attention** tab sorted by urgency. |
| **Hosting / traveling dashboards** | Top-of-page issue banners; ending-soon badges on booking rows. |
| **`DealCard`** | Inline problem copy for issue phases. |

### Lease PDF & Truth Surface

- **`LeaseAgreementDownloadButton`** on Truth Surface confirmation and deal detail.
- **`DealLeasePdfService`** — on-demand PDF generation when document not yet materialized.
- **Download authorization** — Landlord, tenant, or platform admin only.
- **Deal-activated email** — Lease PDF attached when notification sends.
- **Dual-control fix** — Template approval uses authenticated caller; no client-supplied approver id.

### PMS — Hostaway

- **`HostawayChannelProvider`** — OAuth2 per-host credentials; content sync and booking publish.
- **Webhook** — `POST /v1/webhooks/hostaway` with `ProcessHostawayWebhookCommand`.
- **UI** — `ConnectHostawayCard` on `/app/channels`; during pre-launch, Hostaway is primary with OwnerRez shown as coming soon.

### Partner portal polish

- **`PersonCell`** on `/app/partner/members` and `/app/partner/endorsements`.
- API DTOs include `displayName`, `email`, and related identity fields via `PartnerUserIdentityResolver`.

### Notifications

- **`getNotificationRoute`** — Correct deep links for truth surface (deal-scoped), listing review (landlord path), inquiry sessions, deposit/payment billing, and verification.

---

## Highlights — platform tranche (commit `1b066d7`)

*First production deploy of scope documented in `RELEASE_NOTES_2026-07-11.md`.*

| Area | Summary |
|------|---------|
| **Pre-launch program** | `/join` founding-partner signup, `GET /v1/platform/public-config`, marketplace gating. |
| **Phone & SMS** | Twilio OTP; SMS notification channel. |
| **Inquiry offers** | Propose / accept / counter / withdraw rent-deposit offers; partner participants; `/app/partner/inquiries`. |
| **Partner booking** | Org-pays vs tenant-pays; SetupIntent; `CompletePartnerRequestPanel`. |
| **Lease agreements** | Template module, listing lease columns, admin editor at `/app/admin/lease-agreements`, deal PDF endpoint. |
| **Reviews & reputation** | Bilateral stay reviews, partner service reviews, reputation on public profile. |
| **Deposit return evidence** | Sealed manifest required for partial withholds; handshake migration landed. |
| **OwnerRez PMS** | Channel integration module (first provider). |

---

## Backend changes (18 July delta)

| Method | Path | Change |
|--------|------|--------|
| `POST` | `/v1/auth/verify-email` | May return password-setup token for founding hosts. |
| `PUT` | `/v1/listings/{id}` | `leaseTerms` object on update body. |
| `POST` | `/v1/listings/{id}/lock-address` | Lock/sync address on published listings. |
| `GET` | `/v1/lease-agreements/deals/{dealId}/pdf` | On-demand generation; stricter party auth. |
| `POST` | `/v1/webhooks/hostaway` | Hostaway unified webhook ingestion. |

*Partner member/endorsement list endpoints return enriched identity fields (same routes, expanded DTOs).*

---

## Frontend changes (18 July delta)

| Route | Component | Purpose |
|-------|-----------|---------|
| `/app/listings/new` | `ListingWizard` + editors | Full create flow with location, photos, lease terms. |
| `/app/listings/:id/edit` | Sectioned editors | Location, photos, lease terms parity. |
| `/app/deals/mine` | Needs attention tab | Urgent booking triage. |
| `/app/deals/:dealId` | `BookingAttentionBanner`, `LeaseAgreementDownloadButton` | Alerts + lease download. |
| Truth Surface pages | `LeaseAgreementDownloadButton` | Post-confirm lease PDF. |
| `/app/channels` | `ConnectHostawayCard` | Hostaway connect + pre-launch copy. |
| `/app/partner/*` | `PersonCell` | Human-readable member/endorsement rows. |
| Notifications | `getNotificationRoute` | Fixed navigation from notification clicks. |
| `/join`, `/auth/*` | Updated flows | Founding-host verify → password setup. |

---

## Database & migrations

Apply per environment using scripts under `tools/scripts/`. Full platform tranche migrations are listed in `RELEASE_NOTES_2026-07-11.md`.

**18 July:** No new migrations in the working-tree delta; schema changes for lease terms and address lock use migrations already in `1b066d7`.

**Before deploy, confirm applied:**

- Pre-launch signup, phone verification, SMS channel
- Inquiry offers and partner participant
- Partner booking payer fields
- Lease agreements module and listing lease terms
- Deposit-return handshake and evidence manifest
- Reviews module (generate via `db-migrate-reviews.ps1` if not yet applied)
- Channel integration (OwnerRez + Hostaway uses same `channel_integration` schema)

---

## Configuration & dependencies

| Key | Purpose |
|-----|---------|
| `prelaunch.enabled` | Gates marketplace and limits host app surface. |
| `Twilio:*` | SMS OTP and notification delivery. |
| `Hostaway` section | Per-deployment Hostaway API settings. |
| `deposit_return.window_days` | Deposit-return disclosure window (default 21). |
| `review.window_days` | Stay review publish window (default 14). |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests.
2. **Migrate** all pending contexts (see July 11 notes + reviews script).
3. **Configure** pre-launch flag, Twilio, Stripe, Hostaway, OwnerRez per environment.
4. **Deploy** API + worker, then web.
5. **Smoke tests — 18 July delta**
   - Founding host: `/join` (host) → verify email → set password → land on `/app/listings`; blocked from `/app/deals`.
   - Partner waitlist signup cannot log in.
   - Listing wizard: location pin/address mismatch handling → photos → lease terms → submit for review.
   - Published listing: lock address without full edit unlock.
   - Deal: Truth Surface confirm → download lease PDF; notification click opens correct deal page.
   - Host dashboard: payment-failed / deposit-return / ending-soon banners visible.
   - Partner members: names displayed, not GUIDs.
   - `/app/channels`: Hostaway connect flow succeeds; webhook receives test event.
6. **Smoke tests — platform tranche regression**
   - Inquiry offer propose → accept; partner on thread.
   - Org-pays partner reservation + tenant completion panel.
   - Stay review submit; reputation on public profile.
   - Deposit return with evidence manifest on partial withhold.

---

## Known limitations

- **OwnerRez during pre-launch** — UI shows coming soon; Hostaway is the supported connect path until launch flag clears.
- **Founding hosts** — Cannot access deals, applications inbox, or billing until full launch; listings + channels only.
- **Reviews migrations** — Confirm `db-migrate-reviews.ps1` has been run in target environment.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-07-18 | Today's deployment: `1b066d7` + founding-host surface, listing editors, booking attention, Hostaway, notification fixes. |

---

*End of release notes.*
