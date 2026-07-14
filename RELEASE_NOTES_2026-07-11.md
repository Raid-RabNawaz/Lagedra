# Lagedra Release Notes — Friday 11 July 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Changes on branch `dev` since `RELEASE_NOTES_2026-07-04.md` (commit `5c95728` and the pending working tree staged for this deployment). Supersedes the July 4 narrative for net-new capabilities only; prior booking, PMS, host billing, and arbitration filing-fee items remain in force.

**Program references:** Pre-launch founding-partner program, lease agreements, inquiry negotiation, partner-org booking, reviews & reputation, SMS notifications, deposit-return evidence.

---

## Executive summary

This release adds a **pre-launch founding-partner program** with marketplace gating, **SMS phone verification**, and a dedicated **`/join` signup** flow. **Structured inquiry** now supports **rent/deposit offer negotiation** and **partner participants** on threads. **Institution partners** can **pay on behalf of endorsed members**, with tenant completion flows for pending requests. A new **lease agreements** module delivers **jurisdiction templates**, **listing lease terms**, and **deal PDF generation**. **Stay reviews and reputation** scores publish after bilateral submission or window expiry. **Deposit return** now requires **sealed evidence** for partial withholds. **Twilio SMS** extends the notification channel alongside email and in-app delivery.

---

## Highlights

### Pre-launch program & join flow

- **Founding-partner waitlist** — When `prelaunch.enabled` is on, registration captures host/partner intent (portfolio size, housing type, placements/year, company name) and flags `IsPreLaunchSignup` without the standard email-verification loop.
- **Launch gating** — `RequireLaunchAccess` restricts public marketplace and authenticated app routes; exempt roles: `PlatformAdmin`, `Arbitrator`.
- **Login restriction** — Non-exempt users receive `PreLaunchRestricted` during pre-launch.
- **Join UX** — `/join` replaces `/auth/register` redirect; `/how-it-works` marketing page; `GET /v1/platform/public-config` exposes `{ preLaunchEnabled }` for anonymous clients.

### Phone verification & SMS

- **SMS OTP** — `POST /v1/auth/phone/send-code` and `confirm` with rate limits; verified phone required for SMS notifications.
- **Twilio integration** — `TwilioSmsService` in infrastructure; `NotificationChannel.Sms` in notification pipeline; templates seeded for booking, payments, deposit return, reviews, and arbitration events.
- **UI** — Phone verification on `/app/verification` and profile completeness scoring.

### Inquiry offers & partner participation

- **Offer negotiation** — Propose, accept, counter, and withdraw accepted rent/deposit offers on inquiry sessions; accepted offer feeds apply pricing.
- **Partner participants** — Tenant invites endorsed partner org; partner staff view and participate; partners can start inquiries on behalf of members.
- **API routes:**
  - `POST /v1/inquiry-sessions/{sessionId}/offers`
  - `POST .../offers/{offerId}/accept|counter`
  - `POST .../offers/accepted/withdraw`
  - `POST|DELETE /v1/inquiry-sessions/{sessionId}/partner`
  - `GET /v1/inquiry-sessions/partner`
  - `POST /v1/listings/{listingId}/inquiry/partner`
- **UI** — `InquiryOfferPanel`, `InquiryParticipantsPanel`; `/app/partner/inquiries` partner inbox.

### Partner organization booking & payer model

- **Payer types** — `Tenant` (default) or `PartnerOrganization` on `deal_applications`.
- **Org card on file** — `POST /v1/partners/{id}/setup-intent` creates Stripe SetupIntent on org customer; org pays path on `/app/partner/reservations`.
- **Tenant completion** — `CompletePartnerRequestPanel` on `/app/applications/:id` for pending partner-direct requests (`POST /v1/applications/{id}/attach-payment`).
- **Endorsed members** — Partner staff create reservations choosing payer; tenant completes consent and payment when required.

### Lease agreements

- **New module** — `lease_agreements` schema: jurisdiction-coded templates, version lifecycle (Draft → PendingApproval → Published → Deprecated), dual-control approval, placeholder catalog.
- **Deal PDF** — Generated from Truth Surface, listing lease terms, and lease-party profiles; `GET /v1/lease-agreements/deals/{dealId}/pdf`.
- **Listing lease terms** — 20+ owned columns on listings (rent due day, late/NSF fees, utilities, parking, lead paint, keys, insurance minimum, etc.) via `LeaseTerms` value object.
- **Lease party profiles** — Mailing/notice addresses and broker fields on users for PDF placeholders.
- **Admin UI** — `/app/admin/lease-agreements` and `/app/lease-agreements` (arbitrators) with `LeaseRichTextEditor` and placeholder insertion.

### Reviews & reputation

- **Stay reviews** — Bilateral guest↔host reviews with category ratings (cleanliness, accuracy, communication, location, check-in, value, house rules); private until both submit or window expires.
- **Partner service reviews** — Responsiveness, reliability, support quality on partner orgs.
- **Reputation aggregates** — Users, listings (anonymous), and partner organizations.
- **API** — `/v1/deals/{dealId}/reviews`, `/v1/users/{userId}/reviews|reputation`, `/v1/listings/{listingId}/reviews`, `/v1/partners/organizations/{orgId}/reviews|reputation`.
- **UI** — `LeaveStayReviewPanel` on deal detail; `ReputationPreview` on public profile; `PartnerServiceReviewPanel` on partner surfaces.
- **Job** — `PublishExpiredStayReviewsJob` (daily) publishes when review window closes.

### Deposit return evidence (enhancement)

- **Partial withholds** — Host confirm requires sealed evidence manifest (damage photos) via integrated `EvidenceUpload` on `DepositReturnPanel`.
- **Window disclosure** — Platform setting `deposit_return.window_days` (default 21) surfaced in UI per CA §1950.5 guidance.
- **Handshake migration** — `20260703215021_SyncBillingModel` lands full move-out / deposit-return columns on payment confirmations.

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/platform/public-config` | Anonymous | Pre-launch flag for client gating. |
| `POST` | `/v1/auth/phone/send-code` | Authenticated | Send SMS OTP. |
| `POST` | `/v1/auth/phone/confirm` | Authenticated | Confirm phone verification. |
| `POST` | `/v1/applications/{id}/attach-payment` | Tenant | Complete partner request (payment + consent). |
| `POST` | `/v1/partners/{id}/setup-intent` | Partner staff | Org booking SetupIntent. |
| `POST` | `/v1/inquiry-sessions/{sessionId}/offers` | Thread party | Propose rent/deposit offer. |
| `POST` | `/v1/inquiry-sessions/{sessionId}/offers/{offerId}/accept` | Counterparty | Accept offer. |
| `POST` | `/v1/inquiry-sessions/{sessionId}/offers/{offerId}/counter` | Counterparty | Counter offer. |
| `GET` | `/v1/inquiry-sessions/partner` | Partner staff | Partner inquiry inbox. |
| `POST` | `/v1/listings/{listingId}/inquiry/partner` | Partner staff | Start inquiry for endorsed member. |
| `GET\|POST` | `/v1/deals/{dealId}/reviews` | Deal party | Stay review submit/read. |
| `GET` | `/v1/users/{userId}/reputation` | Authenticated | User reputation summary. |
| `GET\|POST` | `/v1/lease-agreements/*` | Role-scoped | Template CRUD and lifecycle. |
| `GET` | `/v1/lease-agreements/deals/{dealId}/pdf` | Deal party | Download lease PDF. |
| `GET` | `/v1/admin/lease-agreements/pending-approvals` | Platform admin | Dual-control queue. |

*Extended:* `POST /v1/auth/register` accepts founding-partner fields; `POST .../deposit-return/host-confirm` accepts `evidenceManifestId`; partner reservations accept `payerType`.*

---

## Frontend changes

### New or substantially new pages & components

| Route | Component | Purpose |
|-------|-----------|---------|
| `/join` | `JoinPage` | Founding-partner signup (host/partner chooser). |
| `/how-it-works` | `HowItWorksPage` | Pre-launch marketing / FAQ. |
| `/app/admin/lease-agreements` | `LeaseAgreementTemplatesPage` | Admin template editor + lifecycle. |
| `/app/lease-agreements` | same | Arbitrator template access. |
| `/app/partner/inquiries` | `PartnerInquiriesPage` | Partner inquiry inbox. |
| `/app/applications/:id` | `CompletePartnerRequestPanel` | Tenant completes partner request. |
| `/app/deals/:dealId` | `LeaveStayReviewPanel` | Post-stay review. |
| Inquiry threads | `InquiryOfferPanel`, `InquiryParticipantsPanel` | Negotiation + partner invite. |
| `/app/partner/reservations` | payer toggle + SetupIntent | Org-pays booking. |
| Global | `RequireLaunchAccess`, `PublicConfigProvider` | Pre-launch marketplace gate. |

---

## Database & schema

Apply in dependency order. Use module-specific scripts under `tools/scripts/`.

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260703215021_SyncBillingModel` | Billing | Deposit-return handshake columns. |
| `20260707082327_AddPreLaunchSignupFields` | Auth | Founding-partner signup fields. |
| `20260709201002_AddPhoneVerificationFields` | Auth | Phone OTP hash, expiry, rate limits. |
| `20260709201011_AddSmsChannelRecipientAddress` | Notifications | SMS channel + provider message id. |
| `20260709205406_AddApplicationPayerFields` | Billing | `PayerType`, `PayerUserId` on applications. |
| `20260709205441_AddPartnerOrganizationStripeCustomerId` | Partner | Org Stripe customer id. |
| `20260709211812_AddInquiryOffers` | Inquiry | `inquiry.offers` table. |
| `20260709214543_AddInquiryPartnerParticipant` | Inquiry | Partner participant on sessions. |
| `20260709224622_InitialCreateLeaseAgreements` | LeaseAgreements | Templates, versions, deal documents. |
| `20260709224637_AddLeasePartyProfileFields` | Auth | Lease PDF party addresses. |
| `20260709224654_AddListingLeaseTerms` | Listings | Lease terms columns. |
| `20260710195857_AddDepositReturnEvidenceManifest` | Billing | Evidence manifest FK on payment confirmation. |
| `20260710195921_AddDepositReturnEvidenceManifest` | PlatformSettings | `deposit_return.window_days` seed. |
| **`InitialCreateReviews`** *(generate)* | Reviews | Stay reviews, windows, partner service reviews. |
| **`AddReviewWindowDaysSetting`** *(generate)* | PlatformSettings | `review.window_days` = 14. |

**Scripts:** `db-migrate-prelaunch-signup.ps1`, `db-migrate-twilio-sms.ps1`, `db-migrate-partner-booking-payer.ps1`, `db-migrate-inquiry-offers.ps1`, `db-migrate-inquiry-partner-participant.ps1`, `add-lease-agreement-migrations.ps1`, `db-migrate-deposit-return-evidence.ps1`, `db-migrate-reviews.ps1`.

---

## Security & privacy posture

- **Pre-launch gating** — Marketplace and app hidden from general public until launch; only exempt operational roles retain full access.
- **Phone OTP** — Rate-limited send; hash stored server-side; SMS only to verified numbers.
- **Partner payer** — Org SetupIntent scoped to partner staff; tenant attach-payment limited to application tenant.
- **Deposit evidence** — Partial withholds require sealed manifest before host confirm; supports dispute audit trail.
- **Reviews** — Bilateral blind publish; listing reviews anonymous; reputation aggregates exclude PII beyond public profile fields.

---

## Configuration & dependencies

- **Twilio** — `AccountSid`, `AuthToken`, `MessagingServiceSid` in gateway/worker config.
- **Platform settings** — `prelaunch.enabled`, `deposit_return.window_days`, `review.window_days`.
- **Stripe** — Partner organization customers for org-pays booking path.
- **Lease templates** — Jurisdiction codes align with jurisdiction packs module.

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run `tests/Lagedra.Tests.Unit`.
2. **Migrate** all contexts in table order; **generate** Reviews migrations before production deploy.
3. **Configure** Twilio, pre-launch flag, Stripe, and platform settings per environment.
4. **Deploy** API + worker, then web.
5. **Smoke tests**
   - **Pre-launch:** With flag on, anonymous `/listings` gated; `/join` signup creates waitlist lead; admin login works.
   - **Phone:** Send/confirm OTP; SMS notification delivers to verified number.
   - **Inquiry:** Propose offer → counter → accept; partner added to thread; partner inbox lists session.
   - **Partner booking:** Org-pays reservation with SetupIntent; tenant completes pending request on application detail.
   - **Lease:** Admin creates template → publish; deal PDF downloads with placeholders filled.
   - **Reviews:** Submit bilateral stay reviews; reputation appears on public profile after publish.
   - **Deposit return:** Partial withhold with evidence manifest; host confirm blocked without manifest.
   - **Regression:** Predetermined-deposit apply, OwnerRez sync, arbitration filing fee, host billing statement.

---

## Known limitations & follow-up (engineering)

- **Listing lease terms UI** — Backend and migrations ready; dedicated listing editor fields not yet wired in web.
- **Reviews migrations** — Module code complete; run `db-migrate-reviews.ps1` to generate before deploy.
- **Working tree** — Portion of this scope may be uncommitted; commit and pass CI before tagging.
- **`db-migrate.ps1`** — Does not yet include `ReviewsDbContext` or all partner contexts; use targeted scripts.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-07-11 | Pre-launch, lease agreements, inquiry offers, partner payer, reviews, SMS, deposit evidence. |

---

*End of release notes.*
