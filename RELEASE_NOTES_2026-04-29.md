# Lagedra Release Notes — Wednesday 29/04/2026

> Release window: **Wed 29/04/2026 · 10:00 PST** (smoke test 13:00 PST)
> Branch / tag: `release/2026-04-29`
> Replaces / supersedes: `R-2026-04-22` (rolled forward)
> Today (notes drafted): Fri 24/04/2026 — per the Friday "plan next Wednesday scope" cadence in `RELEASE_PLAN_2026-04.md`.

---

## TL;DR

This release ships the combined **R-2026-04-22 bug-fix slate** (KYC start, checkout creation, listing `Book now`, identity/risk status, similar listings, search relevance, reservations routing) **plus the R-2026-04-29 product follow-ups** queued in `RELEASE_PLAN_2026-04.md` — a new marketplace home, an Airbnb-style hero search, a guided listing wizard, server-proxied media uploads (avatar / listing photos / evidence), a separate landlord listing view, a much richer profile page, and end-to-end error boundaries with friendly error messaging.

Single deployment covers backend + frontend (see Section "Rollout").

---

## Highlights

### Marketplace experience

- **New marketplace home page** at `/listings` — category browse strip (Apartments, Houses, Villas, Studios, Lofts, Cabins, Cottages), curated rails, and a sticky `HeroSearchBar`. The previous list view moves to `/listings/search`. (`apps/web/src/features/listings/pages/MarketplaceHomePage.tsx`, `apps/web/src/app/routes.tsx`)
- **Hero search bar** with segmented Where / Check-in / Check-out / Filter pills, popular-destination suggestions, and a custom range calendar. (`apps/web/src/features/listings/components/HeroSearchBar.tsx`, `DateRangeCalendar.tsx`)
- **Listing detail polish**: photo lightbox, privacy-preserving approximate map (radius circle, no exact pin), refreshed `ListingCard`, and updated `SaveButton` interactions. (`PhotoLightbox.tsx`, `ListingApproxMap.tsx`, `ListingDetailPage.tsx`)
- **Search page** rebuilt — fixes the broken keyword filter (`keyword=` no longer ignored), restores result counts, adds proper empty/error states. (~+800 LOC, `SearchPage.tsx`, `listingApi.ts`)

### Hosting & landlord tools

- **Listing wizard** — multi-step create flow covering basics, pricing, amenities, considerations, safety devices, and house rules with progress and validation. (`apps/web/src/features/listings/components/ListingWizard.tsx`, `CreateListingPage.tsx`)
- **Landlord listing detail** — dedicated `/app/landlord/listings/:id` page so hosts get edit / publish / unpublish actions, media management, and listing analytics shortcuts without seeing the public booking UI. (`LandlordListingDetailPage.tsx`)
- **My listings** redesigned — status cards, quick actions, empty states, and richer status badges. (~+320 LOC, `MyListingsPage.tsx`)
- **Edit listing** parity with the wizard for amenities, considerations, and safety devices. (~+340 LOC, `EditListingPage.tsx`)
- **Saved listings** — empty/error states and consistent card layout. (`SavedListingsPage.tsx`)

### Verification & profile

- **Verification page hardened** — clearer step state, retry paths, surfaced API errors, and recovery for `404` identity/risk status responses. (~+440 LOC, `VerificationPage.tsx`, `useVerification.ts`)
- **Profile page expansion** — avatar upload, expanded personal/contact/preferences sections, inline validation, and consent indicators. (~+600 LOC, `ProfilePage.tsx`, `authApi.ts`)
- **Profile photo upload** end-to-end: new `UploadProfilePhotoCommand`, avatar bucket wiring, and `ProfilePhotoUrl` exposed via `UserProfileDto` / `GetCurrentUserQuery`. (`src/Lagedra.Auth/Application/Commands/UploadProfilePhotoCommand.cs`, `Presentation/Endpoints/AuthEndpoints.cs`)

### Uploads (server-proxied)

To eliminate browser→bucket CORS configuration and surface a single, clear error path, three new direct-upload commands proxy through the API:

- `UploadProfilePhotoCommand` — avatars to the users bucket.
- `UploadListingMediaCommand` — listing photos (gallery) and videos (`VirtualTourUrl`); listings bucket gets a public-read policy so URLs can be returned directly. (`src/Lagedra.Modules/ListingAndLocation/Application/Commands/UploadListingMediaCommand.cs`, `ListingEndpoints.cs`)
- `DirectUploadEvidenceCommand` — single-call evidence upload that atomically writes the manifest row alongside object storage. (`src/Lagedra.Modules/Evidence/Application/Commands/DirectUploadEvidenceCommand.cs`, `UploadEndpoints.cs`)
- Frontend evidence flow updated end-to-end (`EvidenceUpload.tsx`, `useEvidence.ts`, `evidenceApi.ts`).

### Errors, resilience, and UX consistency

- **Friendly error layer** — `apps/web/src/lib/errors.ts` maps Axios / HTTP statuses (incl. `400/401/403/404/409/422/429/451/5xx`) to user-readable titles and messages with a stable machine-readable code passthrough.
- **Error boundaries everywhere**:
  - `RouteErrorBoundary` plugged into every route group so a single failed page no longer wipes the layout.
  - `PageBoundary` wraps every `LazyPage` and resets on pathname change.
  - Reusable `ErrorState` component with retry, used across listings, applications, evidence, and verification surfaces.
- **HTTP client** (`apps/web/src/api/http.ts`) updated to normalize errors into the friendly layer.

### Routing fixes (from 17/04 QA sweep)

- `/app/reservations` now redirects to `/app/deals` (was redirecting to `/listings`). Nav label and deep links are aligned.
- `/app/landlord/stripe-onboarding` renamed to `/app/landlord/payout-setup`; old path 301-style redirects to the new one to preserve emails / docs.
- `LazyPage` now renders inside both Suspense and PageBoundary (`apps/web/src/app/routes.tsx`).

---

## Bug Fixes (carried from R-2026-04-22)

| ID | Severity | Area | Fix |
|----|----------|------|-----|
| 1 | High | Verification | `POST /v1/identity/kyc/start` no longer returns `451`; consent middleware allow-listed for KYC start; UI shows actionable error. (`ConsentMiddleware.cs`, `IdentityEndpoints.cs`, `VerificationPage.tsx`) |
| 2 | High | Verification | `GET /v1/identity/status` and `GET /v1/risk/{userId}` return correct payloads; `404`s replaced with structured "no record yet" responses. (`IdentityEndpoints.cs`, `useVerification.ts`) |
| 3 | High | Checkout | `POST /v1/deals/{dealId}/checkout` no longer fails silently with `400`; failure reason surfaced in `CheckoutPage.tsx`. (`CreateCheckoutPaymentIntentCommand.cs`, `StripeService.cs`) |
| 4 | High | Listing detail | `Book now` is no longer click-intercepted by the SVG overlay; pointer-events corrected. (`ListingDetailPage.tsx`, `index.css`) |
| 5 | Medium | Routing | `Reservations` nav now resolves to `/app/deals`; deep link `/app/reservations` redirects correctly. (`routes.tsx`, `MarketplaceLayout.tsx`) |
| 6 | Medium | Listings | `GET /v1/listings/{id}/similar` returns `200`; UI falls back to empty state when none. (`ListingEndpoints.cs`, `listingApi.ts`) |
| 7 | Medium | Search | Keyword search actually filters results; `keyword` parameter wired through. (`ListingEndpoints.cs`, `SearchPage.tsx`) |
| 8 | Low/Med | Realtime | Notifications hub close `1006` reduced via reconnect/backoff; surfaced via friendly error layer. (`http.ts`) |
| 9 | Low | Auth UX | `Loading session...` flicker reduced; permissions hydration deferred. (`permissions.ts`, `MarketplaceLayout.tsx`) |

All P0/P1 items from the 17/04 Live QA Sweep in `RELEASE_PLAN_2026-04.md` are addressed in this release.

---

## Backend Changes

### New endpoints

- `POST /v1/auth/me/photo` — multipart avatar upload. Returns updated `UserProfileDto`. (`AuthEndpoints.cs`)
- `POST /v1/listings/{id}/media` — multipart listing photo/video upload. (`ListingEndpoints.cs`)
- `POST /v1/uploads/direct` — single-call evidence upload (proxies file + writes manifest atomically). (`UploadEndpoints.cs`)
- `GET /v1/host/payment-details` improvements + `HostStripeEndpoints` adjustments for the renamed payout-setup flow.

### Modified endpoints

- `POST /v1/identity/kyc/start` — no longer guarded by full consent middleware; tighter error contract.
- `POST /v1/deals/{dealId}/checkout` — returns structured failure (`code`, `message`) instead of bare `400`.
- `GET /v1/listings/{id}/similar` — query corrected; consistent shape with search results.
- `GET /v1/listings` — `keyword` honored.
- `GET /v1/identity/status`, `GET /v1/risk/{userId}` — return `200` with status payload (no record => explicit `none` state).

### Infrastructure

- `MinioStorageService` / `MinioSettings` — multi-bucket support (users, listings, evidence, exports), per-bucket public-read policy where appropriate, content-type / size enforcement. (`MinioStorageService.cs`, `MinioSettings.cs`, `IObjectStorageService.cs`)
- `EncryptionService` hardening (key rotation tolerant ciphertext header). (`EncryptionService.cs`)
- `MalwareScanPollingJob` adjusted for the new direct-upload manifest path. (`MalwareScanPollingJob.cs`)
- `AnalyticsModuleRegistration` registers the new listing media analytics events. (`AnalyticsModuleRegistration.cs`)
- API gateway config: route entries for new upload endpoints. (`src/Lagedra.ApiGateway/appsettings.json`)
- `Dockerfile`: includes the new module copy lines and ensures `/app/data-protection-keys` permission step in both runtime stages. (`Dockerfile`)

---

## Frontend Changes

### New components / pages

- `MarketplaceHomePage`, `LandlordListingDetailPage`
- `HeroSearchBar`, `DateRangeCalendar`, `PhotoLightbox`, `ListingApproxMap`, `ListingWizard`
- `PageBoundary`, `RouteErrorBoundary`, `ErrorState`
- `lib/errors.ts` (friendly error mapping)

### Notable updates

- `MarketplaceLayout` — sticky search header, refined nav, consent banner positioning.
- `ApplyDialog`, `ApplicationDetailPage` — improved validation, retry UX.
- `EvidenceUpload` — switched to `DirectUploadEvidenceCommand`, removed presigned-URL CORS path.
- `HostStripeOnboardingPage` — simplified, points to `/app/landlord/payout-setup`.
- `index.html`, `index.css` — base font + token cleanup.

### New API surface

- `apps/web/src/api/endpoints.ts` adds entries for direct upload endpoints and avatar upload.
- `apps/web/src/api/http.ts` integrates the friendly error layer.

---

## Release Gates

Per `RELEASE_PLAN_2026-04.md` ("29/04 Release Gates" + "22/04 Release Gates"):

- [ ] No `4xx/5xx` for: verify identity, open booking, proceed checkout (regression of 22/04 fixes).
- [ ] Smoke test on `/listings`, `/listings/search`, listing detail, `/app/deals`, `/app/my-applications`, `/app/verification`, `/app/landlord/listings`, `/app/landlord/payout-setup`.
- [ ] Admin pages load without blocking errors and with valid API responses (arbitration backlog, fraud flags, blog posts).
- [ ] Billing/checkout E2E happy path **and** failure path both tested; failure surfaces a toast.
- [ ] Avatar upload, listing media upload, and direct evidence upload each succeed end-to-end with malware scan completing.
- [ ] Error banner/toast coverage on every failed API action (spot check 5 surfaces).
- [ ] Updated operational docs (`DEPLOYMENT.md`) published.

---

## Rollout

Full deploy (backend + frontend in parallel). Follow `DEPLOYMENT.md` Section D:

1. **Backend** — Section A: build `lagedra/api` and `lagedra/worker`, push to ECR, `aws ecs update-service --force-new-deployment` for both services. Wait for `running == desired`.
2. **Frontend** — Section B: build via `apps/web/Dockerfile.build`, `aws s3 sync` to `lagedra-web-prod`, then CloudFront invalidation `/*`.
3. **Config** — no task-definition env changes required for this release. (No new SSM keys.)
4. **Smoke** — run the gate checklist above against `https://lagedra.com` and `https://api.lagedra.com/health`.

Rollback: redeploy previous ECS task revisions and re-sync the prior `dist/` snapshot to S3, then invalidate CloudFront.

---

## Known Limitations / Follow-ups (post-release)

- Listing video transcoding is not yet automated — uploaded MP4s are served as-is via `VirtualTourUrl`.
- WebSocket reconnect is best-effort; a visible health indicator is queued for the next Wednesday.
- Map cluster view at high zoom levels can still flicker on slow networks.

---

## Ownership & Tracking

- Board labels: `R-2026-04-29` (everything in this release), `P0`/`P1`/`P2` for priority.
- Hotfix window: **Thu 30/04** (post-release monitoring per cadence).
- Next planning: **Fri 01/05/2026** for the 06/05 release.
