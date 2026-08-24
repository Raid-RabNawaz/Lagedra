# Lagedra API Guide

Short, practical reference for the Lagedra HTTP API (`v1`).

## How to call the API

1. **Base URL** — your environment’s API host (local, staging, or production).
2. **Auth** — most routes need a Bearer JWT:

   ```http
   Authorization: Bearer <access_token>
   ```

3. **Content type** — send JSON unless the route says multipart (file uploads):

   ```http
   Content-Type: application/json
   ```

4. **Get a token** — `POST /v1/auth/login` or `POST /v1/auth/register`, then use the returned access token. Refresh with `POST /v1/auth/refresh`.

5. **Public routes** — health, login/register, listing search, blog, and some SEO pages work without a token.

---

## Common flows

### Sign up and sign in

1. `POST /v1/auth/register` — create account  
2. Verify email via `GET /v1/auth/verify-email` (link from email)  
3. `POST /v1/auth/login` — get access + refresh tokens  
4. `GET /v1/auth/me` — confirm who you are  

### Find a place and apply (guest)

1. `GET /v1/listings` — search (keyword, map bounds, filters, page/pageSize)  
2. `GET /v1/listings/{listingId}` — listing detail  
3. `POST /v1/listings/{listingId}/quote` — price for check-in/out  
4. `GET /v1/applications/preview` — booking preview  
5. `POST /v1/applications/setup-intent` — Stripe setup (if paying on apply)  
6. `POST /v1/applications` — submit application  

### Host reviews applications

1. `GET /v1/applications/listing/{listingId}` — apps for a listing  
2. If the listing is managed by a property manager, the named home owner must consent first (`POST /v1/applications/{id}/owner-consent` or email token `/v1/actions/consent-owner-tenancy`)  
3. `POST /v1/applications/{id}/approve` — approve (blocked until owner consents on PM listings)  
4. or `POST /v1/applications/{id}/reject`  
5. Email-link approve: `POST /v1/actions/approve-application` with token  

### Checkout and stay

1. `POST /v1/deals/{dealId}/checkout`  
2. `POST /v1/deals/{dealId}/checkout/confirm`  
3. `GET /v1/deals/{dealId}/checkout/status`  
4. `POST /v1/deals/{dealId}/activate` — activate deal when ready  
5. `GET /v1/deals/mine` — your deals  

### Host payouts (Stripe)

1. `POST /v1/hosts/payouts/start` — start onboarding (`returnUrl`, `refreshUrl`)  
2. Complete Stripe’s hosted flow  
3. `GET /v1/hosts/payouts/status` — check readiness  
4. `POST /v1/hosts/payouts/refresh-link` — if the link expired  

### Ask about a listing (inquiry)

1. `POST /v1/listings/{listingId}/inquiry` — start session  
2. `POST /v1/inquiry-sessions/{sessionId}/questions` — ask  
3. Host: `POST /v1/inquiry-sessions/{sessionId}/answers`  
4. Optional offers: propose / accept / counter under `/offers`  

---

## Auth

| Method | Path | Notes |
|--------|------|--------|
| POST | `/v1/auth/register` | Create account |
| GET | `/v1/auth/verify-email` | `userId`, `token` query params |
| POST | `/v1/auth/resend-verification` | Resend email |
| POST | `/v1/auth/login` | Email + password |
| POST | `/v1/auth/external-login` | Social / IdP token |
| POST | `/v1/auth/refresh` | New access token |
| POST | `/v1/auth/logout` | Invalidate refresh token |
| POST | `/v1/auth/forgot-password` | Start reset |
| POST | `/v1/auth/reset-password` | Finish reset |
| GET | `/v1/auth/me` | Current user |
| PUT | `/v1/auth/me` | Update profile |
| POST | `/v1/auth/me/profile-photo` | Upload photo (multipart) |
| DELETE | `/v1/auth/me/profile-photo` | Remove photo |
| POST | `/v1/auth/change-password` | Change password |
| POST | `/v1/auth/phone/send-code` | SMS code |
| POST | `/v1/auth/phone/confirm` | Confirm with `code` |
| GET | `/v1/auth/users` | Admin list (`page`, `pageSize`) |
| PUT | `/v1/auth/users/{userId}/role` | Admin set role |
| POST | `/v1/auth/users/{userId}/send-set-password-email` | Admin |
| GET | `/v1/auth/users/{userId}/public-profile` | Public profile |

---

## Listings

| Method | Path | Notes |
|--------|------|--------|
| GET | `/v1/listings` | Search; requires `page`, `pageSize` |
| POST | `/v1/listings` | Create listing |
| GET | `/v1/listings/mine` | Host’s listings |
| GET | `/v1/listings/{listingId}` | Detail |
| PUT | `/v1/listings/{listingId}` | Update |
| DELETE | `/v1/listings/{listingId}` | Delete |
| POST | `/v1/listings/{listingId}/publish` | Publish |
| POST | `/v1/listings/{listingId}/submit-for-review` | Send to admin review |
| POST | `/v1/listings/{listingId}/close` | Close |
| GET | `/v1/listings/{listingId}/similar` | Similar listings |
| GET | `/v1/listings/{listingId}/share-url` | Share link |
| GET | `/v1/listings/{listingId}/price-history` | Price history |
| GET | `/v1/listings/{listingId}/availability` | Optional `from` / `to` |
| POST | `/v1/listings/{listingId}/quote` | Quote for dates |
| POST | `/v1/listings/{listingId}/block-dates` | Block dates |
| DELETE | `/v1/listings/{listingId}/block-dates/{blockId}` | Unblock |
| POST | `/v1/listings/{listingId}/photos` | Add photo metadata |
| DELETE | `/v1/listings/{listingId}/photos/{photoId}` | Remove photo |
| PUT | `/v1/listings/{listingId}/photos/{photoId}/cover` | Set cover |
| PUT | `/v1/listings/{listingId}/photos/reorder` | Reorder |
| POST | `/v1/listings/{listingId}/media/upload` | Upload file (multipart) |
| POST | `/v1/listings/import-from-url` | Import from URL |
| POST | `/v1/listings/{listingId}/approx-location` | Approx lat/lng |
| POST | `/v1/listings/{listingId}/lock-address` | Lock precise address |

### Listing definitions (amenities, safety, considerations)

| Method | Path |
|--------|------|
| GET | `/v1/listing-definitions/amenities` |
| GET | `/v1/listing-definitions/safety-devices` |
| GET | `/v1/listing-definitions/considerations` |

Admin CRUD: `/v1/admin/listing-definitions/...`

Admin review:  
`GET /v1/admin/listings/pending-review` · `POST .../approve` · `POST .../deny`

---

## Applications

| Method | Path | Notes |
|--------|------|--------|
| GET | `/v1/me/verification-tier` | Guest verification tier |
| GET | `/v1/applications/preview` | Requires listingId, checkIn, checkOut |
| POST | `/v1/applications/setup-intent` | Stripe setup intent |
| POST | `/v1/applications` | Submit application |
| GET | `/v1/applications/mine` | My applications |
| GET | `/v1/applications/owner-pending` | Owner consent inbox |
| GET | `/v1/applications/{id}` | One application |
| GET | `/v1/applications/listing/{listingId}` | By listing (host) |
| POST | `/v1/applications/{id}/approve` | Host approve (blocked until owner consents on PM listings) |
| POST | `/v1/applications/{id}/reject` | Host reject |
| POST | `/v1/applications/{id}/owner-consent` | Home owner consents to the tenancy |
| POST | `/v1/applications/{id}/owner-decline` | Home owner declines the tenancy |
| POST | `/v1/applications/{id}/attach-payment` | Attach payment method |
| POST | `/v1/actions/approve-application` | Approve via email token |
| POST | `/v1/actions/consent-owner-tenancy` | Owner consent via email token |
| POST | `/v1/actions/decline-owner-tenancy` | Owner decline via email token |

---

## Deals, checkout, payment

| Method | Path | Notes |
|--------|------|--------|
| GET | `/v1/deals/mine` | Optional `phase` |
| GET | `/v1/deals/{dealId}/stay-access` | Access info for stay |
| POST | `/v1/deals/{dealId}/activate` | Activate |
| POST | `/v1/deals/{dealId}/checkout` | Start checkout |
| POST | `/v1/deals/{dealId}/checkout/confirm` | Confirm |
| GET | `/v1/deals/{dealId}/checkout/status` | Status |
| GET | `/v1/deals/{dealId}/payment/details` | Payment details |
| GET | `/v1/deals/{dealId}/payment/status` | Payment status |
| POST | `/v1/deals/{dealId}/payment/confirm` | Confirm payment |
| POST | `/v1/deals/{dealId}/payment/confirm-platform-payment` | Platform payment |
| POST | `/v1/deals/{dealId}/payment/dispute` | Dispute |
| POST | `/v1/deals/{dealId}/payment/cancel` | Cancel booking |
| POST | `/v1/deals/{dealId}/payment/damage-claim` | File damage claim |
| POST | `/v1/deals/{dealId}/payment/begin-move-out` | Start move-out |
| POST | `/v1/deals/{dealId}/payment/deposit-return/host-confirm` | Host confirms return |
| POST | `/v1/deals/{dealId}/payment/deposit-return/tenant-confirm` | Tenant confirms |

Damage claim resolution:  
`PUT .../damage-claims/{claimId}/approve|reject|partial-approve`

Billing:  
`GET .../billing` · `GET .../proration-quote` · `POST .../stop-billing` · `GET /v1/me/billing/statement`

---

## Host payments & Stripe

| Method | Path |
|--------|------|
| PUT/GET | `/v1/hosts/payment-details` |
| POST | `/v1/hosts/payouts/start` |
| POST | `/v1/hosts/payouts/refresh-link` |
| GET | `/v1/hosts/payouts/status` |
| POST | `/v1/hosts/stripe/onboard` |
| POST | `/v1/hosts/stripe/refresh-link` |
| GET | `/v1/hosts/stripe/status` |

`HostStripeOnboardRequest`: optional `returnUrl`, `refreshUrl`.

---

## Channels (PMS / channel managers)

Installed providers today: Hostaway, OwnerRez, Guesty, Hosthub, Smoobu. Hosthub uses the host’s own API key from Hosthub **Settings → API keys** (`providerKey: hosthub`, `secret`). See [hosthub-integration.md](./hosthub-integration.md) for Hosthub staging vs production bases and the property-manager flow.

| Method | Path | Notes |
|--------|------|--------|
| GET | `/v1/channels/providers` | Supported providers; `usesOAuth` tells the UI to redirect instead of asking for credentials |
| GET | `/v1/channels` | My connections |
| POST | `/v1/channels` | Connect (`providerKey`, credentials) |
| POST | `/v1/channels/{id}/enable` | Enable |
| POST | `/v1/channels/{id}/disable` | Disable |
| POST | `/v1/channels/{id}/sync` | Sync |
| GET | `/v1/channels/{id}/listings` | Channel listings |
| DELETE | `/v1/channels/{id}` | Disconnect |
| POST | `/v1/channels/ownerrez/oauth/start` | Begin OwnerRez OAuth; returns the consent URL. 400 unless an OAuth app is configured |
| GET | `/v1/channels/ownerrez/oauth/callback` | OwnerRez return leg (anonymous); redirects to the SPA |

OwnerRez has two connect flows and the deployment picks one: with
`Channels__OwnerRez__ClientId`/`ClientSecret` unset, hosts `POST /v1/channels` with their
account email and personal access token; once those settings are present, `usesOAuth` flips
to true, `POST /v1/channels` rejects OwnerRez credentials, and the OAuth endpoints take over.
Tokens issued under either flow keep working. Production leaves them unset, so hosts paste
their own API key; OwnerRez requires a paid plan to keep an OAuth app, which is why the
authorize flow is built but switched off.

`POST /v1/webhooks/ownerrez` is authenticated by the Basic credentials configured on the
OAuth app's Webhooks section (`Channels__OwnerRez__WebhookUsername`/`WebhookPassword`) and is
rejected outright while those are unset — which is the case today, since OwnerRez delivers
webhooks only to OAuth apps and the API-key flow gets none. It acts on booking creates,
updates and deletes
(reconciling remote cancellations) and on `application_authorization_revoked`, which
disconnects the connection. Other entity types are acknowledged without action — listing
content still refreshes on the scheduled sync, because OwnerRez allows only two seconds to
respond. Unfamiliar events return 2xx deliberately: OwnerRez retries failures ten times and
auto-disables apps that fail often.
| GET | `/channels/{provider}/listing/{externalId}` | Redirect / lookup |
| POST | `/v1/webhooks/hostaway` | Hostaway webhook |
| POST | `/v1/webhooks/ownerrez` | OwnerRez webhook (Basic auth; booking changes and authorization revocations) |

---

## Structured inquiry

### Deal-based (legacy-style)

`/v1/inquiries/{dealId}/...` — unlock, approve-unlock, lock, questions, answers, close, get.

### Session-based (preferred)

| Method | Path |
|--------|------|
| GET | `/v1/inquiry-sessions/mine` |
| GET | `/v1/inquiry-sessions/host` |
| GET | `/v1/inquiry-sessions/partner` |
| GET | `/v1/inquiry-sessions/{sessionId}` |
| POST | `/v1/inquiry-sessions/{sessionId}/questions` |
| POST | `/v1/inquiry-sessions/{sessionId}/answers` |
| POST | `/v1/inquiry-sessions/{sessionId}/offers` |
| POST | `/v1/inquiry-sessions/{sessionId}/offers/{offerId}/accept` |
| POST | `/v1/inquiry-sessions/{sessionId}/offers/{offerId}/counter` |
| POST | `/v1/inquiry-sessions/{sessionId}/offers/accepted/withdraw` |
| POST/DELETE | `/v1/inquiry-sessions/{sessionId}/partner` |
| POST | `/v1/listings/{listingId}/inquiry` |
| GET | `/v1/listings/{listingId}/inquiry/mine` |
| POST | `/v1/listings/{listingId}/inquiry/partner` |
| GET | `/v1/inquiries/predefined-questions` |

---

## Partners

| Method | Path | Notes |
|--------|------|--------|
| GET | `/v1/partners/discover` | Find partners |
| POST | `/v1/partners` | Register org |
| GET | `/v1/partners/me` | My org |
| GET | `/v1/partners/{id}` | Org detail |
| POST | `/v1/partners/{id}/verify` | Verify |
| GET/POST | `/v1/partners/{id}/members` | Members |
| GET/POST | `/v1/partners/{id}/referral-links` | Referral links |
| POST | `/v1/partners/{id}/referral-links/{linkId}/deactivate` | Deactivate link |
| GET/POST | `/v1/partners/{id}/reservations` | Reservations |
| POST | `/v1/partners/{id}/setup-intent` | Payment setup |
| GET | `/v1/partners/{id}/endorsed-members` | Endorsed guests |
| GET/POST | `/v1/partners/{id}/endorsements` | Endorsements |
| POST | `/v1/partners/{id}/endorsements/{endorsementId}/approve` | Approve |
| POST | `/v1/partners/{id}/endorsements/{endorsementId}/revoke` | Revoke |
| POST | `/v1/partners/{id}/invites` | Invite guest |
| POST | `/v1/referral/{code}/redeem` | Redeem referral |
| GET/POST | `/v1/me/partner-endorsements` | Tenant endorsements |

Admin: `/v1/admin/partners`, `.../pending`, `.../{id}/suspend`

---

## Evidence

Typical flow: create manifest → request upload URL (or direct upload) → complete → seal.

| Method | Path |
|--------|------|
| POST | `/v1/evidence/manifests` |
| GET | `/v1/evidence/manifests/{id}` |
| POST | `/v1/evidence/manifests/{id}/seal` |
| POST | `/v1/evidence/uploads/request-url` |
| POST | `/v1/evidence/uploads/{id}/complete` |
| GET | `/v1/evidence/uploads/{id}/scan` |
| GET | `/v1/evidence/uploads/{id}/download-url` |
| POST | `/v1/evidence/uploads/direct` | multipart |

Admin: scan queue + quarantine under `/v1/admin/evidence/...`

---

## Identity & verification

| Method | Path |
|--------|------|
| POST | `/v1/identity/kyc/start` |
| POST | `/v1/identity/kyc/complete` |
| GET | `/v1/identity/status` | `userId` required |
| POST/GET | `/v1/identity/kyc/manual/documents` |
| POST | `/v1/identity/kyc/manual/submit` |
| POST | `/v1/verification/background-check/consent` |
| POST | `/v1/verification/affiliation` |
| POST | `/v1/verification/fraud-flag` |
| GET | `/v1/verification/fraud-flags` |
| GET/POST | `/v1/risk/{tenantUserId}` (+ `/recalculate`) |

Admin KYC queue: `/v1/admin/identity/manual-queue/...`  
Webhook: `POST /v1/webhooks/kyc`

---

## Insurance

| Method | Path |
|--------|------|
| GET | `/v1/deals/{dealId}/insurance` |
| POST | `/v1/deals/{dealId}/insurance/verify` |
| POST | `/v1/deals/{dealId}/insurance/manual-proof` |
| POST | `/v1/webhooks/insurance/purchase` |

Admin unknown queue: `GET /v1/admin/insurance/unknown-queue`

---

## Arbitration

| Method | Path |
|--------|------|
| POST/GET | `/v1/arbitration/cases` | File / list by `status` |
| GET | `/v1/arbitration/cases/{caseId}` |
| POST | `/v1/arbitration/cases/{caseId}/filing-fee/checkout` |
| POST | `/v1/arbitration/cases/{caseId}/evidence` |
| POST | `/v1/arbitration/cases/{caseId}/evidence-complete` |
| POST | `/v1/arbitration/cases/{caseId}/assign` |
| POST | `/v1/arbitration/cases/{caseId}/begin-review` |
| POST | `/v1/arbitration/cases/{caseId}/decision` |
| PUT | `/v1/arbitration/cases/{caseId}/close` |
| POST | `/v1/arbitration/cases/{caseId}/appeal` |
| GET | `/v1/arbitrators/{userId}/cases` |

Admin: backlog, caseload, assign-auto under `/v1/admin/arbitration/...`

---

## Compliance, integrity, privacy

**Compliance** — record/list/resolve/dismiss/escalate violations; ledger by user or deal; deal monitoring under `/v1/deals/{dealId}/compliance/...`

**Integrity** — flags and restrictions (user + admin); collusion detect; restrict account.

**Privacy** — consent, export, deletion, legal holds, consent status for current user.

---

## Reviews

| Method | Path |
|--------|------|
| GET/POST | `/v1/deals/{dealId}/reviews` |
| GET | `/v1/users/{userId}/reviews` |
| GET | `/v1/users/{userId}/reputation` |
| GET | `/v1/listings/{listingId}/reviews` |
| GET/POST | `/v1/partners/organizations/{orgId}/reviews` |
| GET | `/v1/partners/organizations/{orgId}/reputation` |

---

## Notifications

| Method | Path |
|--------|------|
| GET | `/v1/notifications/all` |
| GET | `/v1/notifications/unread` |
| GET | `/v1/notifications/unread/count` |
| POST | `/v1/notifications/{notificationId}/read` |
| POST | `/v1/notifications/read-all` |
| GET/PUT | `/v1/notifications/preferences/{userId}` |
| GET | `/v1/notifications/history/{userId}` |

---

## Saved listings

| Method | Path |
|--------|------|
| POST/DELETE | `/v1/saved-listings/{listingId}` |
| GET | `/v1/saved-listings` |
| POST/GET | `/v1/saved-listings/collections` |
| GET | `/v1/saved-listings/collections/{collectionId}` |
| POST | `/v1/saved-listings/{listingId}/collections/{collectionId}` |
| DELETE | `/v1/saved-listings/{listingId}/collections` |

---

## Lease agreements

Placeholders, templates, versions, request-approval / approve / publish / deprecate, PDF by deal: `/v1/lease-agreements/...`  
Admin pending: `/v1/admin/lease-agreements/...`

---

## Truth surface

Create / confirm / reconfirm snapshots; verify; receipt; by deal: `/v1/truth-surface/...`

---

## Platform & content

| Method | Path |
|--------|------|
| GET | `/v1/platform/public-config` | Public config |
| GET | `/v1/admin/settings` | All settings |
| PUT | `/v1/admin/settings/{key}` | Update setting |
| GET | `/health` | Health check |
| GET | `/api/v1/blog` | Public blog list |
| GET | `/api/v1/blog/{slug}` | Post |
| GET | `/api/v1/blog/sitemap` | Sitemap |
| * | `/api/v1/admin/blog/...` | Admin blog CRUD |
| GET | `/api/v1/pages/{slug}` | SEO page |
| PUT | `/api/v1/admin/pages/{slug}` | Upsert SEO page |

Other admin: analytics, audit, compliance violations, integrity flags.

---

## Webhooks

| Method | Path | Notes |
|--------|------|--------|
| POST | `/v1/webhooks/stripe` | Stripe events |
| POST | `/v1/webhooks/kyc` | KYC provider |
| POST | `/v1/webhooks/hostaway` | Channel sync |
| POST | `/v1/webhooks/insurance/purchase` | Insurance purchase |

These are for provider callbacks, not normal app clients.

---

## Tips

- **UUIDs** — IDs in paths are UUIDs.  
- **Money** — amounts are usually **cents** (`*Cents` fields).  
- **Dates** — listing/application dates often use `date` (`YYYY-MM-DD`); many others use `date-time`.  
- **Enums** — OpenAPI often exposes enums as integers; prefer named values from the app/SDK when available.  
- **Pagination** — list endpoints usually take `page` + `pageSize` (or `skip`/`take` for partners).  
- **Full schema** — for exact request bodies, use Swagger UI or the OpenAPI `components.schemas` section.
