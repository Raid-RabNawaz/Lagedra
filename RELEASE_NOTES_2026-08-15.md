# Lagedra Release Notes — Saturday 15 August 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Uncommitted working-tree changes on branch `dev` since `947b0d7` (“commit 08-08-2026”) / `RELEASE_NOTES_2026-08-08.md`. This document covers **net-new work only**. Marketplace gallery, SignIn dialog, guest save, Excel-only import as shipped 8 August, Stripe onboard-loop fix, and platform-fee migration are **not repeated**; see that note if needed.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release advances **host listing import**, **active-stay rent confirmation**, and **partner / payout / ops polish**. Hosts can bulk-import listings from **Excel or XML** (including Zillow/HotPads-style feeds) with **server-side photo URL import**. During active bookings, hosts **check in monthly rent** (received / missed) on the billing page, with missed rent feeding compliance. Partners add members **by email** and can **remove** members. Guesty is available during pre-launch. Profile is **role-aware**, date fields use a shared **DatePicker**, and KYC uploads fix multipart Content-Type failures. Two migrations ship: widen listing descriptions and create `rent_check_ins`.

---

## Highlights

### Unified listing import (Excel + XML) & photo URLs

| Item | Detail |
|------|--------|
| **UI** | `ImportFromExcelDialog` replaced by `ImportListingsDialog` (`excel` \| `xml`) on `/app/listings/new`. |
| **Shared rules** | `listingImportShared.ts` — max 100 rows, max 20 photos; shared catalog/validation. |
| **XML** | Lagedra template and Zillow/HotPads-style `<property>` feeds; can lock address and fetch photos. |
| **URL import** | Cap default selection / apply to 20 photos; client import concurrency 3. |
| **Server photos** | `POST /v1/listings/{id}/media/import-from-urls` — API fetches and attaches remote images (180s client timeout). |
| **Description** | Listing description column widened to `text` for long channel sync content. |

### Monthly rent check-ins

| Item | Detail |
|------|--------|
| **Host** | On `/app/deals/:dealId/billing`, confirm months 2+ rent as received or missed (optional note). |
| **Tenant** | Sees check-in status on the same billing surface. |
| **Jobs** | `RentCheckInJob` opens periods; stay completion stops billing via `OnStayCompletedStopBillingHandler`. |
| **Compliance** | Missed rent → `RentMissedEvent` → `OnRentMissedRecordSignalHandler`. |
| **Payment instructions** | Host payment details remain visible after payment is Confirmed. |

**APIs:**

- `GET /v1/deals/{dealId}/rent-checkins`
- `POST /v1/deals/{dealId}/rent-checkins/{checkInId}/respond` — `{ received, note? }`

**Migration:** `20260812171619_AddRentCheckIns` → `activation_billing.rent_check_ins`  
**Script:** `tools/scripts/db-migrate-rent-checkins.ps1`

### Partners — email invite & remove

- **Add member by email** (preferred over raw user id) on partner members UI.
- **Remove member** — `DELETE /v1/partners/{id}/members/{memberId}`.
- Referrals and reservations date fields use shared **DatePicker**.

### Channels — Guesty in pre-launch

- Pre-launch **Guesty connect** is live (no longer “Coming soon”); copy updated on `/app/channels`.

### Host Stripe onboarding (beyond Aug 8)

| Item | Detail |
|------|--------|
| **Pending verification** | Surfaces when details are submitted and Stripe has no actionable requirements. |
| **Account update** | Prefer account-update link when details already submitted. |
| **Status DTO** | Adds `pendingVerification`, `detailsSubmitted`, `disabledReason`. |
| **Requirement labels** | Clearer copy for phone, identity documents, etc. |

### Profile, dashboard & shell

- **Profile** — Role-aware sections: marketplace About / lease / broker / hosting for Members; partners/admins get identity basics + workspace hint. DOB DatePicker with max age gate (18+).
- **Dashboard** — In-page mode toggle removed; Hosting/Travelling switch lives in header only.
- **AppShell** — Mobile drawer always shows expanded labels; pin control desktop-only.
- **Header** — Hosting/Travelling label always visible with improved `aria-*`.

### DatePicker platform rollout

Shared `date-picker` UI replaces native `type="date"` on Search stay filters, admin analytics/audit/jurisdiction/lease/listing analytics, partner referrals/reservations, Manual KYC DOB, and profile DOB.

### Verification, notifications, auth & gateway

| Area | Change |
|------|--------|
| **Manual KYC** | Multipart Content-Type fix (was causing 500s); upload timeout 120s; DatePicker + 18+. |
| **Notifications** | Hub updates recent + all caches; normalizes `isRead`; all-notifications poll every 60s. |
| **Register** | Best-effort E.164 phone normalize at signup (does not reject bad format). |
| **API gateway** | Maps Twilio SMS delivery webhooks; Quartz scheduling owned by worker only. |
| **Deploy** | Task defs include `MinIO__KycBucket=lagedra-private-prod`; image tags toward `live-202608120353`. |

### Worker & supporting backend (ops)

- Composite Quartz sweep jobs; PG notify realtime push path for in-app notifications.
- Twilio SMS delivery status webhooks (`RecordSmsDeliveryStatusCommand`).
- California lease template / conditional helpers; insurance lifecycle job consolidation; privacy maintenance job.

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/listings/{id}/media/import-from-urls` | Member | Server-side photo import from URLs. |
| `GET` | `/v1/deals/{dealId}/rent-checkins` | Deal party | List monthly rent check-ins. |
| `POST` | `/v1/deals/{dealId}/rent-checkins/{checkInId}/respond` | Landlord | Mark rent received / missed. |
| `DELETE` | `/v1/partners/{id}/members/{memberId}` | Partner staff | Remove organization member. |
| `POST` | Twilio webhook paths | Twilio signed | SMS delivery status callbacks. |

*Partner add-member prefers `email` in request body. Host Stripe status DTO expands pending-verification fields.*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| `/app/listings/new` | Dual Excel / XML import via `ImportListingsDialog`. |
| `/app/deals/:dealId/billing` | Monthly Rent Check-ins card. |
| `/app/partners/.../members` | Email invite + remove member. |
| `/app/channels` | Guesty available in pre-launch. |
| `/app/payout-setup` | Pending verification / account-update UX. |
| `/app/profile` | Role-aware sections and DOB DatePicker. |
| `/app` | Mode toggle only in header. |
| Search + admin + partners | Shared DatePicker for date fields. |
| `/app/verification` | KYC multipart fix + DatePicker. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260811171614_WidenListingDescription` | Listings | `Description` → `text`. |
| `20260812171619_AddRentCheckIns` | Billing | Table `activation_billing.rent_check_ins`. |

**Scripts:**

```powershell
pwsh tools/scripts/db-migrate-widen-listing-description.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-rent-checkins.ps1 -SkipAdd
```

Confirm Aug 8 platform-fee and earlier Channel OAuth / KYC migrations are already applied in the target environment.

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `jsdom` (web devDependency) | XML import unit tests. |
| `MinIO__KycBucket` | KYC document storage bucket in ECS task defs. |
| Twilio webhook signing | SMS delivery status validation (`TwilioRequestValidator`). |
| Realtime push mode | PG notify relay for in-app notification delivery (worker/API). |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web` (install lockfile / `jsdom`).
2. **Migrate** widen listing description, then rent check-ins.
3. **Configure** KYC bucket, Twilio webhooks, and realtime push settings per environment.
4. **Deploy** API + worker, then web.
5. **Smoke tests — 15 August**
   - Excel and XML import create drafts; XML with photo URLs attaches media via import-from-urls.
   - Active deal billing: rent check-in periods appear; host marks received/missed; tenant sees status.
   - Partner: invite member by email; remove member.
   - Pre-launch: Guesty connect card works.
   - Payout setup: pending verification messaging when applicable.
   - Profile: Member vs partner/admin sections; DOB capped at 18+.
   - Manual KYC upload succeeds (no multipart 500).
   - DatePicker on search stay dates and admin analytics filters.
6. **Regression** — Marketplace gallery/save, SignIn dialog, Stripe onboard no-loop, OwnerRez OAuth (prior releases).

---

## Known limitations

- **Uncommitted scope** — Rent check-ins, XML import, partner remove, and related files are still in the working tree; commit and pass CI before tagging.
- **Rent check-ins** — Host-attested; automatic bank reconciliation is not included.
- **XML import** — Feed shapes beyond Lagedra template and common Zillow/HotPads property XML may need mapping follow-ups.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-08-15 | XML/Excel unified import, photo URL import, rent check-ins, partner email/remove, Guesty pre-launch, role profile, DatePicker, KYC fix, two migrations. |

---

*End of release notes.*
