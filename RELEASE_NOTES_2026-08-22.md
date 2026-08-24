# Lagedra Release Notes — Saturday 22 August 2026

**Document type:** Product & engineering release notes  
**Audience:** Engineering, operations, product, and customer-facing stakeholders  
**Scope baseline:** Working-tree changes on branch `dev` (HEAD `947b0d7`) that are **net-new since** `RELEASE_NOTES_2026-08-15.md`. Items documented on 15 August (Excel/XML import, rent check-ins, partner email invite/remove, Guesty pre-launch, DatePicker rollout, KYC multipart fix, and related ops work) are **not repeated** here; see that note if this deploy also ships that backlog for the first time.

**Release tag / branch:** `dev` → production (API gateway, worker, web client)

---

## Executive summary

This release adds **Hosthub PMS integration**, **home-owner tenancy consent** for property-manager listings, and **listing ownership / provenance** controls. Hosts connect **Hosthub** on `/app/channels` to pull listings and sync bookings. When a listing is managed on behalf of a homeowner, booking applications require **owner consent** (in-app or one-tap email) before the host can approve. Listing create/edit captures **Owner vs Property Manager**, homeowner lookup, optional **broker clause**, and **how the listing was added** (manual, URL, Excel, XML, channel). Admin analytics gains **CSV download** and an **Added via** column. Profile update and phone verification enforce **E.164** and **minimum age 18**.

---

## Highlights

### Hosthub channel integration

| Item | Detail |
|------|--------|
| **Provider** | `HosthubChannelProvider` (`providerKey: hosthub`) — pull listings/availability, push bookings, pull booking updates using host API key. |
| **UI** | Connect / sync / disconnect cards on `/app/channels`; hosting dashboard PMS copy updated. |
| **Config** | `Channels:Hosthub` (`BaseUrl` default `https://app.hosthub.com`, optional `SourceId`, `UserAgent`); ECS / `.env.example` wired. |
| **Docs** | `docs/hosthub-integration.md`; API guide lists Hosthub with other PMS providers. |

### Owner tenancy consent (PM-managed listings)

When a listing is managed as **Property Manager** with a linked homeowner, applications require owner consent before host approve.

| Surface | Purpose |
|---------|---------|
| `/owner/consent` | Anonymous one-tap consent / decline from email token (`OwnerConsentPage`). |
| `/app/owner-consents` | Signed-in homeowner pending inbox (`OwnerConsentsPage`). |
| Application card / detail | `OwnerConsentPanel`, `ConsentTickButton`; `isAwaitingOwnerConsent` blocks approve until consent. |

**APIs:**

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `GET` | `/v1/applications/owner-pending` | Homeowner | Pending applications needing consent. |
| `POST` | `/v1/applications/{id}/owner-consent` | Homeowner | Give consent (`consentGiven`, `consentVersion?`). |
| `POST` | `/v1/applications/{id}/owner-decline` | Homeowner | Decline tenancy. |
| `POST` | `/v1/actions/consent-owner-tenancy` | Anonymous (token) | One-tap consent from email. |
| `POST` | `/v1/actions/decline-owner-tenancy` | Anonymous (token) | One-tap decline from email. |

**Domain:** consent version `owner-tenancy-consent-v1`; events `OwnerTenancyConsentGiven` / `OwnerTenancyConsentDeclined`; approve returns `Application.OwnerConsentRequired` when missing. Notifications on request / given / declined. Lease PDF placeholders include owner name and consent metadata; broker fields when `IncludeBrokerClause` is set.

**Migration:** `20260817211408_AddOwnerTenancyConsentToDealApplication` — consent columns + `HomeOwnerUserId` on `deal_applications`.  
**Script:** `tools/scripts/add-owner-tenancy-consent-migration.ps1`

### Listing ownership, broker clause & submit gates

| Item | Detail |
|------|--------|
| **UI** | `ListingOwnershipFields` on create/edit wizard — Owner vs Property Manager; email lookup for homeowner; optional include broker clause. |
| **Lookup** | `POST /v1/listings/home-owner-lookup` — `{ email }` → user id / display name. |
| **Model** | `ManagerRole`, `HomeOwnerUserId`, `IncludeBrokerClause` on listings; submit-for-review requires homeowner when role is Property Manager. |
| **Editability** | Live edit allowed for `Draft`, `Denied`, `Published`, `Activated`; `InReview` / `Closed` frozen (`listingSubmitGates` + `ListingManagementGuard`). |
| **Payout gate** | Temporary: submit-for-review does **not** require payout setup (`REQUIRE_PAYOUT_SETUP_TO_SUBMIT_FOR_REVIEW = false`); accept still requires payouts ready. |

**Migration:** `20260817203729_AddListingManagementAndBrokerClause`  
**Script:** `tools/scripts/add-listing-management-migration.ps1`

### Listing provenance & admin CSV reports

| Item | Detail |
|------|--------|
| **Added via** | `AddedVia` / `AddedViaDetail` on listings (Manual, URL, Excel, XML, channel labels including Hosthub). |
| **Admin UI** | Listing analytics sortable **Added via** column; **Download report** CSV on platform analytics and listing analytics (`analyticsReports.ts`, `csv.ts`). |
| **Server** | `GetListingAnalyticsQuery` + `ListingAddedViaFormatter`. |

**Migration:** `20260819180010_AddListingAddedVia`  
**Script:** `tools/scripts/db-migrate-listing-added-via.ps1`

### Auth — phone & age enforcement

| Command / path | Change |
|----------------|--------|
| `UpdateProfileCommand` | Reject non–E.164 phone (`Auth.PhoneInvalid`); reject under 18 (`Auth.Underage`). |
| `SendPhoneVerificationCommand` | Normalize to E.164 or return `Auth.PhoneInvalid` (avoids SMS 500). |

*(Aug 15 already covered register-time best-effort E.164 and frontend DOB 18+; this release hardens profile update and phone OTP send.)*

---

## Backend changes

### New or materially updated HTTP endpoints

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/v1/listings/home-owner-lookup` | Member | Resolve homeowner account by email. |
| `GET` | `/v1/applications/owner-pending` | Authenticated homeowner | Pending owner-consent applications. |
| `POST` | `/v1/applications/{id}/owner-consent` | Homeowner | Consent to tenancy. |
| `POST` | `/v1/applications/{id}/owner-decline` | Homeowner | Decline tenancy. |
| `POST` | `/v1/actions/consent-owner-tenancy` | Anonymous token | Email one-tap consent. |
| `POST` | `/v1/actions/decline-owner-tenancy` | Anonymous token | Email one-tap decline. |

*Extended:* listing create/update/submit with management and `AddedVia` fields; Hosthub via existing `/v1/channels*` connect/sync/disconnect; listing analytics DTOs include added-via.*

---

## Frontend changes

| Route / surface | Change |
|-----------------|--------|
| `/app/channels` | Hosthub connect / sync / disconnect. |
| `/owner/consent` | Anonymous owner consent landing. |
| `/app/owner-consents` | Homeowner pending consent inbox + nav entry. |
| Applications | Owner consent panel / tick on card and detail. |
| Listing create/edit | Ownership fields, broker clause, homeowner lookup. |
| `/app/admin/analytics`, listing analytics | CSV download; Added via column. |

---

## Database & schema

| Migration | Context | Summary |
|-----------|---------|---------|
| `20260817203729_AddListingManagementAndBrokerClause` | Listings | `ManagerRole`, `HomeOwnerUserId`, `IncludeBrokerClause`. |
| `20260817211408_AddOwnerTenancyConsentToDealApplication` | Billing | Owner consent fields + `HomeOwnerUserId` on applications. |
| `20260819180010_AddListingAddedVia` | Listings | `AddedVia`, `AddedViaDetail`. |

**Apply (after Aug 15 migrations if not yet run):**

```powershell
pwsh tools/scripts/add-listing-management-migration.ps1   # or SkipAdd + database update
pwsh tools/scripts/add-owner-tenancy-consent-migration.ps1
pwsh tools/scripts/db-migrate-listing-added-via.ps1 -SkipAdd
```

If 15 August was not deployed, also apply `WidenListingDescription` and `AddRentCheckIns` first.

---

## Configuration & dependencies

| Item | Purpose |
|------|---------|
| `Channels__Hosthub__BaseUrl` | Hosthub API base URL. |
| `Channels__Hosthub__SourceId` / `UserAgent` | Optional Hosthub request metadata. |
| Owner consent email templates | One-tap links to `/owner/consent?token=…`. |

---

## Deployment & verification checklist

1. **Build** API gateway, worker, and `apps/web`.
2. **Migrate** listing management → owner consent → listing added-via (and Aug 15 migrations if pending).
3. **Configure** Hosthub channel settings in target environment.
4. **Deploy** API + worker, then web.
5. **Smoke tests — 22 August**
   - Connect Hosthub on `/app/channels`; sync pulls listings; disconnect clears credentials.
   - Create PM listing with homeowner email lookup + broker clause; submit for review.
   - Guest applies → homeowner receives notice → consent via `/app/owner-consents` and via email one-tap; host approve blocked until consent; decline path works.
   - Admin listing analytics shows **Added via**; CSV download opens with expected columns.
   - Profile: invalid phone rejected; DOB under 18 rejected; phone OTP send fails cleanly on bad number.
6. **Regression** — Excel/XML import, rent check-ins, Guesty, DatePicker, KYC upload (15 August).

---

## Known limitations

- **Uncommitted scope** — Hosthub, owner consent, ownership fields, and related migrations remain in the working tree; commit and pass CI before tagging.
- **Payout on submit** — Intentionally relaxed for submit-for-review; re-enable when ready.
- **Owner consent** — Required only when listing manager role is Property Manager with a resolved homeowner.

---

## Document control

| Version | Date | Notes |
|---------|------|--------|
| 1.0 | 2026-08-22 | Hosthub, owner tenancy consent, listing ownership/broker/added-via, admin CSV, profile phone/age enforcement. |

---

*End of release notes.*
