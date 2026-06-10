# Lagedra Release Notes — Friday 16 May 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Cumulative working-tree changes on branch `dev` relative to remote `origin/dev` at authoring time; single deployment is assumed to cover API gateway, modules, and web client.

**Program references:** Phase 16 (booking pre-flight & payments), Phase 16.10 (host one-tap approval from email), Phase 17 (pre-booking structured inquiry), as reflected in source annotations and migration tooling.

---

## Executive summary

This release advances the marketplace booking path from **browse → date selection → priced quote → consent gate → apply with card on file**, and extends **structured inquiry** to **listing-scoped threads** before a deal exists. It introduces **Stripe customer persistence** for tenants, **SetupIntent** capture during application, **optional default security deposit** on listings, and a **token-gated, sessionless API** so hosts can **approve applications from email** without signing in. Supporting changes include **Truth Surface confirmation party enforcement**, **privacy consent status** suitable for booking pre-flight, **feature-flag plumbing** for rollout control, and **operational migration scripts** for Phase 16 and Phase 17 database updates.

---

## Highlights

### Booking & applications (Phase 16)

- **Listing quote API** — `POST /v1/listings/{listingId}/quote` returns an itemised quote (rent, deposit, insurance, disclosed protocol fee) from check-in/check-out dates, with validation against minimum/maximum stay.
- **Default deposit on listings** — Hosts can set `DefaultDepositCents` on create/update; quote logic prefers this value when present before falling back to suggested bands or caps.
- **Booking panel on listing detail** — Prospective guests select dates; the client calls availability, quote, and consent status before enabling **Apply**.
- **Stripe customer on user** — `AspNetUsers.StripeCustomerId` stores the Stripe Customer id after first booking-related payment setup.
- **Card on file at apply** — `POST /v1/applications/setup-intent` issues a **SetupIntent** (idempotent per tenant/listing); the captured **PaymentMethod** is submitted with the application (`deal_applications.StripePaymentMethodId`).
- **Application submission** — Submit flow extended to accept payment method metadata consistent with the SetupIntent path.

### Host approval from email (Phase 16.10)

- **Anonymous action endpoint** — `POST /v1/actions/approve-application` accepts a signed **approve** token and optional deposit amount; authentication is **HMAC on the token**, not JWT, so the host needs no active session.
- **Public landing** — `/host/approve` loads the token from the query string, posts to the action endpoint, and surfaces success or structured errors (expired, already used, invalid signature, missing deposit, etc.).
- **Notifications** — Booking notification pipeline issues time-limited approve tokens for transactional email templates.

### Structured inquiry before booking (Phase 17)

- **Data model** — Inquiry sessions may exist **without** a deal: `DealId` nullable; **`ListingId`** and **`TenantUserId`** required with indexing for “my thread on this listing.”
- **Listing inquiry API** — `POST /v1/listings/{listingId}/inquiry` starts (or resumes) a session; `GET /v1/listings/{listingId}/inquiry/mine` returns the tenant’s active thread for that listing.
- **Session-based thread API** — `GET/POST` under `/v1/inquiry-sessions/{sessionId}` for questions and answers; parallel **host** and **tenant** inboxes: `GET /v1/inquiry-sessions/host`, `GET /v1/inquiry-sessions/mine`.
- **Deal-linked inquiry** — Existing `/v1/inquiries/{dealId}` flows remain; manual close is restricted; **lock** path available for session governance.
- **UI** — Routes for **host inquiries**, **my inquiries**, and **listing inquiry by session id**; inquiry components updated for session and open-text question support.
- **Notifications** — Handler for **listing inquiry started**; notification template seeding support.

### Truth Surface & deals

- **Truth Surface confirm** — Confirm and reconfirm flows enforce that the caller confirms **as the correct party** (landlord vs tenant); platform administrators retain elevated support paths per existing authorization.
- **Deals experience** — Deal list/detail presentation and timeline/badge refinements; shared status integration updates for applications.

### Privacy & compliance

- **`GET /v1/privacy/consents/me/status`** — Allows signed-in users to poll **their own** consent state on an exempt path (consistent with booking pre-flight and consent middleware behavior).
- **Consent checker** — Integration surface expanded to support consumer callers that need consent-aware decisions.

### Platform & operations

- **Feature flags** — Configuration-backed `IFeatureFlags` (e.g. `FeatureFlags:BookingFlow.V2` / `FeatureFlags__BookingFlow.V2` in containers).
- **Database migrations** — Auth, billing, listings, and inquiry contexts updated; Phase 16 and Phase 17 helper scripts document apply order (Phase 16 before Phase 17; Phase 17 backfill can reference billing when present).
- **API gateway** — Registers action endpoints and other module routes as required for the above surface area.

---

## Backend changes

### New or materially new HTTP endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/v1/listings/{listingId}/quote` | Stay-scoped pricing quote for booking pre-flight. |
| `POST` | `/v1/applications/setup-intent` | Create Stripe SetupIntent + return client secret and customer id. |
| `POST` | `/v1/actions/approve-application` | Token-gated host approval (anonymous). |
| `POST` | `/v1/listings/{listingId}/inquiry` | Start or attach pre-booking inquiry session. |
| `GET` | `/v1/listings/{listingId}/inquiry/mine` | Tenant’s inquiry for a listing. |
| `GET` | `/v1/inquiry-sessions/host` | Host inbox. |
| `GET` | `/v1/inquiry-sessions/mine` | Tenant inquiry inbox. |
| `GET` | `/v1/inquiry-sessions/{sessionId}` | Session-scoped thread. |
| `POST` | `/v1/inquiry-sessions/{sessionId}/questions` | Submit question to session. |
| `POST` | `/v1/inquiry-sessions/{sessionId}/answers` | Submit response to session. |
| `GET` | `/v1/privacy/consents/me/status` | Current user consent snapshot for UI gating. |

*Existing inquiry routes under `/v1/inquiries/{dealId}` are retained and updated for consistency with the expanded domain.*

### Domain & application layer (selected)

- **Activation & billing:** `CreateBookingSetupIntentCommand`, `ApproveApplicationByTokenCommand`, extended `SubmitApplicationCommand` / `ApproveDealApplicationCommand`, payment confirmation handler adjustments, deal application aggregate and status provider updates.
- **Listings:** `GetListingQuoteQuery`, listing aggregate and DTOs for default deposit, availability query refinements.
- **Structured inquiry:** `StartListingInquiryCommand`, session-scoped submit commands, listing inquiry queries, `ListingInquiryStartedEvent` and notification handler, integrity scan compatibility.
- **Infrastructure:** `StripeService` / `IStripeService` extensions (customer resolution, SetupIntent), `ActionTokenService`, `UserStripeProfileService`, shared integration interfaces (`IUserStripeProfileService`, `IActionTokenService`, `IFeatureFlags`, and related contracts).

---

## Frontend changes

### New or substantially new user-facing surfaces

- **`BookingPanel`** — Date-driven booking widget on listing detail: availability, quote breakdown, consent/KYC messaging, link to **Apply** / inquiry.
- **`ApplyDialog`** — Stripe Elements integration with setup-intent pre-flight; submission carries payment method where applicable.
- **`HostApprovePage`** — Public `/host/approve` one-tap approval from email tokens.
- **`HostInquiriesPage`**, **`MyInquiriesPage`**, **`ListingInquiryPage`** — Pre-booking and session-based inquiry UX; navigation integrated into app shell and member routing.
- **`InlineTruthSurfaceConfirm`** — Inline confirmation affordances within billing/activation flows where embedded.

### API client & types

- Endpoint catalog extended for **quote**, **setup-intent**, **listing inquiry**, and **inquiry session** routes.
- DTO and hook updates across listings, inquiry, applications, truth surface, and privacy client modules.

---

## Database & schema

Applied through EF Core migrations (exact names in repository):

| Context | Migration (timestamp prefix) | Summary |
|---------|------------------------------|---------|
| `ListingsDbContext` | `20260512202411_AddDefaultDepositCentsToListings` | Nullable `DefaultDepositCents` on `listings.listings`. |
| `AuthDbContext` | `20260512202426_AddStripeCustomerIdToUsers` | Nullable `StripeCustomerId` on `auth.AspNetUsers`. |
| `BillingDbContext` | `20260512202439_AddStripePaymentMethodIdToApplications` | Nullable `StripePaymentMethodId` on `activation_billing.deal_applications`. |
| `InquiryDbContext` | `20260514222405_AddListingScopedInquiry` | `sessions.DealId` nullable; `ListingId`, `TenantUserId`, indexes; `questions.OpenQuestionText`; conditional backfill from billing when available. |

**Apply order:** Run Phase 16 migrations before Phase 17 where the inquiry backfill references billing data. Use `tools/scripts/db-migrate-phase16.*` and `tools/scripts/db-migrate-phase17.*` as documented in those scripts.

---

## Security & privacy posture

- **One-tap approval** relies on **short-lived, signed tokens** with explicit error contracts for reuse, expiry, and tampering; the endpoint group is **anonymous by design** — operational monitoring should alert on abuse patterns.
- **Truth Surface confirmation** rejects cross-party confirmation for non-admin callers, reducing accidental or fraudulent attestation.
- **Consent status** endpoint is scoped to the authenticated **current user** and is intended for exempt routing alongside booking flows.

---

## Configuration & dependencies

- **Stripe:** API version and keys must be configured in each environment; SetupIntent and customer APIs require current Stripe integration settings on the gateway host.
- **Feature flags:** `BookingFlow.V2` (and extensible `IFeatureFlags`) for progressive rollout.
- **Front-end:** Lockfile updates (`pnpm-lock.yaml`) where package resolution changed — verify CI/install uses the committed lockfile.

---

## Deployment & verification checklist

1. **Build** solution and web app; run unit/integration tests as defined by the repository pipeline.
2. **Migrate** databases in order: listings + auth + billing (Phase 16), then inquiry (Phase 17).
3. **Configure** Stripe, JWT, Brevo (or equivalent), and feature flags per environment.
4. **Smoke tests**
   - Anonymous `POST /v1/actions/approve-application` with valid/invalid token matrix.
   - Apply flow: setup-intent → submit application → host approve (authenticated and token paths).
   - Listing detail: dates → quote → apply CTA gating with consent.
   - `POST /v1/listings/{id}/nquiry` → thread under session id → host/tenant inbox lists.
   - Truth Surface confirm as landlord vs tenant (forbid cross-party).

---

## Known limitations & follow-up (engineering)

- Legacy inquiry rows rely on migration backfill and integrity scan for edge cases where join data was missing; monitor first production run after Phase 17.
- Feature flag defaults should be confirmed per environment before enabling **Booking Flow V2** broadly.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-05-16 | Initial formal notes from working-tree scope vs `origin/dev`. |

---

*End of release notes.*
