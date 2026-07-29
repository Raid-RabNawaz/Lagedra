# Lagedra Release Notes — Saturday 25 July 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Uncommitted working-tree changes on branch `dev` since `RELEASE_NOTES_2026-07-18.md`. This document covers **net-new work only**; items documented on 18 July (founding-host pre-launch surface, listing location/photos/lease editors, booking attention banners, lease PDF download, Hostaway, partner identity cells, notification deep links) are assumed already released or tracked separately and are **not repeated here**.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release improves **email delivery and onboarding**, adds **admin-operational tooling**, introduces **manual identity verification**, and tightens **booking privacy and host triage**. Outbound mail moves from Brevo SMTP to **Twilio SendGrid** with a **standard contact footer** on every message. Platform admins can **send set-password emails** from the user admin screen. Tenants and hosts complete **manual KYC** (document upload + admin review) instead of the prior automated flow on the verification page. **Deal stay access** reveals full property address and counterpart contact only after booking confirmation. The **applications inbox** gains payment-failed triage, and **admin analytics** adds period filters and richer listing metrics.

---

## Highlights

### Email delivery & transactional messaging

- **SendGrid provider** — Replaces Brevo/MailKit SMTP with `SendGridEmailService` (`Twilio:SendGridApiKey`, `Twilio:FromEmail`, `Twilio:FromName`).
- **Universal footer** — All emails append contact details via `EmailFooter`:
  - Contact / Email Us at: **info@lagedra.com**
  - **213-735-2362**
- **Welcome / verify templates** — `WelcomeEmailComposer` centralizes host, tenant, partner, founding-host, and pre-launch partner copy; verify links target the SPA `/auth/verify-email` route.
- **Forgot password** — Sends to inactive or unverified accounts so founding hosts on waitlist can recover access.
- **Reset password** — Successful reset also marks email confirmed and account active (supports first-time password setup).
- **Resend verification** — Uses the same composer as original signup for consistent copy.
- **In-app welcome notifications** — Role- and signup-type-specific titles and bodies after registration.

### Admin — set password, analytics, manual verification

- **Set password email** — Platform admins trigger a one-time set-password link from `/app/admin/users` (**Set password** action per row).
  - API: `POST /v1/auth/users/{userId}/send-set-password-email`
  - User lands on `/auth/reset-password?setup=1`
- **Analytics dashboard** (`/app/admin/analytics`) — Date-range pickers; tiles for listings added, applications, new deals, MRR, and conversion with period context.
- **Listing analytics** (`/app/admin/listing-analytics`) — Filters by landlord, search, status, and date added; columns include landlord, status, created date, and rent.
- **Manual verification queue** (`/app/admin/manual-verification`) — Review dialog loads ID/selfie images with approve/reject actions.
  - API: `GET /v1/admin/identity/manual-queue/{id}` (presigned document URLs)
- **Admin navigation** — Platform admin sidebar no longer inherits member Traveling/Hosting mode toggle.

### Manual identity verification (KYC)

- **User flow** — `/app/verification` uploads ID front/back (optional back) and selfie, then submits for manual review (`ManualKycUpload`).
- **API routes:**
  - `POST /v1/identity/kyc/manual/documents` — multipart upload (max 12 MB)
  - `GET /v1/identity/kyc/manual/documents` — caller’s uploaded documents
  - `POST /v1/identity/kyc/manual/submit` — submit for admin review
- **Admin review** — Queue detail with document preview and decision actions.

### Deal stay access & listing address privacy

- **Stay access card** — `/app/deals/:dealId` shows full property address and host/guest contact when deal is Active, AwaitingDepositReturn, or Closed (`DealStayAccessCard`).
- **API** — `GET /v1/deals/{dealId}/stay-access` (deal parties only).
- **Public listing privacy** — Listing detail strips street and ZIP for anonymous and non-party viewers; precise address remains hidden until booking access applies.

### Applications inbox — payment-failed triage

- **`/app/applications`** — **Needs attention** tab (pending + payment failed) and dedicated **Payment failed** filter.
- **`ApplicationCard`** — Destructive styling and inline banner when payment failed, with link to resolve on the deal.
- **`ApplicationStatsSummary`** — Surfaces failure counts for host inbox triage.

### Trust level & trust ledger

- **Verification tier API** — `GET /v1/me/verification-tier` returns caller tier and optional partner org id.
- **Trust ledger UI** — `/app/trust-ledger` and deal-scoped ledger show expanded entry types (email verified, phone verified, background check, partner endorsement events, early termination, arbitration rulings).
- **Event recording** — Cross-module handlers write ledger entries on verification and partner lifecycle events.

### Deposit return — handshake UX

- **Deal summary** — Exposes `hostConfirmedDepositReturnedAt`, `tenantConfirmedDepositReceivedAt`, and `depositReturnSettledAt` on deal DTOs.
- **`DepositReturnPanel`** — Polls payment status until settled; refreshes deal list on completion.
- **Deal detail copy** — Clearer next-step messaging for each handshake state.

### Web client — auth & errors

- **HTTP client** — Skips Bearer token and refresh on anonymous auth paths (login, register, verify, forgot/reset password, resend).
- **Error mapping** — Prefers API `description`; improved 409 handling (e.g. Hostaway already connected); preserves credential errors on login 401.

### Reviews (backend)

- **`PublishExpiredStayReviewsJob`** — Repairs stay reviews stuck in Submitted when the review window has already published.

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/auth/users/{userId}/send-set-password-email` | Platform admin | Email set-password link to user. |
| `GET` | `/v1/me/verification-tier` | Authenticated | Caller verification tier. |
| `GET` | `/v1/deals/{dealId}/stay-access` | Deal party | Full address + counterpart contact. |
| `POST` | `/v1/identity/kyc/manual/documents` | Authenticated | Upload KYC document. |
| `GET` | `/v1/identity/kyc/manual/documents` | Authenticated | List caller’s KYC uploads. |
| `POST` | `/v1/identity/kyc/manual/submit` | Authenticated | Submit manual KYC for review. |
| `GET` | `/v1/admin/identity/manual-queue/{id}` | Platform admin | Manual KYC detail + document URLs. |
| `GET` | `/v1/admin/analytics/summary` | Platform admin | Expanded summary with date range. |
| `GET` | `/v1/admin/analytics/listings` | Platform admin | Filtered listing analytics rows. |

*Modified:* `GET /v1/listings/{id}` address fields redacted for non-owner/non-party callers; forgot-password and reset-password command behavior as described above.*

---

## Frontend changes

| Route | Component / area | Purpose |
|-------|------------------|---------|
| `/app/admin/users` | Set password action | Admin-triggered password setup email. |
| `/app/admin/analytics` | Period filters, expanded tiles | Platform metrics by date range. |
| `/app/admin/listing-analytics` | Landlord/status/date filters | Operational listing reporting. |
| `/app/admin/manual-verification` | Review dialog | Manual KYC approve/reject. |
| `/app/verification` | `ManualKycUpload` | Document upload + submit for review. |
| `/app/deals/:dealId` | `DealStayAccessCard` | Post-booking address and contact reveal. |
| `/app/applications` | Needs attention / payment failed tabs | Host booking-request triage. |
| `/app/trust-ledger` | Tier badge + entry types | User trust history. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260724213209_AddKycDocuments` | Identity | `identity.kyc_documents` for manual KYC uploads. |

**Script:** `tools/scripts/db-migrate-manual-kyc.ps1`

---

## Configuration & dependencies

| Key / setting | Purpose |
|---------------|---------|
| `Twilio:SendGridApiKey` | SendGrid API authentication (replaces Brevo SMTP). |
| `Twilio:FromEmail` / `Twilio:FromName` | Outbound sender identity. |
| `Twilio:ApiKeySid` / `Twilio:ApiKeySecret` | Optional Twilio API-key auth for SMS (alongside AuthToken). |

**Deployment updates:** `.env.example`, `deploy/env/*.env`, `docker-compose.yml`, ECS task definitions (`Twilio__SendGridApiKey` via SSM), `DEPLOYMENT.md` SendGrid troubleshooting. **Removed:** MailKit / inline Brevo credentials from task defs.

**Verify SendGrid:** `tools/scripts/sendgrid-verify-integration.ps1`

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`.
2. **Configure** SendGrid API key and from-address in target environment; remove legacy Brevo vars.
3. **Migrate** `IdentityDbContext` (`20260724213209_AddKycDocuments`).
4. **Deploy** API + worker, then web.
5. **Smoke tests**
   - Register host → verify email → set password; confirm footer on all emails.
   - Admin: `/app/admin/users` → Set password → user completes `/auth/reset-password?setup=1`.
   - `/app/verification` → upload docs → submit; admin reviews in manual queue.
   - Active deal → stay access card shows address; public listing hides street/ZIP.
   - Payment-failed application appears in host **Needs attention** tab.
   - Admin analytics loads with date range; listing analytics filters work.
   - Forgot password works for unverified founding-host account.

---

## Known limitations

- **Uncommitted scope** — This release reflects the current working tree; commit and pass CI before tagging.
- **Manual KYC migration** — Apply `db-migrate-manual-kyc.ps1` before enabling upload flow in production.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-07-25 | Net-new since 18 July: SendGrid, email footer, admin set-password, manual KYC, stay access, analytics, applications triage. |

---

*End of release notes.*
