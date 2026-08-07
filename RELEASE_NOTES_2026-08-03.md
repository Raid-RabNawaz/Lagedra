# Lagedra Release Notes — Sunday 3 August 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Commit `ea7e5d2` (“older changes”) plus the pending working tree on branch `dev`. Supersedes operational detail in `RELEASE_NOTES_2026-07-25.md` for net-new items only; prior July 25 and July 18 features shipped in `ea7e5d2` are summarized below with pointers to those documents.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This deployment completes the **July platform backlog** (pre-launch host surface, listing editors, PMS channels, SendGrid email, manual KYC, stay access, admin analytics, and related work in `ea7e5d2`) and adds **August channel, payout, and booking-hardening** improvements.

**New in August:**

- **OwnerRez OAuth** — Connect via consent flow, disconnect, webhooks, and daily token refresh (replaces pasted tokens when OAuth app is configured).
- **Smoobu PMS** — New channel provider (API key auth); UI respects pre-launch “coming soon” when applicable.
- **Host Stripe Express** — Express Dashboard login, account update links, and outstanding requirement visibility on payout setup.
- **Inquiry routing** — Pre-booking threads stay on listing routes; deal-linked threads open only from the deal.
- **Address gates** — Host cannot approve applications without a locked precise address; lease PDF and location editor enforce full street address.
- **API guide** — New `docs/api-guide.md` HTTP reference for integrators and ops.

---

## Highlights — August delta (since 25 July)

### Channel integration — OwnerRez OAuth & Smoobu

| Feature | Detail |
|---------|--------|
| **OwnerRez OAuth** | `POST /v1/channels/ownerrez/oauth/start` → consent URL; callback syncs connection; `DELETE /v1/channels/{id}` disconnects (credentials cleared, listings kept). |
| **OwnerRez webhooks** | `POST /v1/webhooks/ownerrez` — booking updates and token revocation (Basic auth). |
| **Token refresh** | `OwnerRezTokenRefreshJob` refreshes 30-day OAuth tokens daily. |
| **Provider metadata** | `GET /v1/channels/providers` returns `usesOAuth: true` for OwnerRez when OAuth app is configured. |
| **Credential guard** | `POST /v1/channels` rejects OwnerRez API-token connect when OAuth is configured. |
| **Smoobu** | New `SmoobuChannelProvider` (HMAC-signed API key); connect card on `/app/channels`. |
| **UI** | `OwnerRezReturnNotice`, `DisconnectChannelButton`, OAuth return query handling on `/app/channels`. |

**Migration:** `20260731192455_AddChannelOAuthTokens` — `EncryptedRefreshToken`, `TokenExpiresAt` on `channel_connections`.  
**Script:** `tools/scripts/db-migrate-channel-oauth-tokens.ps1`

### Host payouts — Stripe Connect Express

| Feature | Detail |
|---------|--------|
| **Express Dashboard** | `POST /v1/hosts/stripe/express-login` and `/v1/hosts/payouts/express-login` → Stripe Express login URL. |
| **Update bank/tax** | `POST /v1/hosts/stripe/update-link` and `/v1/hosts/payouts/update-link` → account update URL. |
| **Status** | Payout status includes `outstandingRequirements[]` (bank, ToS, tax, etc.). |
| **UI** | Redesigned `/app/payout-setup` with dashboard, update, and requirement callouts. |

### Inquiry thread routing

| Rule | Behavior |
|------|----------|
| **Pre-booking** | Open listing-scoped sessions use `/app/inquiry/:sessionId`; listing detail always offers “Ask the host a question”. |
| **Deal-linked** | After booking, conversation opens from `/app/deals/:dealId/inquiry` only; listing inquiry route redirects if `dealId` is set. |
| **Inboxes** | Host, tenant, and partner inquiry lists route rows to the correct surface via `inquiryThreadHref`. |

**API behavior:** `GET /v1/listings/{id}/inquiry/mine` returns only open, listing-scoped sessions (`DealId == null`).

### Locked precise address & lease PDF

- **Application approve** — `POST /v1/applications/{id}/approve` returns `Application.PreciseAddressRequired` when full street address is not locked.
- **Lease PDF** — Actionable errors when listing address fields required for PDF generation are missing.
- **Listing domain** — Activated listings may lock precise address; `ListingLocationEditor` surfaces lock state and reconciliation.

### Platform settings & Stripe resilience

- **Admin** — `stripe.platform_fee_price_id` editable on `/app/admin/settings` for host monthly protocol fee subscription.
- **SetupIntent** — Booking and partner setup intents recreate stale Stripe customer IDs after account migration; idempotency keys corrected.

### My Listings & documentation

- **`/app/listings`** — Inline View/Edit/Close/Delete grouping; full-width submit-for-review CTA.
- **`docs/api-guide.md`** — Practical API reference (auth, listings, applications, deals, payouts, channels, inquiries).

### Deployment configuration

- **SSM / secrets** — OwnerRez `client-id`, `client-secret`, `webhook-password` in `deploy/aws/04-secrets.sh`.
- **ECS task defs** — API and worker reference new OwnerRez OAuth and webhook env vars.
- **`.env.example`** — `Channels__OwnerRez__*` and Smoobu section.

---

## Included from commit `ea7e5d2` (prior release notes)

The following shipped in `ea7e5d2` but were documented in earlier release notes. See linked docs for full detail.

### From [RELEASE_NOTES_2026-07-25.md](RELEASE_NOTES_2026-07-25.md)

SendGrid email delivery, universal email footer, `WelcomeEmailComposer`, admin **Set password** on `/app/admin/users`, manual KYC upload + admin review, deal **stay access**, applications payment-failed triage, admin analytics v2, trust ledger expansion, deposit-return handshake UX, HTTP auth client fixes, migration `20260724213209_AddKycDocuments`.

### From [RELEASE_NOTES_2026-07-18.md](RELEASE_NOTES_2026-07-18.md) (also in `ea7e5d2`)

Founding-host pre-launch surface, listing location/photos/lease editors, booking attention banners, lease PDF download, Guesty/Hostaway/OwnerRez credential channels hub, partner identity display, notification deep links.

---

## Backend changes (August delta)

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/v1/channels/ownerrez/oauth/start` | Begin OwnerRez OAuth. |
| `GET` | `/v1/channels/ownerrez/oauth/callback` | OAuth callback (redirects to SPA). |
| `DELETE` | `/v1/channels/{id}` | Disconnect channel connection. |
| `POST` | `/v1/webhooks/ownerrez` | OwnerRez unified webhook. |
| `POST` | `/v1/hosts/stripe/express-login` | Stripe Express Dashboard URL. |
| `POST` | `/v1/hosts/stripe/update-link` | Stripe account update URL. |
| `POST` | `/v1/hosts/payouts/express-login` | Alias for Express login. |
| `POST` | `/v1/hosts/payouts/update-link` | Alias for update link. |

*Modified:* application approve (address gate), listing inquiry queries, payout status DTO, lease PDF error contracts, setup-intent customer resolution.*

---

## Frontend changes (August delta)

| Route | Change |
|-------|--------|
| `/app/channels` | OwnerRez OAuth connect, disconnect, return notices; Smoobu card. |
| `/app/payout-setup` | Express dashboard, update link, outstanding requirements. |
| `/app/inquiry/:sessionId` | Pre-booking only; redirect when deal-linked. |
| `/app/deals/:dealId/inquiry` | Deal-linked conversation entry. |
| `/app/inquiries`, `/app/my-inquiries`, partner inquiries | Correct thread routing. |
| `/app/listings` | My Listings action layout. |
| `/app/admin/settings` | Stripe platform fee price ID field. |
| Listing detail / applications | Address lock gates and inquiry CTA copy. |

---

## Database & migrations

| Migration | Context | Apply when |
|-----------|---------|------------|
| `20260731192455_AddChannelOAuthTokens` | Channel | Before OwnerRez OAuth in prod |
| `20260724213209_AddKycDocuments` | Identity | If not yet applied (`db-migrate-manual-kyc.ps1`) |

Run `tools/scripts/db-migrate-channel-oauth-tokens.ps1` after build.

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests.
2. **Secrets** — Push OwnerRez OAuth and webhook credentials to SSM; update ECS task defs.
3. **Migrate** Channel OAuth tokens migration; confirm KYC migration applied.
4. **Deploy** API + worker, then web.
5. **Smoke tests — August delta**
   - OwnerRez OAuth connect → callback → sync → disconnect.
   - OwnerRez webhook receives test payload.
   - Host opens Express Dashboard and update link from `/app/payout-setup`.
   - Pre-booking inquiry on listing; after deal, thread only on deal page.
   - Approve application blocked until address locked; lease PDF error is actionable.
   - Smoobu connect (non–pre-launch) or “coming soon” in pre-launch.
6. **Regression** — SendGrid email with footer, admin set-password, manual KYC queue, stay access card, channel credential connect (Hostaway/Guesty).

---

## Known limitations

- **Uncommitted work** — August channel OAuth and related files may still be uncommitted; commit and pass CI before tagging.
- **OwnerRez OAuth** — Requires `ClientId`/`ClientSecret` in environment; credential-token connect disabled when OAuth is configured.
- **Smoobu** — Hidden behind pre-launch “coming soon” when `prelaunch.enabled` is on.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-08-03 | August delta + `ea7e5d2` deployment bundle; references July 25/18 notes. |

---

*End of release notes.*
