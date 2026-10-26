# Lagedra Release Notes — Saturday 24 October 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Uncommitted working-tree changes on branch `dev` since commit `258a063` (“changes for 28-09-2026”) / `RELEASE_NOTES_2026-09-25.md`. This document covers **that entire working tree**. Items already shipped in `258a063` and documented on 25 September (Partner Eviction Protection, in-app lease signing, PMS monthly-vs-nightly basis, OwnerRez HA-XML, search/calendar availability, admin KYC image preview, company phone) are **not repeated** here.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

Signup is now **one personal account** for staying and listing. Guests unlock hosting with **Become a host** and choose **individual** or **property-management company**. Companies verify with a **business registration** (no personal ID). Airbnb URL import reads the live **deferred-state** page, falls back through **ScrapingAnt**, and caps the gallery at **20 photos**. Marketplace search understands **US ZIP and Canadian postal codes**. Google Sign-In uses the new client ID and a visible Google button. URL import is rate-limited to **20 per hour / 150 per day**. OwnerRez connect defaults back to the **public API** (OAuth/token) unless HA-XML is explicitly selected.

---

## Highlights

### Guest-first accounts and Become a host

New personal signups are **members**. Hosting is off until the member completes **Become a host**. Accounts that joined through the old host door (`SignupType = Host`) keep hosting after migrate.

| Item | Detail |
|------|--------|
| **Signup** | Join door **“I want to stay or list a home”** creates `signupType: Member`. Copy covers browse now / host later. |
| **Unlock** | `/app/become-a-host` — city, country, bio (≥ 40 characters), optional state / languages / occupation, and host kind. Company kind requires legal company name. |
| **Mode switch** | Travelling / Hosting appears only when `hostingEnabled` is true. A stored “host” mode is ignored until then. Header CTA **Become a host** when hosting is still off. |
| **Pre-launch** | Limited host surface uses `hostingEnabled`, not “signed up as Host”. Founding hosts and members who enabled hosting keep listings, PMS, profile, phone, and payouts. |
| **Welcome email** | Personal accounts share one verification template that covers staying and hosting. Partners keep their own copy. |

**API:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/auth/me/hosting` | Member | Enable hosting. Body: city, country, bio, `hostKind`, optional state / languages / occupation / `companyName`. |

**Errors:** `Auth.Hosting.InvalidKind`, `Auth.Hosting.LocationRequired`, `Auth.Hosting.BioRequired`, `Auth.Hosting.CompanyNameRequired`, `Auth.Hosting.MembersOnly`.

**API (extended):** `GET /v1/auth/me` and public profile include `hostKind` and `hostingEnabled`.

**Migrations:**  
`20261023193516_AddHostKindToUsers` — `auth.AspNetUsers.HostKind` default **Individual**.  
`20261023202429_AddHostingEnabled` — `HostingEnabled` default **false**; existing `SignupType = Host` rows set **true**.  
**Scripts:** `tools/scripts/db-migrate-host-kind.ps1`, `tools/scripts/db-migrate-hosting-enabled.ps1`

### Property-management verification

Hosts choose how they verify. Individuals still upload government ID + selfie. Companies upload a **business registration** (PDF or image) and optional **proof of authority**. They are never asked for a personal ID.

| Item | Detail |
|------|--------|
| **Kinds** | `Individual` / `PropertyManagementCompany` (`HostKind`). |
| **Company fields** | Legal business name, registration/license number, jurisdiction (state or country). |
| **Switch** | Changing kind drops documents that belong to the other path. Locked after verification is submitted (`Identity.HostKind.Locked`). |
| **Surfaces** | `/app/verification` — kind picker + `BusinessVerificationForm`. Account readiness, profile health, application cards, public profile, and listing review use **Business** / **Business verified** copy for companies. |
| **Admin queue** | Manual verification preview order: registration, authority, then ID / selfie / back. PDFs accepted for business documents. |

**API:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `PUT` | `/v1/identity/host-kind` | Member | `{ hostKind }`. Creates a profile if needed. |

**Upload / submit (extended):** `POST /v1/identity/kyc/manual/documents` accepts `BusinessRegistration` and `ProofOfAuthority` (PDF allowed). `POST /v1/identity/kyc/manual/submit` accepts legal name, registration number, jurisdiction. `GET /v1/identity/status` includes host kind and business fields.

**Errors:** `Identity.Kyc.PersonalIdNotRequired`, `Identity.Kyc.BusinessDocumentNotAllowed`, `Identity.Kyc.MissingBusinessName`, `Identity.Kyc.MissingRegistrationNumber`, `Identity.Kyc.MissingJurisdiction`, `Identity.Kyc.MissingBusinessRegistration`, `Identity.HostKind.Invalid`, `Identity.HostKind.Locked`.

**Migration:** `20261023193452_AddHostVerificationKind` — `identity_profiles.HostKind` (default Individual), `LegalBusinessName`, `BusinessRegistrationNumber`, `BusinessJurisdiction`. Existing profiles stay individuals.

### Airbnb URL import

Import from an Airbnb URL now reads the server-rendered **`data-deferred-state`** GraphQL blob (name, long description, guests/beds/baths, amenities, gallery, house rules). Open Graph / JSON-LD remain the fallback. Pre-2026 shapes are still recognized.

| Item | Detail |
|------|--------|
| **Photos** | Gallery taken only from listing photo containers (not review icons / avatars). Cap **20** photos. Hosts can add more in the editor. |
| **Thumbs** | `PhotoThumbBox` shows imported and uploaded photos as squares. Airbnb CDN URLs request `im_w=480` for display only. |
| **Quality gate** | `ListingPageQuality` rejects empty bodies, bot walls (Cloudflare, Akamai, PerimeterX, DataDome, Imperva), and Airbnb shells that lack deferred-state and JSON-LD. |
| **Fetcher** | `FallbackListingImportClient` — when a ScrapingAnt key is set, default **Primary**: ScrapingAnt first, direct HTTP if the page is unusable or the API fails. **Fallback** mode spends credits only when the direct fetch fails. |
| **Rate limit** | `POST /v1/listings/import-from-url` — **20/hour** and **150/day** per user (429). Wizard stays usable; UI shows the limit message. |
| **Config** | `ListingImport:ScrapingAnt:Mode` (`Primary` \| `Fallback`), residential proxy, JS browser, 60s timeout. |

**UI:** import-from-URL panel and import dialog surface the 429 copy.

### Marketplace ZIP and map search

| Item | Detail |
|------|--------|
| **Postal** | `SearchKeywordTokenizer.NormalizePostalCode` — US ZIP / ZIP+4 (first 5) and Canadian FSA+LDU. Query param `postalCode`. |
| **Match** | A ZIP search matches the stored code **or** the map radius/box, so offset approx pins still appear. |
| **Typeahead** | Nominatim postal-only queries use the `postalcode` parameter (US vs CA country codes). |
| **Map** | Place search suspends bounds reporting so the default viewport cannot replace the chosen city. Wide result sets stay at city zoom instead of fitting the whole country. |

**Routes:** `/listings`, `/listings/search`, hero “where” field.

### Google Sign-In

| Item | Detail |
|------|--------|
| **Client ID** | Production / API / Docker / deploy docs use `1074067576483-7a9u94qhlvk79s1bce60hi11d36q0eal.apps.googleusercontent.com`. |
| **Button** | Native Google button stays visible (Chrome ignores clicks on a fully transparent overlay). Width tracks the container (200–400px). |

Also set `ExternalAuth:Google:ClientId` on the API and `VITE_GOOGLE_CLIENT_ID` in `apps/web/.env.production`.

### OwnerRez connect default

`Channels:OwnerRez:OpenApiEnabled` defaults to **true** again. New connections use OAuth when an app is configured, otherwise a personal access token. Set the flag **false** to collect an `ora…` HA-XML Connection ID. Existing token connections stay on API v2; advertiser-id connections stay on HA-XML.

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/auth/me/hosting` | Member | Unlock hosting and set host kind. |
| `PUT` | `/v1/identity/host-kind` | Member | Individual vs property-management company. |

*Extended:* `GET /v1/auth/me` and public profile (`hostKind`, `hostingEnabled`); identity status / manual KYC upload / submit (business documents and fields); listing search (`postalCode` + ZIP-or-geo match); listing import-from-URL (Airbnb deferred-state, quality gate, 20/150 rate limit); admin listing review (`hostKind`; cover URL + photo count only, no full photo graph); `GET /v1/channels/providers` connect-mode copy when Open API is on.*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| `/join` | One personal door: stay now, host later. |
| `/app/become-a-host` | New form to unlock hosting. |
| Header / mode switch | **Become a host** until `hostingEnabled`; switch hidden until then. |
| `/app/verification` | Host-kind picker; business registration form. |
| Profile / dashboard / applications | Business vs ID labels; readiness copy for companies. |
| `/app/admin/manual-verification` | Business docs first; PDF preview. |
| `/app/admin/listing-review` | Host kind; queue no longer loads every Hostaway photo. |
| Listing wizard / photos | Square thumbs; Airbnb display resize. |
| Import from URL | 429 limit message. |
| `/listings`, `/listings/search` | ZIP/postal typeahead and geo-or-code match; map fly/fit. |
| Sign-in / join | Visible Google button; new client ID. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20261023193452_AddHostVerificationKind` | Identity | Host kind + business fields on profiles. |
| `20261023193516_AddHostKindToUsers` | Auth | `HostKind` on users, default Individual. |
| `20261023202429_AddHostingEnabled` | Auth | `HostingEnabled`; backfill `SignupType = Host`. |

**Apply (after 25 September migrations if not already applied):**

```powershell
pwsh tools/scripts/db-migrate-host-kind.ps1 -SkipAdd
pwsh tools/scripts/db-migrate-hosting-enabled.ps1 -SkipAdd
```

Apply **host-kind** first so both identity and auth columns exist, then **hosting-enabled**.

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `ExternalAuth:Google:ClientId` / `VITE_GOOGLE_CLIENT_ID` | New Google OAuth client. |
| `Channels:OwnerRez:OpenApiEnabled` | `true` — OAuth/token connect. `false` — HA-XML Connection ID. |
| `ListingImport:ScrapingAnt:ApiKey` | Optional. Empty = direct fetch only. |
| `ListingImport:ScrapingAnt:Mode` | `Primary` (default) or `Fallback`. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`; run unit tests (host verification rules, hosting enabled / host kind, keyword/postal tokenizer, Airbnb extractor, page quality, fetcher fallback).
2. **Migrate** host-kind, then hosting-enabled.
3. **Configure** the new Google client ID on API and web; whitelist production origins on the Google Cloud client.
4. **Decide** OwnerRez: leave Open API on, or set `OpenApiEnabled=false` for HA-XML-only connect.
5. **Optional** set ScrapingAnt key/mode for Airbnb import.
6. **Deploy** API + worker, then web (web must be rebuilt so the Google client ID is baked in).
7. **Smoke tests — 24 October**
   - New personal signup: no hosting switch; header **Become a host**; complete the form (individual) → switch appears; dashboard in host mode.
   - Become a host as a **company** → verification asks for registration + legal name / number / jurisdiction, not ID/selfie. Submit; admin queue shows the PDFs.
   - Switch kind before submit → opposite documents disappear. After submit, kind is locked.
   - Legacy `SignupType = Host` account still has the travelling / hosting switch after migrate.
   - Import an Airbnb URL → title, description, amenities, house rules, and ≤ 20 photos. Import 21 times in an hour → 429 with the limit message.
   - Search `90210` or a Canadian postal code → listings match by ZIP or map area; map stays on the place, not the continent.
   - Google Sign-In on join and login opens the account chooser (button is visible).
   - OwnerRez connect: with Open API on, OAuth or token form appears; with the flag off, `ora…` field appears.
   - Admin listing review still loads for a Hostaway listing with hundreds of photos.
8. **Regression** — Eviction Protection, lease signing, PMS price basis, Stay Protection opt-out, no-lease, place search, KYC ID preview (25 / 18 September).

---

## Known limitations

- **Uncommitted scope** — Become a host, company verification, Airbnb import, ZIP search, Google client ID, and the Open API default remain in the working tree; commit and pass CI before tagging.
- **Hosting does not change role** — members stay `Member`; only `hostingEnabled` and `hostKind` change.
- **Company KYC is manual** — Persona/ID flows stay on the individual path.
- **Airbnb cancellation policy** is usually missing (Airbnb resolves it after dates are chosen).
- **Import photo cap** is 20 from Airbnb; additional photos are manual.
- **ScrapingAnt** is optional; without a key, bot-walled Airbnb pages may still fail.
- **Ops one-off** — `tools/scripts/onefinebnb-monthly-prices.sql` corrects inflated ×30 rents for one production host after a price-basis pass that lacked `PmsSourceAmountCents`. Not part of the product ship; run only against the intended host after review.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-10-24 | Guest-first Become a host, company verification, Airbnb import + ScrapingAnt fallback, ZIP search, Google client ID, OwnerRez Open API default, import rate limits. |

---

*End of release notes.*
