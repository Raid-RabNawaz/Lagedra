# Lagedra Release Notes — Friday 25 September 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Uncommitted working-tree changes on branch `dev` since commit `9778959` (“changes 21-09-2026”) / `RELEASE_NOTES_2026-09-18.md`. This document covers **that entire working tree**. Items already shipped in `9778959` and documented on 18 September (Stay Protection opt-out, no-lease listings, PMS price/calendar sync flags, marketplace place search, account readiness, multi-photo upload, Truvi prod / Checkr essential) are **not repeated** here.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release adds **Lagedra Partner Eviction Protection** for endorsed guests, **in-app lease signing** when a host requires it, and **host-controlled PMS monthly-vs-nightly rent**. OwnerRez import moves to the **HA-XML channel feed** (Connection ID `ora…`) instead of personal API tokens. Marketplace date search **keeps a listing visible** unless one stay covers the whole window; the booking calendar lets guests **check out on the morning a booked stay starts**. Admins can **preview KYC photos in the review dialog**, including HEIC phone uploads. Public contact copy uses the new company phone **+1 (213) 377-5272**.

---

## Highlights

### Lagedra Partner Eviction Protection

Separate from Truvi Stay Protection (property damage). This is a **guest-paid** product for guests with an **active partner endorsement**. It covers qualifying unpaid rent and eviction costs, up to a **$10,000** cap. Claims are filed by email to **info@lagedra.com**, not in the app.

| Item | Detail |
|------|--------|
| **Who pays** | Endorsed guest, once per booking. Fee is admin-configurable (`eviction_protection.fee_cents`, default **$50**). Set to **$0** to waive the fee; coverage still applies. |
| **Who does not** | Guests without an active endorsement never see the line. |
| **Quote / apply / checkout** | Line appears on listing quote, apply preview, checkout, billing, and application detail when included. |
| **Cancellation** | Full fee refunded **only when cancelled before the check-in date**. Refund is a separate Stripe refund from the platform balance (does not reverse the host transfer). |
| **Early move-out** | Ending a covered stay before scheduled check-out requires `documentedNonpayment`. `false` → standing **EndedNoClaim** (coverage ends, no claim). `true` → **ClaimReviewable**. On-time or late move-out does not set standing. |
| **Truth Surface** | Snapshot and agreement document include fee, cap, and the three coverage terms. |
| **Deal UI** | `DealEvictionProtectionCard` — Active / Ended / Claim reviewable. End-stay card asks the nonpayment question when coverage applies. |

**Default terms (guest-facing):**

1. If the booking is cancelled before check-in, the Eviction Protection fee is refunded.
2. If the tenant moves out early for a reason that is not a qualifying nonpayment or eviction event, coverage ends when the booking ends and no claim applies.
3. If the early move-out is connected to documented nonpayment and meets the qualifying-event requirements, the claim may still be reviewed under the Eviction Protection terms.

**API (extended):** listing quote, reservation preview, checkout, deal summary, deal application, and payment confirmation include `evictionProtectionFeeCents` / `evictionProtectionIncluded`; payment confirmation and deal summary add `evictionProtectionClaimStanding`.  
**Move-out body:** `POST /v1/deals/{dealId}/payment/begin-move-out` accepts `{ documentedNonpayment }` (required on first early move-out of a covered stay). Error `EvictionProtection.EarlyMoveOutReasonRequired`.

**Setting:** seeded on first admin load of Platform Fees & Settings. Editable under `/app/admin/settings` group **Eviction Protection**. A change applies to **new bookings only**.

**Migrations:**  
`20260923195724_AddEvictionProtectionFee` — fee + included on applications; fee on payment confirmations.  
`20260924183604_AddEvictionProtectionClaimStanding` — `EvictionProtectionClaimStanding` on payment confirmations (`varchar(32)`, nullable).  
**Scripts:** `tools/scripts/db-migrate-eviction-protection.ps1`, `tools/scripts/db-migrate-eviction-protection-claim-standing.ps1`

### In-app lease signing

Hosts can require **typed-name electronic signatures** after a booking is sealed. Off by default. Does **not** block checkout.

| Item | Detail |
|------|--------|
| **Listing flag** | `RequireLeaseSignatures` — wizard/edit checkbox **“Require host and tenant to sign”**, only when the lease source is the Lagedra template. Custom upload and no-lease listings never require in-app signing. |
| **Who can sign** | Deal host or tenant only. Each role signs once. Typed name ≥ 2 characters; must accept the electronic-signature confirmation. |
| **PDF** | Signature is bound to the lease PDF hash at sign time. After each signature the PDF is re-rendered so `{{host.signatureName}}` / `{{tenant.signatureName}}` and dates land on the stored document. |
| **Deal UI** | `DealLeaseSigningCard` — waiting / you signed / both signed; type-name form when `callerCanSign`. |

**API:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/lease-agreements/deals/{dealId}/signing` | Host or tenant | Status (`signingRequired`, `callerCanSign`, `bothSigned`, host/tenant typed names and timestamps). |
| `POST` | `/v1/lease-agreements/deals/{dealId}/sign` | Host or tenant | `{ typedName, accepted }`. Idempotent if already signed. |

**Errors:** `LeaseAgreement.DealNotFound`, `LeaseAgreement.Forbidden`, `LeaseAgreement.SigningNotRequired`, `LeaseAgreement.SignatureNotAccepted`, `LeaseAgreement.SignatureNameRequired`, `LeaseAgreement.PdfNotReady`.

**API (extended):** listing create/update/detail include `requireLeaseSignatures`.

**Migrations:**  
`20260921175427_AddListingRequireLeaseSignatures` — `listings.RequireLeaseSignatures` default **false**.  
`20260921175441_AddDealLeaseSignatures` — `lease_agreements.deal_lease_signatures` (unique `DealId` + `Role`).  
**Script:** `tools/scripts/db-migrate-lease-signatures.ps1`

### PMS monthly vs nightly rent

Connected PMS feeds often do not say whether the stored amount is monthly or nightly. Treating a monthly amount as nightly × 30 produced inflated rents (Hostaway Reno case).

| Item | Detail |
|------|--------|
| **Normalizer** | Unlabeled amounts are nightly × 30 unless that product exceeds **$40,000/month**, in which case the amount is treated as already monthly. |
| **Host choice** | Host sets **Monthly** or **Nightly** per imported listing. Choice is stored and reused on later syncs. Nightly = amount × 30. |
| **Prompt** | Channels page asks when a nightly reading would exceed $40,000/month. |
| **Correction** | Even without price-sync enabled, a stored rent that still looks like the old unlabeled ×30 / ×31 inflation is rewritten to the monthly reading. |

**API:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `PUT` | `/v1/channels/{connectionId}/listings/{mapId}/price-basis` | Member | `{ priceBasis: "Monthly" \| "Nightly" }`. |

**Errors:** `Channel.NotFound`, `Channel.ListingMapNotFound`, `Channel.PriceBasis.MissingAmount` (sync the listing first).

**UI:** `/app/channels` — per imported listing, Monthly / Nightly with the implied rent shown.

**Migration:** `20260922194708_AddChannelListingPriceBasis` — `channel_listing_maps.PmsPriceBasis` (`varchar(20)`, nullable), `PmsSourceAmountCents`.  
**Script:** `tools/scripts/db-migrate-channel-listing-price-basis.ps1`

### OwnerRez HA-XML channel import

New OwnerRez connections use the **read-only inquiry partnership** feed, not personal access tokens.

| Item | Detail |
|------|--------|
| **Connect** | Host pastes the OwnerRez **Connection ID** (`ora…`). Platform HA-XML username/key live in configuration, never on the host connection. |
| **Gateway** | Token connections still use API v2. Connections without a token use HA-XML (`OwnerRezChannelGateway`). |
| **Default** | `Channels:OwnerRez:OpenApiEnabled` = **false**. Token and OAuth connect stay in code but are not offered until this is turned on. |
| **Providers DTO** | `GET /v1/channels/providers` adds `connectMode`: `haxml` / `oauth` / `token` / `credentials`. |
| **Connect errors** | `Channel.HaXmlNotConfigured`, `Channel.InvalidAdvertiserId`, `Channel.OpenApiDisabled`, `Channel.OwnerRezRequiresOAuth`. |

**Ops:** `deploy/aws/set-staging-ownerrez-haxml-key.ps1` writes SSM `/lagedra/staging/ownerrez-haxml-key` (`-Apply` attaches `Channels__OwnerRez__HaXmlKey` and restarts staging API). Staging task defs include HA-XML env. Worker `appsettings.json` carries the same OwnerRez HA-XML block.

**Config:** `Channels:OwnerRez:HaXmlBaseUrl` (sandbox `https://faststage.ownerrez.com`; live `https://fast.ownerrez.com` once OwnerRez issues production credentials), `HaXmlUsername`, `HaXmlKey`, `HaXmlUserAgent`.

### Marketplace dates and booking calendar

| Item | Detail |
|------|--------|
| **Search** | A listing is hidden for `availableFrom`/`availableTo` only when **one stay covers the entire search window**. A booking in the middle of a long search no longer drops the listing. |
| **Calendar** | Occupied nights cannot be check-in. The **morning a booked stay starts** can still be checkout. A click that would cross a taken night starts a new check-in if that night is free. |

**Surfaces:** listing search, `DateRangeCalendar` / `DateRangeField`, booking panel.

### Admin KYC document preview

Admins reviewing the manual verification queue can **see ID and selfie images in the dialog**. Bytes stream through the authenticated API so the private KYC bucket is not opened to the browser.

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/admin/identity/manual-queue/{id}/documents/{documentType}` | Platform admin | `IdFront` / `IdBack` / `Selfie`. `Cache-Control: private, no-store`. |

**Client:** `ReviewDocumentImage` + `kycPreview.ts` — HEIC/HEIF phone uploads convert to JPEG via `heic2any` before display. Preview order: front, selfie, back.

**UI:** `/app/admin/manual-verification`.

### Company phone

Public and transactional contact numbers move from **+1 (213) 735-2362** to **+1 (213) 377-5272**.

| Surface | Update |
|---------|--------|
| Legal / SMS pages | About, Contact, Privacy, Terms, SMS opt-in (`companyInfo.ts`). |
| Email footer | `EmailFooter.ContactPhone`. |
| A2P HELP reply | `SmsProgram.HelpReply`. |

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/lease-agreements/deals/{dealId}/signing` | Host or tenant | Lease signature status. |
| `POST` | `/v1/lease-agreements/deals/{dealId}/sign` | Host or tenant | Typed-name electronic signature. |
| `PUT` | `/v1/channels/{id}/listings/{mapId}/price-basis` | Member | Set PMS monthly vs nightly. |
| `GET` | `/v1/admin/identity/manual-queue/{id}/documents/{documentType}` | Platform admin | Stream KYC image for review. |
| `POST` | `/v1/deals/{dealId}/payment/begin-move-out` | Party or admin | Body now accepts `documentedNonpayment` for Eviction Protection. |

*Extended:* listing create/update/detail (`requireLeaseSignatures`); listing quote (`evictionProtectionFeeCents`, `evictionProtectionIncluded`); reservation preview, checkout, deal summary, deal application, payment confirmation (same plus `evictionProtectionClaimStanding` where stored); `GET /v1/channels/providers` (`connectMode`); listing search (full-window availability); OwnerRez `POST /v1/channels` (HA-XML Connection ID).*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| Listing wizard / edit | **Require host and tenant to sign** (Lagedra template only). |
| Listing detail booking panel | Eviction Protection line and terms when the signed-in guest is endorsed. |
| Apply dialog | Eviction Protection on reservation preview. |
| `/app/checkout`, `/app/billing` | Eviction Protection fee line. |
| Application detail (page + dialog) | Eviction Protection line. |
| `/app/deals/:dealId` | Eviction Protection card; lease signing card; end-stay asks documented nonpayment when coverage applies. |
| `/app/channels` | Monthly / Nightly price-basis dialog and choices; OwnerRez connect collects `ora…` Connection ID. |
| `/listings`, calendar | Search keeps partial-overlap listings; calendar allows checkout on a booked stay’s start morning. |
| `/app/admin/settings` | Eviction Protection fee group. |
| `/app/admin/manual-verification` | In-dialog KYC image preview (HEIC converted). |
| Legal / SMS | New company phone. |
| Shared `Dialog` | Portal-based overlay (used by KYC and channel dialogs). |

**Dependency:** `heic2any` `^0.0.4` in `apps/web`.

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260921175427_AddListingRequireLeaseSignatures` | Listings | `RequireLeaseSignatures` default false. |
| `20260921175441_AddDealLeaseSignatures` | Lease | `deal_lease_signatures` (unique deal + role). |
| `20260922194708_AddChannelListingPriceBasis` | Channel | `PmsPriceBasis`, `PmsSourceAmountCents`. |
| `20260923195724_AddEvictionProtectionFee` | Billing | Eviction fee + included on applications; fee on payment confirmations. |
| `20260924183604_AddEvictionProtectionClaimStanding` | Billing | Claim standing on payment confirmations. |

**Apply (after 18 September migrations if not already applied):**

```powershell
pwsh tools/scripts/db-migrate-lease-signatures.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-channel-listing-price-basis.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-eviction-protection.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-eviction-protection-claim-standing.ps1 -SkipAdd
```

The Eviction Protection **fee setting** is not a migration. It is created the first time an admin opens Platform Fees & Settings (default `5000`).

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `eviction_protection.fee_cents` | Guest Eviction Protection fee. `0` waives the charge; coverage remains. |
| `Channels:OwnerRez:OpenApiEnabled` | `false` — HA-XML Connection ID only. |
| `Channels:OwnerRez:HaXmlBaseUrl` | HA-XML host (sandbox vs live). |
| `Channels:OwnerRez:HaXmlUsername` / `HaXmlKey` | Platform channel credentials. |
| `heic2any` | Admin KYC HEIC preview in the browser. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests (eviction fee / refund / early move-out, lease signing, price interpretation / rate normalizer, HA-XML parser, availability window, KYC preview helpers).
2. **Migrate** in the order above (lease signatures → price basis → eviction fee → claim standing).
3. **Configure** staging/prod OwnerRez HA-XML username and key; confirm `OpenApiEnabled` is false unless token/OAuth connect is intended.
4. **Open** `/app/admin/settings` once so `eviction_protection.fee_cents` is seeded (or set the desired amount).
5. **Deploy** API + worker, then web.
6. **Smoke tests — 25 September**
   - Endorsed guest: listing quote, apply, checkout, and Truth Surface show Eviction Protection (or “Included” when fee is $0). Non-endorsed guest: no line.
   - Cancel a covered booking **before** check-in → Eviction Protection fee refunds; cancel on/after check-in → fee kept.
   - Active covered deal, end stay early without a reason → API requires `documentedNonpayment`. Choose not qualifying → card shows **Ended**. Choose documented nonpayment → **Claim reviewable**.
   - Lagedra-template listing with **Require host and tenant to sign**: after seal, host and tenant each type a name; PDF shows both names; second sign is a no-op.
   - No-lease / custom-upload listing: signing card does not appear.
   - Channels: import a listing whose PMS amount × 30 would exceed $40,000 → prompt; choose Monthly vs Nightly; rent updates and is remembered on re-sync.
   - OwnerRez: connect with an `ora…` id (not an API token); listings and calendar pull from HA-XML.
   - Search Jan 1–Mar 20 on a listing booked only Jan 10–31 → listing still appears. Calendar: checkout allowed on the morning a booked stay starts; occupied nights cannot be check-in.
   - Admin manual verification: open a queue item; front/selfie/back images render (including a HEIC upload).
   - Legal/contact/SMS and a sample email footer show **+1 (213) 377-5272**.
7. **Regression** — Stay Protection opt-out, no-lease, PMS price/calendar sync toggles, place search, account readiness (18 September); trust tiers and Checkr screening (12 September).

---

## Known limitations

- **Uncommitted scope** — Eviction Protection, lease signing, price basis, OwnerRez HA-XML, search/calendar, KYC preview, and the phone change remain in the working tree; commit and pass CI before tagging.
- **Lease signing does not gate checkout** — parties can pay before both have signed.
- **Signing is Lagedra-template only** — host-uploaded PDFs and no-lease listings are out of scope.
- **Eviction claims are offline** — hosts email `info@lagedra.com`; there is no in-app claim form.
- **OwnerRez HA-XML is read-only** — listings, rates, and availability only; bookings are not created on the feed.
- **HA-XML sandbox vs live** — production needs a live username/key and `https://fast.ownerrez.com` before treating OwnerRez as a production channel.
- **PMS price-basis prompt** — only amounts that would exceed $40,000/month as nightly × 30 are flagged; ordinary nightly rates are converted without asking.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-09-25 | Eviction Protection, in-app lease signing, PMS price basis, OwnerRez HA-XML, search/calendar availability, admin KYC preview, company phone. |

---

*End of release notes.*
