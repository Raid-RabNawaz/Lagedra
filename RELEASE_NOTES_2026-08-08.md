# Lagedra Release Notes — Saturday 8 August 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Uncommitted working-tree changes on branch `dev` since `RELEASE_NOTES_2026-08-03.md` (`HEAD` remains `ea7e5d2`). This document covers **net-new work only**. Items documented on 3 August (OwnerRez OAuth, Smoobu, Stripe Express links, inquiry routing, address gates, API guide) and earlier July releases are **not repeated** here; see those notes if this deploy also ships that backlog for the first time.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

Today’s release focuses on **marketplace guest experience**, **frictionless sign-in**, **host listing productivity**, and **payout onboarding reliability**. Marketplace cards and listing photos get an Airbnb-style gallery and lightbox; guests can save listings via an inline **Sign in** dialog without leaving the page. Founding hosts in pre-launch gain access to **profile**, **verification**, and **payout setup**. Hosts can **bulk-import listings from Excel** as drafts. Stripe Connect onboarding avoids redirect loops and clarifies bank-requirement status. Ops apply a **platform fee price ID** settings migration so admin Fees & Settings and host subscriptions stay in sync.

---

## Highlights

### Marketplace photo & card experience

| Surface | Change |
|---------|--------|
| **Listing cards** | Fixed `4/5` image/info silhouette; compact stay labels; aligned meta and price rows on `/listings`. |
| **Home skeletons** | Matching `4/5` card placeholders on `MarketplaceHomePage`. |
| **All photos gallery** | New `PhotoGalleryModal` — full-screen grid from listing detail; open photo → lightbox. |
| **Photo lightbox** | Full natural-size scrollable image (no crop); nested-safe `bodyScrollLock` with gallery. |
| **Description** | Expandable listing description (clamped + Show more) on listing detail. |

### Guest save & sign-in dialog

- **SaveButton** — Visible to guests; opens `SignInDialog`, then saves after successful auth (user stays on the listing page).
- **SignInForm / SignInDialog** — Shared email/password sign-in (Google when not in pre-launch); reusable across marketplace and auth routes.
- **LoginPage** — Thin wrapper; supports safe `?redirect=/…` return paths.
- **Password visibility** — Show/hide toggle on sign-in and reset-password forms.

### Pre-launch host surface & navigation

- **Expanded host paths** during `prelaunch.enabled`: `/app/profile`, `/app/verification`, `/app/payout-setup` (and stripe-onboarding alias), in addition to listings and channels.
- **Profile header** — Profile link always available for pre-launch hosts (`AuthedHeaderActions`).
- **Admin / partner nav** — Admins see Main + admin sections only (no member Hosting/Bookings clutter). Partners get Browse + portal + account. Dedicated bottom tabs for admin and partner roles.

### Excel bulk listing import

| Item | Detail |
|------|--------|
| **Route** | `/app/listings/new` — `ImportFromExcelDialog` |
| **Flow** | Download template → fill rows → upload → create **Draft** listings (max 100 rows) |
| **Implementation** | `listingExcelImport.ts` (+ unit tests); lazy `exceljs` dependency |
| **API** | Reuses existing create-listing endpoint — **no new routes** |

### Host Stripe onboarding hardenings

| Fix | Detail |
|-----|--------|
| **Onboard loop** | If charges and payouts are already enabled, onboard returns status **without** issuing a new Account Link (avoids Stripe redirect loop). |
| **Bank status** | “Action needed” for bank only when Stripe requires `external_account` (not when restricted for ToS alone). |
| **Client refetch** | Payout status query always refetches on mount/focus after returning from Account Links. |
| **Staging utility** | `deploy/aws/_staging/clear-host-stripe.sql` — backup + wipe `identity.host_stripe_accounts` for reconnect testing. |
| **Web config** | Production Stripe publishable key updated in `apps/web/.env.production` for live checkout/onboarding. |

*Express Dashboard login and account update links remain as documented on 3 August.*

### Platform fee price ID — migration

| Item | Detail |
|------|--------|
| **Migration** | `20260804191953_AddStripePlatformFeePriceIdSetting` — seeds `stripe.platform_fee_price_id` in `platform.platform_settings`. |
| **Scripts** | `tools/scripts/db-migrate-stripe-platform-fee-price.ps1`, `tools/add-stripe-platform-fee-price-setting-migration.sh` |
| **Admin** | Setting editable under `/app/admin/settings` (Fees & settings) for host monthly protocol fee subscription. |

---

## Backend changes

| Area | Change |
|------|--------|
| **Host Stripe onboard** | Skip new Account Link when account already charges- and payouts-capable. |
| **Host Stripe status** | Bank requirement mapping refined from Stripe `requirements`. |
| **Platform settings** | Migration seeds empty `stripe.platform_fee_price_id` row. |

No new public HTTP endpoints for the Aug 8 guest/import UX; Excel import and save use existing listing APIs.

---

## Frontend changes

| Route / surface | Component | Purpose |
|-----------------|-----------|---------|
| `/listings`, cards | `ListingCard`, home skeletons | Marketplace card layout polish. |
| `/listings/:id` | `PhotoGalleryModal`, `PhotoLightbox`, expandable description | Gallery + lightbox + description UX. |
| Marketplace | `SaveButton` + `SignInDialog` | Guest save with inline sign-in. |
| `/auth/login`, dialogs | `SignInForm`, `SignInDialog` | Shared sign-in with redirect support. |
| `/auth/reset-password` | Password visibility toggle | Usability. |
| `/app/listings/new` | `ImportFromExcelDialog` | Bulk draft import from Excel. |
| `/app/payout-setup` | Status/refetch hardenings | Reliable Connect return path. |
| Pre-launch `/app/*` | `preLaunchAccess`, header, permissions | Profile / verification / payout + role nav. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260804191953_AddStripePlatformFeePriceIdSetting` | PlatformSettings | Seeds `stripe.platform_fee_price_id`. |

**Apply:**

```powershell
pwsh tools/scripts/db-migrate-stripe-platform-fee-price.ps1 -SkipAdd
```

*(Use without `-SkipAdd` only if generating the migration for the first time.)*

Also confirm Aug 3 migrations are applied if not already: Channel OAuth tokens, KYC documents.

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `exceljs` (web) | Excel template download and row parse for listing import. |
| `VITE_STRIPE_PUBLISHABLE_KEY` | Live publishable key in production web env. |
| `stripe.platform_fee_price_id` | Platform setting value must be set to a real Stripe `price_…` after migration. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web` (pnpm install for `exceljs` lockfile).
2. **Migrate** `AddStripePlatformFeePriceIdSetting`; set the price ID in admin settings.
3. **Deploy** API + worker (if Stripe onboard changes ship), then web.
4. **Smoke tests — 8 August**
   - Marketplace: card aspect ratio; listing “Show all photos” → gallery → lightbox scroll; description Show more.
   - Guest: heart Save → Sign in dialog → after login, listing saved without full page bounce.
   - Pre-launch host: open `/app/profile`, `/app/verification`, `/app/payout-setup`; other `/app` routes still limited.
   - Admin: sidebar has no Hosting/Bookings member clutter.
   - Excel: download template → upload rows → drafts appear under My Listings.
   - Payout: completed Connect host does not loop through Account Link; bank “Action needed” only when bank is required.
5. **Regression** — OwnerRez OAuth, inquiry deal vs listing routing, address-required approve, Express Dashboard / update link (Aug 3).

---

## Known limitations

- **Uncommitted scope** — Marketplace, SignIn dialog, Excel import, and related files are still in the working tree; commit and pass CI before tagging.
- **Platform fee price** — Migration only seeds an empty key; ops must paste the live Stripe Price ID after deploy.
- **Excel import** — Creates drafts only; hosts must still complete location/photos/review before publish.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-08-08 | Marketplace gallery/save, SignIn dialog, Excel import, pre-launch host paths, Stripe onboard hardenings, platform fee migration. |

---

*End of release notes.*
