# Lagedra Release Plan - Wednesday 06/05/2026

> Release window: **Wed 06/05/2026 - 10:00 PST** (smoke test 13:00 PST)
> Branch / tag: `release/2026-05-06`
> Primary theme: **Partner Portal readiness**
> Planning source: next Wednesday scope after `RELEASE_NOTES_2026-04-29.md`

---

## TL;DR

This release turns the existing `PartnerNetwork` backend foundation into a usable partner portal. The current repo already has partner organization registration, verification, members, referral links, direct reservations, JWT partner membership claims, and database schema. What is missing is the actual frontend portal, stricter organization-level authorization, partner API client wiring, reservation listing, and test coverage.

The goal for 06/05 is to let an `InstitutionPartner` sign in, manage their organization, add members, generate referral links, redeem referrals, and create/view direct reservations without relying on manual API calls.

---

## Release Objective

Ship a minimum complete partner portal for verified institutional partners while hardening the backend so partner actions cannot leak across organizations.

Success means:

- Partner users have a clear `/app/partner` area in the web app.
- Partner org admins can manage members, referral links, and reservations.
- Platform admins can verify partner organizations from an admin surface.
- Referral redemption updates the referred user's risk profile as designed.
- Organization-scoped authorization is enforced server-side, not only in the UI.

---

## Scope

### P0 - Must Ship

- Add frontend partner portal routes under `/app/partner`.
- Add partner API endpoints to `apps/web/src/api/endpoints.ts`.
- Add partner service/client functions for organizations, members, referral links, referral redemption, and reservations.
- Add sidebar and dashboard navigation for `InstitutionPartner`.
- Add partner organization overview page.
- Add partner registration/onboarding page for users without a partner organization.
- Add referral link management UI with create, list, usage count, expiry, and copy-link behavior.
- Add member management UI with add/list support.
- Add direct reservations UI with create/list support.
- Add backend endpoint to list partner direct reservations.
- Enforce organization-level authorization on partner members, referral links, and reservations.
- Restrict partner management actions to `InstitutionPartner`, `PlatformAdmin`, and authorized organization members/admins.
- Add release smoke tests for the full partner flow.

### P1 - Should Ship

- Add admin partner verification page under `/app/admin/partners`.
- Add admin API/query for pending partner organizations if not already available.
- Add clear empty/error states for partner pages.
- Add toast/error handling for partner API failures using the existing friendly error layer.
- Add validation for referral link limits and expiry dates.
- Add duplicate member handling in the UI.
- Add partner org status badges: `PendingVerification`, `Verified`, `Suspended`.
- Add frontend types for partner DTOs.
- Add backend tests for partner authorization and referral redemption.

### P2 - Nice to Ship

- Add deactivate referral link endpoint and UI action.
- Add resend/invite email flow for partner members.
- Add reservation detail page with linked deal/application status.
- Add analytics card for referral conversions and active reservations.
- Add audit log entries for partner org verification, member add, referral creation, and reservation creation.

---

## Backend Work

### Current Backend Already Exists

The existing module is `src/Lagedra.Modules/PartnerNetwork` and already includes:

- `PartnerOrganization`
- `PartnerMember`
- `ReferralLink`
- `ReferralRedemption`
- `DirectReservation`
- `RegisterPartnerOrganizationCommand`
- `VerifyPartnerOrganizationCommand`
- `AddPartnerMemberCommand`
- `GenerateReferralLinkCommand`
- `RedeemReferralLinkCommand`
- `CreateDirectReservationCommand`
- `GetPartnerOrganizationQuery`
- `ListPartnerMembersQuery`
- `ListReferralLinksQuery`
- `PartnerMembershipProvider`
- `partner_org_id` JWT claim through `IPartnerMembershipProvider`

### Required Backend Changes

- Add `ListDirectReservationsQuery` and handler.
- Add `GET /v1/partners/{id}/reservations`.
- Add endpoint/policy checks so only the following can manage a partner org:
  - `PlatformAdmin`
  - organization admin member
  - authorized organization member for read-only operations
- Add helper service for partner authorization, for example `IPartnerAccessService`.
- Update `AddPartnerMemberCommand` to require inviter authorization before adding members.
- Update `GenerateReferralLinkCommand` to require org admin/member authorization.
- Update `ListReferralLinksQuery` and `ListPartnerMembersQuery` to enforce caller authorization.
- Update `CreateDirectReservationCommand` to require verified organization and authorized caller.
- Add query for "my partner organization" using the current user's membership, or expose enough data through `/v1/auth/me` for the frontend to locate the partner org.
- Add admin query/list endpoint for partner organizations, filtered by status.
- Ensure referral link generation checks for code collisions before save.
- Convert referral expiration/max-use domain exceptions into structured `Result` failures instead of uncaught exceptions.

### Backend Endpoints Target

Partner user endpoints:

- `POST /v1/partners`
- `GET /v1/partners/me`
- `GET /v1/partners/{id}`
- `GET /v1/partners/{id}/members`
- `POST /v1/partners/{id}/members`
- `GET /v1/partners/{id}/referral-links`
- `POST /v1/partners/{id}/referral-links`
- `POST /v1/partners/{id}/referral-links/{linkId}/deactivate`
- `GET /v1/partners/{id}/reservations`
- `POST /v1/partners/{id}/reservations`
- `POST /v1/referral/{code}/redeem`

Admin endpoints:

- `GET /v1/admin/partners`
- `GET /v1/admin/partners/pending`
- `POST /v1/partners/{id}/verify`
- `POST /v1/admin/partners/{id}/suspend`

---

## Frontend Work

### Current Frontend State

The frontend currently has `InstitutionPartner` and `InsurancePartner` roles, but no real partner portal:

- `apps/web/src/app/auth/roles.ts` defines partner roles.
- `apps/web/src/app/auth/permissions.ts` only gives partner roles generic dashboard/account nav.
- `apps/web/src/features/auth/pages/DashboardPage.tsx` has an `Organization` action pointing to `#`.
- `apps/web/src/api/endpoints.ts` has no partner API group.
- There is no `apps/web/src/features/partners` feature folder.
- There is no `/app/partner` route.

### Required Frontend Changes

- Add `apps/web/src/features/partners`.
- Add `partnerApi.ts` service functions.
- Add `partnerTypes.ts` or shared DTO types.
- Add partner routes in `apps/web/src/app/routes.tsx`.
- Update `permissions.ts` so `InstitutionPartner` gets partner navigation.
- Update `DashboardPage.tsx` so `Organization` links to `/app/partner`.
- Add `RequireRole allowed={[roles.institutionPartner, roles.platformAdmin]}` around partner routes.
- Add friendly errors and loading states on every partner page.

### Partner Pages Target

- `/app/partner` - overview/dashboard
- `/app/partner/onboarding` - register partner organization
- `/app/partner/members` - list/add members
- `/app/partner/referrals` - create/list/copy referral links
- `/app/partner/reservations` - create/list direct reservations
- `/app/admin/partners` - platform admin verification queue

### Partner Navigation Target

Institution partner sidebar groups should include:

- `Partner Dashboard` -> `/app/partner`
- `Members` -> `/app/partner/members`
- `Referral Links` -> `/app/partner/referrals`
- `Reservations` -> `/app/partner/reservations`
- `Profile` -> `/app/profile`

---

## User Flows To Support

### Flow 1 - Institution Partner Onboarding

1. User registers as `InstitutionPartner`.
2. User verifies email and logs in.
3. User lands on dashboard with partner-specific CTA.
4. User creates partner organization.
5. Organization enters `PendingVerification`.
6. UI explains that admin verification is required before reservations/referrals can be used.

### Flow 2 - Admin Verification

1. Platform admin opens `/app/admin/partners`.
2. Admin sees pending partner organizations.
3. Admin verifies an organization.
4. Organization status changes to `Verified`.
5. Partner user can create referral links and reservations.

### Flow 3 - Referral Link

1. Partner admin opens `/app/partner/referrals`.
2. Partner creates referral link with expiry and max uses.
3. UI shows generated code and full redemption URL.
4. Partner copies link.
5. Referred user redeems code.
6. `ReferralRedeemedEvent` recalculates risk with `InstitutionBacked` insurance status.

### Flow 4 - Direct Reservation

1. Verified partner opens `/app/partner/reservations`.
2. Partner selects/enters listing ID and guest details.
3. API creates `DirectReservation`.
4. Reservation appears in list.
5. Follow-up work can link reservation to deal application.

---

## Security And Authorization Gates

- [ ] Anonymous users cannot access any `/v1/partners/*` endpoint except none; referral redemption still requires auth.
- [ ] Tenant/Landlord users cannot create or manage partner org resources.
- [ ] Institution partner users cannot access another organization's members/referrals/reservations.
- [ ] Partner members can only perform actions allowed by their `PartnerMemberRole`.
- [ ] Platform admin can verify/suspend partner organizations.
- [ ] Suspended partner organizations cannot create referral links or reservations.
- [ ] Pending partner organizations cannot create direct reservations.
- [ ] JWT `partner_org_id` is refreshed after membership creation or the frontend refetches current profile after onboarding.

---

## QA / Release Gates

### Automated Checks

- [ ] Backend build passes.
- [ ] Frontend build passes.
- [ ] PartnerNetwork command/query tests pass.
- [ ] Partner authorization tests pass.
- [ ] Frontend typecheck passes.

### Manual Smoke Tests

- [ ] Register as `InstitutionPartner`.
- [ ] Create partner organization.
- [ ] Confirm pending-status UI.
- [ ] Verify partner organization as `PlatformAdmin`.
- [ ] Partner dashboard shows verified status.
- [ ] Add partner member.
- [ ] Create referral link.
- [ ] Copy referral link and redeem as another authenticated user.
- [ ] Confirm referral redemption cannot be reused by the same user.
- [ ] Create direct reservation.
- [ ] Confirm reservation appears in list.
- [ ] Confirm unrelated partner user cannot view another org's resources.
- [ ] Confirm tenant/landlord users cannot access `/app/partner`.
- [ ] Confirm admin partner page loads and handles empty state.

### Production Smoke After Deploy

- [ ] `https://api.lagedra.com/health` returns healthy.
- [ ] `/auth/register` includes `InstitutionPartner`.
- [ ] `/app/partner` loads for an institution partner.
- [ ] `/app/admin/partners` loads for platform admin.
- [ ] Partner API calls return no unexpected `4xx/5xx`.
- [ ] Existing booking, listing, checkout, verification, and admin pages still load.

---

## Rollout

This is a full backend + frontend release.

1. Build and deploy backend API/worker using `DEPLOYMENT.md` Section A.
2. Build and deploy frontend using `DEPLOYMENT.md` Section B.
3. If new config is added, register task definitions using `DEPLOYMENT.md` Section C.
4. Invalidate CloudFront.
5. Run the production smoke checklist above.

Rollback:

- Redeploy the previous ECS task definitions for API/worker.
- Re-sync the prior frontend `dist` snapshot to `lagedra-web-prod`.
- Invalidate CloudFront with `/*`.

---

## Out Of Scope

- Full invite-email delivery for partner members.
- Partner billing/subscription management.
- Automated listing selection UX for direct reservations beyond listing ID entry.
- Reservation-to-application conversion automation.
- Partner analytics dashboards beyond simple counts.
- Insurance partner portal; this release focuses on `InstitutionPartner`.

---

## Ownership And Tracking

- Board label: `R-2026-05-06`
- Priority labels: `P0`, `P1`, `P2`
- Code freeze: **Tue 05/05/2026 - 12:00 PST**
- Release window: **Wed 06/05/2026 - 10:00 PST**
- Smoke test: **Wed 06/05/2026 - 13:00 PST**
- Post-release monitoring: **Thu 07/05/2026**

Any unresolved P0 item blocks the release. Any unresolved P1 item needs explicit release-owner approval to defer.
