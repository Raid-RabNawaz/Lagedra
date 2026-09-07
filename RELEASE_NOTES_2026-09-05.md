# Lagedra Release Notes — Saturday 5 September 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Working-tree changes on branch `dev` since commit `5c75a75` (“22-8-2026”) / `RELEASE_NOTES_2026-08-22.md`. This document covers **everything after 22 August**. Items documented on 22 August (Hosthub, owner tenancy consent, listing ownership / broker / added-via, admin CSV analytics, profile phone/age enforcement) are **not repeated** here; see that note if this deploy also ships that backlog for the first time.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release ships **public legal and marketing pages**, **Truvi Screen & Protect stay protection**, **host-provided lease agreements**, **admin listing review filters and bulk actions**, and **A2P SMS campaign consent**. Guests see **stay protection** (not “insurance premium”) on quote, apply, checkout, and billing; screening starts when Truth Surface is confirmed. Hosts can upload a **custom lease** or keep the Lagedra jurisdiction template, and guests can **preview the lease** before booking. Admins can **filter and bulk approve/deny** the listing review queue. Public **Terms, Privacy, About, FAQ, Contact, and SMS opt-in** pages are live and linked from marketplace and auth footers. Campaign SMS is gated on explicit consent with inbound STOP/START/HELP. CI now **runs unit and integration tests** before deploy. Streamline VRS is documented for a future PMS adapter (no runtime provider in this release).

---

## Highlights

### Public legal & marketing pages

| Route | Page | Notes |
|-------|------|-------|
| `/about` | About | Company identity (Lagedra LLC, Sherman Oaks). |
| `/contact` | Contact | Email, phone, mail; links to SMS, terms, privacy. |
| `/faq` | FAQ | Shared accordion + how-it-works FAQ content. |
| `/how-it-works` | How it works | Shared `FaqAccordion`; `#faq` deep-link scroll. |
| `/tc` | Terms | Full T&C (`/terms` redirects here). |
| `/privacy` | Privacy | Full policy; `#cookies` anchor. |
| `/sms` | SMS program | A2P opt-in / opt-out web form. |
| `/pricing` | — | Redirects to `/faq`. |

**Site-wide links:** Marketplace footer, auth layout footer, register/signup Terms & Privacy links, verification Privacy link, admin SEO slugs `tc` and `sms`.

**Legal content updates:** Terms cover Truvi Screen & Protect (guest fee, not renter’s insurance), Lagedra vs host-provided lease, and SMS program. Privacy covers SMS consent storage and cookies.

### Stay protection (Truvi Screen & Protect)

| Item | Detail |
|------|--------|
| **Fee model** | Nightly wholesale recovery: **$6/night** for the first 30 nights, **$4/night** after (configurable). Calculator is platform-side; no Truvi quote API. |
| **Screening** | On Truth Surface confirmation, `OnTruthSurfaceConfirmedRequestTruviVerificationHandler` requests Truvi verification. |
| **Cancellation** | Truvi reservation cancelled; refund retains **$1 screening remainder**. |
| **Copy** | “Insurance premium” renamed **Stay protection** on quote, apply, checkout, billing, cancel dialog, and truth surface. |
| **Guest disclosure** | `StayProtectionGuestAgreementNote` links to Truvi guest agreement. |
| **Application detail** | Screening status, verification ID, flagged reason; **Rescreen guest** when flagged. |

**APIs:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/deals/{dealId}/insurance` | Deal party | Status including `verificationId`, `screeningStatus`, `flaggedReason`. |
| `POST` | `/v1/deals/{dealId}/insurance/verify` | Tenant | Start verification (Truvi-backed). |
| `POST` | `/v1/deals/{dealId}/insurance/manual-proof` | Deal party | Manual proof upload. |
| `PUT` | `/v1/deals/{dealId}/insurance/reservation` | Host / admin | Modify Truvi reservation (dates / pets). |
| `POST` | `/v1/deals/{dealId}/insurance/rescreen` | Host / owner / admin | Rescreen after guest contact update. |

**Config:** `Insurance:FeeCalculationMode` = `Truvi`; `Insurance:Truvi` (`FirstNights`, `FirstNightsFeeCents`, `AdditionalNightFeeCents`, `ScreeningFeeCents`, `BaseUrl`, `ScreeningEnabled`, `SubscriptionKey`). Staging ECS task defs wire `Insurance__Truvi__*`.

**Migration:** `20260903214521_AddTruviScreeningToPolicyRecords` — `ExternalVerificationId`, `ScreeningStatus`, `FlaggedReason` on `insurance.policy_records`.  
**Script:** `tools/scripts/db-migrate-truvi-screening.ps1`

### Host custom lease agreements

| Item | Detail |
|------|--------|
| **Source** | Listing `leaseAgreementSource`: `LagedraTemplate` (default) or `HostProvided`. |
| **Upload** | PDF/DOCX ≤10 MB, malware scan; stored in `MinIO:LeaseDocumentsBucket`. |
| **Deal PDF** | Host file copied immutably onto the deal; Lagedra template path unchanged when no custom lease. |
| **Guest preview** | Signed-in guests can download listing lease preview before booking. |

**Surfaces:** New **Lease agreement** wizard step (`ListingLeaseAgreementEditor`); edit form card; listing detail `ListingLeasePreviewCard`.

**APIs:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/listings/{id}/lease-document` | Member | Upload custom lease (multipart). |
| `DELETE` | `/v1/listings/{id}/lease-document` | Member | Remove custom lease; revert to Lagedra template. |
| `GET` | `/v1/lease-agreements/listings/{listingId}/preview` | Signed-in | Download preview PDF/DOCX. |

**Migrations:**

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260903192418_AddListingCustomLeaseAgreement` | Listings | `LeaseAgreementSource` + custom lease storage metadata. |
| `20260903192434_AddDealLeaseDocumentSource` | Lease agreements | `Source` on `deal_lease_documents`; nullable template ids for host docs. |

**Script:** `tools/scripts/db-migrate-custom-lease-agreement.ps1`

### Admin listing review — filters & bulk actions

| Item | Detail |
|------|--------|
| **Filters** | Location, host name, property type, title, lease type (custom/standard), instant booking, host ID verified, incomplete host profile. |
| **Bulk actions** | Approve or deny up to 50 listings per batch; partial failures reported. |
| **Queue DTO** | City/state/country, instant booking, custom lease flag, host profile completeness, government ID verified. |
| **Performance** | Pending-review query no longer loads the full photo graph (avoids Hostaway-scale timeouts). |

**APIs:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/admin/listings/approve-bulk` | Platform admin | Bulk approve `{ listingIds }`. |
| `POST` | `/v1/admin/listings/deny-bulk` | Platform admin | Bulk deny `{ listingIds, reason }`. |

**Route:** `/app/admin/listing-review`

### A2P SMS campaign consent

| Item | Detail |
|------|--------|
| **Public opt-in** | `/sms` — phone + checkbox (never pre-selected). |
| **Preferences** | `/app/notification-preferences` — optional SMS campaigns toggle with disclosures. |
| **Inbound** | Twilio STOP / START / HELP persist consent and auto-reply (`smsProgram.ts` copy aligned with backend). |
| **Gating** | Campaign SMS requires verified phone + explicit `sms_consents` opt-in. |

**APIs:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/sms/consent` | Anonymous (optional user) | Record opt-in/out. |
| `POST` | `/v1/webhooks/twilio/sms-inbound` | Twilio signature | Inbound STOP/START/HELP. |
| `PUT` | `/v1/notifications/preferences/{userId}` | Self | Extended with `smsCampaignsOptedIn`. |

`ConsentMiddleware` allowlists `/v1/sms`.

**Migration:** Generate and apply `AddSmsCampaignConsent` on `NotificationDbContext` via `tools/scripts/db-migrate-sms-campaign-consent.ps1` if the migration file is not yet in the tree.

### Marketplace, applications & join

| Surface | Change |
|---------|--------|
| `/listings` | Promo banner and promo cards removed. |
| `/listings/:id` | Lease preview section; stay protection label + guest agreement note. |
| Apply dialog | Stay protection copy + guest agreement note. |
| `/app/listings` | Denied listings show admin rejection reason. |
| `/app/applications/:id` | Truvi screening panel + rescreen. |
| Signup / How it works | Terms & Privacy links; shared FAQ accordion. |

### Cancellation, checkout & billing

- Refund calculator caps stay-protection refund and retains **$1** screening fee.
- Checkout and billing hide the stay protection line when the fee is zero.
- Existing cancel/refund endpoints; no new payment routes.

### Streamline VRS (documentation only)

`docs/integrations/streamline/README.md` plus Partner X reference (listings XML feeds, Partner OLB JSON API, dual auth, IP allow-listing, 90-day token rotation). **No runtime Streamline channel provider** in this release.

### CI, deploy & infrastructure

| Item | Change |
|------|--------|
| `.github/workflows/deploy.yml` | Unit + integration tests (Testcontainers) gate backend and frontend deploy. |
| `Dockerfile` / `Lagedra.sln` | Integration test project included in restore/build. |
| ECS staging | `Insurance__Truvi__*` environment variables. |
| CloudFront | Production www redirect (`www-redirect-function.js`, DNS/CF staging artifacts). |
| API gateway | `MapSmsConsentEndpoints()` registered. |
| Lease seed | California template seed failures logged at Error. |

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/deals/{dealId}/insurance` | Deal party | Stay protection / screening status. |
| `POST` | `/v1/deals/{dealId}/insurance/rescreen` | Host / owner / admin | Rescreen flagged guest. |
| `PUT` | `/v1/deals/{dealId}/insurance/reservation` | Host / admin | Modify Truvi reservation. |
| `POST` | `/v1/listings/{id}/lease-document` | Member | Upload custom lease. |
| `DELETE` | `/v1/listings/{id}/lease-document` | Member | Remove custom lease. |
| `GET` | `/v1/lease-agreements/listings/{listingId}/preview` | Signed-in | Listing lease preview. |
| `POST` | `/v1/admin/listings/approve-bulk` | Platform admin | Bulk approve. |
| `POST` | `/v1/admin/listings/deny-bulk` | Platform admin | Bulk deny. |
| `POST` | `/v1/sms/consent` | Anonymous | SMS campaign consent. |

*Extended:* listing create/update `leaseAgreementSource`; notification preferences `smsCampaignsOptedIn`; cancel booking refund retains screening fee; insurance verify/manual-proof remain and are Truvi-backed.*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| `/about`, `/contact`, `/faq`, `/tc`, `/privacy`, `/sms` | New public legal pages + shared chrome. |
| Marketplace / auth footers | Live legal links. |
| Listing wizard / edit | Custom lease editor. |
| Listing detail | Lease preview card; stay protection note. |
| Checkout / billing / apply | Stay protection labeling. |
| `/app/admin/listing-review` | Filters, selection, bulk approve/deny. |
| `/app/applications/:id` | Screening status + rescreen. |
| `/sms`, notification preferences | Campaign SMS opt-in. |
| `/app/listings` | Denial reason on denied cards. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260903192418_AddListingCustomLeaseAgreement` | Listings | Custom lease source and storage metadata. |
| `20260903192434_AddDealLeaseDocumentSource` | Lease agreements | Deal document source (template vs host file). |
| `20260903214521_AddTruviScreeningToPolicyRecords` | Insurance | Truvi verification / screening columns. |
| `AddSmsCampaignConsent` *(generate if missing)* | Notifications | `sms_consents` table. |

**Apply order:**

```powershell
pwsh tools/scripts/db-migrate-custom-lease-agreement.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-truvi-screening.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-sms-campaign-consent.ps1
```

If 22 August was not deployed, apply listing management, owner tenancy consent, and listing added-via first.

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `Insurance__Truvi__*` | Truvi Screen & Protect API and fee tiers. |
| `Insurance__FeeCalculationMode` | `Truvi` to enable stay-protection calculator. |
| `MinIO__LeaseDocumentsBucket` | Private bucket for host lease files. |
| Twilio inbound webhook | SMS STOP/START/HELP + signature validation. |
| Truvi guest agreement URL | Linked from checkout / apply disclosures. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; confirm unit + integration tests pass (CI gate).
2. **Migrate** custom lease → Truvi screening → SMS consent.
3. **Configure** Truvi keys, lease documents bucket, Twilio inbound webhook, CloudFront www redirect if not already live.
4. **Deploy** API + worker, then web.
5. **Smoke tests — 5 September**
   - Open `/about`, `/faq`, `/tc`, `/privacy`, `/sms`, `/contact`; footer links from marketplace and auth.
   - Create listing with custom lease upload; guest (signed in) downloads preview on listing detail.
   - Confirm Truth Surface → insurance status shows Truvi screening; flagged guest → rescreen from application detail.
   - Checkout shows **Stay protection** + guest agreement; cancel booking → refund retains $1 screening.
   - `/sms` opt-in; STOP/START via Twilio; preferences toggle at `/app/notification-preferences`.
   - Admin listing review: filter by custom lease / location; bulk approve and deny.
   - Denied listing on My Listings shows rejection reason.
6. **Regression** — Hosthub connect, owner consent, listing ownership/broker/added-via, admin CSV (22 August).

---

## Known limitations

- **Uncommitted scope** — Legal pages, Truvi, custom leases, bulk review, and SMS consent remain in the working tree; commit and pass CI before tagging.
- **SMS consent migration** — Run `db-migrate-sms-campaign-consent.ps1` if the EF migration is not yet generated.
- **Streamline** — Documentation only; no live channel adapter.
- **Stay protection** — Fee is platform-calculated; Truvi quote API is not used.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-09-05 | Legal pages, Truvi stay protection, custom leases, admin bulk review, SMS campaign consent, CI test gate. |

---

*End of release notes.*
