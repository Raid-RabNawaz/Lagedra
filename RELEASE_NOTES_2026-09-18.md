# Lagedra Release Notes — Friday 18 September 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Working-tree changes on branch `dev` since commit `2708cbd` (“Commit 12-09-2026”) / `RELEASE_NOTES_2026-09-12.md`. This document covers **net-new work only**. Items documented on 12 September (trust tiers v2, Checkr application screening, stay-protection deal card / damage-claim deep link, lease specimen preview, admin queue dialogs, inquiry guest card, branded email layout, stale-chunk recovery) are **not repeated** here; see that note if this deploy also ships that backlog for the first time.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release gives hosts **per-listing Stay Protection opt-out**, a **“no lease” listing option**, and **selective PMS price/calendar sync**. Guests get **geographic marketplace search** (place typeahead, map radius, URL deep links). Members see an **account readiness** checklist on profile and dashboard. Hosts can **upload multiple listing photos** in one batch. Property managers can **submit listings for review without a named homeowner**. Truvi is pointed at the **production** Screen & Protect host in prod; Checkr defaults to the **essential** package. Deal-activated email copy no longer promises a lease attachment when the listing has no lease.

---

## Highlights

### Stay Protection — per-listing opt-out

**Sep 5/12:** Stay Protection (Truvi) was always quoted when the API was available.

**This release:** Hosts can turn Stay Protection **off per listing**. Existing listings stay **on** (`StayProtectionEnabled` default **true**).

| Area | Behavior |
|------|----------|
| **Quote / apply / checkout** | Insurance fee omitted when listing opted out. |
| **Truvi screening** | Not requested on Truth Surface confirm when opted out. |
| **Deal activation** | No stay-protection policy record when opted out. |
| **Deal UI** | Sep 12 stay-protection card and header badge show only when snapshotted fee &gt; 0 or insurance API has data. |
| **Import** | Excel/XML column `stayProtection` maps to `stayProtectionEnabled`. |

**Surfaces:** listing wizard/edit checkbox **“Include Stay Protection (Truvi)”**; public and host listing detail badge when enabled.

**API (extended):** listing create/update/detail include `stayProtectionEnabled`; `GET /v1/deals/mine` adds `insuranceFeeCents`.

**Migration:** `20260915200700_AddListingStayProtectionEnabled` — `listings.StayProtectionEnabled` NOT NULL default true.  
**Script:** `tools/scripts/db-migrate-stay-protection-opt-in.ps1`

### No-lease listings

| Item | Detail |
|------|--------|
| **Source** | `LeaseAgreementSource.None` — third choice besides Lagedra template and host upload. |
| **PDF / preview** | Deal PDF and listing preview return **`LeaseAgreement.NotUsed`** (404) when source is `None`. Truth Surface confirm logs skip, not failure. |
| **Snapshot** | Omits lease template fields; records `leaseAgreementSource` in jurisdiction block. |
| **Email** | `deal_activated` placeholders so no-lease bookings do not promise an attachment; missing PDF logged at Information. |
| **UI** | Wizard/edit radio **“No lease agreement”** (broker clause hidden); listing detail lease section hidden; deal/truth-surface download hidden; admin review filter **“No lease”**. |
| **PM submit** | Property-manager listings may submit for review **without** a named homeowner on file. |

**API (extended):** `GET /v1/deals/mine` includes `leaseAgreementSource`; admin review list includes `leaseAgreementSource`.

### PMS price & calendar sync flags

| Item | Detail |
|------|--------|
| **API** | `PATCH /v1/channels/{connectionId}/listings/{mapId}` — `{ syncPriceFromPms, syncCalendarFromPms }`. Missing map → `Channel.ListingMapNotFound` (404). |
| **UI** | `/app/channels` — per imported listing: **Price from PMS** / **Calendar from PMS** (disabled until imported). |
| **Calendar job** | `ChannelAvailabilitySyncJob` only maps with `SyncCalendarFromPms`; projects PMS calendar onto **host blocked dates** (deal blocks preserved). |
| **Price import** | When `SyncPriceFromPms`, re-import overwrites monthly rent on draft/denied and published/activated (price history recorded). |

**Migration:** `20260917163337_AddChannelListingSyncFlags` — `channel_listing_maps.SyncPriceFromPms`, `SyncCalendarFromPms` default **false**.  
**Script:** `tools/scripts/db-migrate-channel-listing-sync-flags.ps1`

### Marketplace place search

| Item | Detail |
|------|--------|
| **Typeahead** | `PlaceSearchField` — Nominatim + CA-biased popular destinations (`placeSearch.ts`, `popularDestinations.ts`). |
| **Search page** | Geocode on submit; listing query waits until geocode finishes. |
| **URL state** | Deep-link `keyword`, `availableFrom`/`availableTo`, `sortBy`, `latitude`, `longitude`, `radiusKm`. |
| **Hero** | Marketplace home “where” uses the same place field. |
| **Map** | First `moveend`/`zoomend` ignored (avoids over-filter on open); `flyTo` can `fitBounds` from geocoded place. |
| **Keyword** | Server `SearchKeywordTokenizer` — AND tokens; also matches city / state / ZIP on precise address. |

**Routes:** `/listings`, `/listings/search` (same listing search API).

### Account readiness

| Item | Detail |
|------|--------|
| **Card** | `AccountReadinessCard` — identity (`/app/verification#kyc`), host payouts (`/app/payout-setup`), eligibility. Does **not** include Checkr (host-initiated on applications). |
| **Profile** | Replaces hosting-status card; host vs guest from mode store; payout shortcut only in host mode. |
| **Dashboard** | Embedded on hosting and traveling dashboards. |
| **Stripe** | `useHostStripeStatus({ enabled })` — no payout fetch on guest profile. |

### Listing photos & signup polish

- **`ListingPhotosEditor`** — multi-select image upload with batch progress (`uploadListingPhotoFiles.ts`); video remains a separate mutation.
- **Join / register** — city placeholder **Los Angeles, CA**.

### Production configuration

| Item | Change |
|------|--------|
| **Truvi** | Production base URL on prod ECS; `deploy/aws/set-prod-truvi-key.ps1` (SSM + optional apply). |
| **Checkr** | Default `Checkr:PackageSlug` = `essential`; key scripts also set `Checkr__PackageSlug`. |

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `PATCH` | `/v1/channels/{id}/listings/{mapId}` | Member | Set PMS price/calendar sync flags. |

*Extended:* listing create/update/detail (`stayProtectionEnabled`, `leaseAgreementSource` including `None`); listing quote (no stay-protection fee when opted out); `GET /v1/deals/mine` (`insuranceFeeCents`, `leaseAgreementSource`); lease preview / deal PDF (`LeaseAgreement.NotUsed`); listing search (tokenized keyword + address fields).*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| Listing wizard / edit | Stay Protection checkbox; no-lease radio; multi-photo upload. |
| `/listings/:id` | Stay protection badge; hide lease section when `None`. |
| `/app/deals/:dealId` | Gate stay-protection card and lease download. |
| `/app/channels` | Per-listing PMS price/calendar toggles. |
| `/listings`, `/listings/search` | Place typeahead, geo URL params, map fit. |
| `/app/profile`, `/app` | Account readiness card. |
| `/app/admin/listing-review` | Filter and callout for no-lease listings. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260915200700_AddListingStayProtectionEnabled` | Listings | `StayProtectionEnabled` default true. |
| `20260917163337_AddChannelListingSyncFlags` | Channel | Per-map `SyncPriceFromPms`, `SyncCalendarFromPms` default false. |

**Apply (after Sep 12 screening/trust migrations if not already applied):**

```powershell
pwsh tools/scripts/db-migrate-stay-protection-opt-in.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-channel-listing-sync-flags.ps1 -SkipAdd
```

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `Insurance__Truvi__BaseUrl` | Production Screen & Protect host in prod. |
| `Checkr__PackageSlug` | Default `essential`. |
| Nominatim / maps | Client place search; respect rate limits and attribution. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests (stay-protection opt-in, keyword tokenizer, channel sync flags, lease `None`).
2. **Migrate** stay-protection column, then channel listing sync flags.
3. **Configure** prod Truvi key/URL and Checkr package slug.
4. **Deploy** API + worker, then web.
5. **Smoke tests — 18 September**
   - Create listing with Stay Protection **off** → quote has no stay-protection line; Truth Surface confirm does not call Truvi; deal has no protection card.
   - Create listing with **No lease** → listing detail hides lease; deal has no download; deal-activated email does not promise a PDF.
   - PM listing submits for review without homeowner email.
   - Channels: import a listing; enable price sync → rent updates; enable calendar sync → host blocks update (deal blocks unchanged).
   - Marketplace: type “Los Angeles” → suggestions; submit → map fits; URL has `latitude`/`longitude`/`radiusKm`; keyword “venice, ca” matches address fields.
   - Multi-select several photos on listing edit; all attach.
   - Profile/dashboard: readiness card links to KYC and (host mode) payouts.
6. **Regression** — Trust tiers, Checkr screening checkout/report, stay-protection card when opted **in**, specimen lease, admin queues, inquiry guest card (12 September).

---

## Known limitations

- **Uncommitted scope** — Stay-protection opt-in, no-lease, channel sync flags, and place search remain in the working tree; commit and pass CI before tagging.
- **PMS sync defaults** — Price and calendar sync are **off** until the host enables them per listing map.
- **Stay Protection default** — Existing listings remain **enabled**; hosts must uncheck to opt out.
- **Nominatim** — Public geocoder; production should monitor rate limits and user-agent policy.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-09-18 | Stay Protection opt-out, no-lease listings, PMS sync flags, place search, account readiness, multi-photo upload, Truvi prod / Checkr essential. |

---

*End of release notes.*
