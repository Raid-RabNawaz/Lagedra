# Lagedra Weekly Release Plan (Wednesday Cadence)

## Cadence (Every Week)

- **Monday**: triage production issues, confirm scope, assign owners.
- **Tuesday (12:00 PST)**: code freeze for the release branch, regression test + release notes draft.
- **Wednesday (10:00 PST)**: production release window.
- **Wednesday (13:00 PST)**: smoke test on critical flows (listings, applications, reservations, checkout, verification).
- **Thursday**: post-release monitoring and hotfix decision.
- **Friday**: plan the next Wednesday scope.

---

## Live QA Sweep (lagedra.com) - 17/04/2026

Tested manually across public listings and authenticated app flows with runtime console/network validation.

### Confirmed Issues

1. **KYC flow blocked (High)**
   - `POST /v1/identity/kyc/start` returns `451`.
   - User action: `Start Identity Verification` on `/app/verification`.
   - Impact: identity verification cannot start.

2. **Identity/risk status endpoints returning not found (High)**
   - `GET /v1/identity/status?userId=...` returns `404`.
   - `GET /v1/risk/{userId}` returns `404`.
   - Impact: verification/risk status cannot be reliably shown.

3. **Checkout start fails silently (High)**
   - `POST /v1/deals/{dealId}/checkout` returns `400` after clicking `Proceed to Payment`.
   - UI remains on checkout with no clear error.
   - Impact: reservation payment cannot proceed.

4. **Book Now action is not clickable (High)**
   - On listing detail, `Book now` click is intercepted by another element (`svg` overlay/interceptor).
   - Impact: user cannot start booking from listing detail.

5. **Reservations route mismatch (Medium)**
   - Nav item label is `Reservations` but route points to `/app/deals`.
   - Direct `/app/reservations` redirects to `/listings`.
   - Impact: inconsistent routing and potential deep-link failures.

6. **Similar listings API error (Medium)**
   - `GET /v1/listings/{id}/similar` returns `400`.
   - Impact: similar/recommended listings section cannot populate.

7. **Search relevance appears broken (Medium)**
   - Search sends `keyword=zzzz-no-results-123`, but results list remains unchanged (20/25).
   - Impact: keyword search does not filter effectively.

8. **Realtime connection instability (Low/Medium)**
   - Console repeatedly logs websocket close `1006` for notifications hub.
   - Impact: realtime notifications may intermittently disconnect.

9. **Session/auth state flicker (Low)**
   - Route transitions frequently show `Loading session...` and temporary anonymous header state.
   - Impact: UX instability and perceived authentication inconsistency.

---

## Release: Wednesday 22/04/2026 (Bugs & Fixes)

### Release Objective
Stabilize booking + verification journeys and remove blockers for payment and trust onboarding.

### Scope (Must Ship)

- Fix KYC start flow (`451`) and ensure actionable UI error handling.
- Fix checkout creation (`400`) and show failure reason in UI.
- Fix listing `Book now` click interception.
- Fix identity/risk status endpoint behavior (`404`).
- Align reservations routing (`/app/deals` vs `/app/reservations`) and deep links.
- Fix similar listings API (`400`) and fallback rendering when empty.
- Patch search keyword behavior to return correct filtered results.

### Release Gates

- No `4xx/5xx` for core actions: verify identity, open booking, proceed checkout.
- Smoke test pass on: `/listings`, listing detail, `/app/deals`, `/app/my-applications`, `/app/verification`.
- Error banner/toast coverage for failed API actions.

---

## Pending Queue: Wednesday 29/04/2026

These are planned items after the bug-fix release, assuming 22/04 blockers are closed.

### Product/UX Follow-ups

- Improve session loading UX to prevent role/header flicker.
- Add stronger empty/error states for saved listings, applications, and similar listings modules.
- Add retry/health indicator for websocket notifications.

### In-Progress Feature Areas (from current repo work)

- Activation + billing checkout refinements.
- Admin panel updates: arbitration backlog, fraud flags, blog posts.
- Application flow updates (`ApplyDialog`, application detail improvements).
- Listing API/request handling updates.
- Verification and identity endpoint hardening.
- Consent middleware and deployment documentation finalization.

### 29/04 Release Gates

- Regression test of all flows fixed on 22/04.
- Admin pages load without blocking errors and with valid API responses.
- Billing/checkout E2E happy path and failure path both tested.
- Updated operational docs published with the release notes.

---

## Ownership and Tracking

- Track all items in one release board with labels: `R-2026-04-22` and `R-2026-04-29`.
- Priority labels: `P0` (blocker), `P1` (high user impact), `P2` (quality improvements).
- Any unresolved `P0/P1` on Tuesday freeze automatically rolls to hotfix or blocks release.
