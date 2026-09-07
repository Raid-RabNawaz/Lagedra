# Streamline VRS integration

Partner notes for connecting Lagedra to [Streamline VRS](https://www.streamlinevrs.com/) (enterprise
vacation-rental PMS). Streamline has **no public API documentation** — everything in `reference/`
was captured from the authenticated [Partner X portal](https://partner.streamlinevrs.com) on
2026-08-27 under the Lagedra partner account.

Streamline is **not** a paste-your-own-API-key provider like Hosthub or Hostaway. Credentials are
issued per property manager by Streamline, and our server IPs must be allow-listed before any call
succeeds. That changes the host onboarding flow — see [Onboarding differences](#onboarding-differences).

---

## What our account can actually do

Partner X only shows the methods our partnership is entitled to. Ours is the
**Listings + Partner OLB** integration, which is a distribution-channel model: bulk content arrives
as XML feeds, and a small JSON API handles the live booking path.

There is no `GetPropertyList` / `GetPropertyInfo` / `GetPropertyRates` in our entitlement. Public
third-party SDKs (for example `cronixweb/streamline-sdk`) are written against that other, broader
JSON API — **do not use them as a model for our integration.**

| Half of the integration | Transport | Carries |
|---|---|---|
| Listings XML feeds | XML over HTTPS, index → content | Listing content, photos, amenities, rates, taxes/fees, policies, rental agreements, cancellation policies, availability |
| Partner OLB API | JSON or XML RPC | Live availability check, price quote, booking, booked-reservation query, token renewal |

The five Partner OLB methods are the complete list:

| Method | Purpose | Reference |
|---|---|---|
| `VerifyPropertyAvailability` | Fast availability check before booking | [verify-property-availability.md](./reference/verify-property-availability.md) |
| `GetPreReservationPrice` | Price quote with taxes, fees, per-night breakdown | [get-pre-reservation-price.md](./reference/get-pre-reservation-price.md) |
| `MakeReservationDistributionChannel` | Post a reservation into Streamline | [make-reservation-distribution-channel.md](./reference/make-reservation-distribution-channel.md) |
| `GetDistributionChannelReservationList` | List our reservations in a PM's system | [get-distribution-channel-reservation-list.md](./reference/get-distribution-channel-reservation-list.md) |
| `GetTokenExpiration` · `RenewExpiredToken` | Token lifecycle | [get-token-expiration.md](./reference/get-token-expiration.md) · [renew-expired-token.md](./reference/renew-expired-token.md) |

---

## Endpoints and authentication

There are **two independent credential sets**, which is the single most important thing to get right.

| | Listings XML feeds | Partner OLB API |
|---|---|---|
| Auth | HTTP Basic (`Authorization` header) | `token_key` + `token_secret` inside the request body |
| Scope | One global credential across **all** PMs in our feeds | One unique token set **per property manager** |
| Issued by | Engagement Manager during onboarding | The connected PM, or emailed via self-service onboarding |
| Rotation | Static unless we request a change | Expires every 90 days |
| Failure code | `E0034` Invalid username or password | — |

| Endpoint | URL |
|---|---|
| JSON API | `https://web.streamlinevrs.com/api/json` |
| XML API | `https://web.streamlinevrs.com/api/1.1` |
| Feeds (v4.2.1) | `https://web.streamlinevrs.com/partner/streampal/4.2.1/{index-or-content}?…` |
| IP allow list | https://partner.streamlinevrs.com/admin_pages/allowed_ips |

Auth is **not** header-based for the OLB API. `methodName` sits at the top level and credentials go
inside `params` next to the business arguments:

```json
{
  "methodName": "VerifyPropertyAvailability",
  "params": {
    "token_key": "YOUR_TOKEN_KEY",
    "token_secret": "YOUR_TOKEN_SECRET",
    "unit_id": "288531",
    "startdate": "01/10/2020",
    "enddate": "01/17/2020"
  }
}
```

### Operational limits

- **IP allow-listed.** A token set only works from an allow-listed IPv4/IPv6 address. Our API
  gateway egress IPs must be registered per environment before anything works, including local dev.
  Streamline recently changed endpoints for IPv6 compatibility and monitors for misconfigured lists.
- **100 requests per minute** per token set. Streamline explicitly expects integrations above that
  to cache locally rather than request a raise, which is an argument for feed-driven sync over
  live polling.
- **90-day token expiry**, per PM, renewed with `RenewExpiredToken`. Renewal invalidates the old set
  immediately and returns the new pair in the response, so renewal must be transactional with
  however we persist credentials. An expired token can still be renewed using the last valid set,
  and renewal must originate from an allow-listed IP.

---

## Feed model

The feeds are an index-of-indexes tree. Start at the Advertisers Content Index, which lists every
property manager who has opted into our integration, with URLs to that PM's four sub-indexes:

```
advertisersContentIndex
├── advertiserListingContentIndex        → getListing?listing_id=…&code=…
├── advertiserLodgingConfigurationContentIndex
├── advertiserLodgingRateContentIndex
└── advertiserUnitAvailabilityContentIndex
```

Every index entry carries `<lastUpdatedDate>`, so incremental sync is a matter of comparing that
against our last-synced timestamp rather than re-fetching content. Streamline recommends refreshing
the feeds **at least once a day** and notes that content whose `lastUpdatedDate` falls within the
last three days should be retrieved and processed.

Only units the PM explicitly opted into the integration appear in the feed. A PM missing from the
feed has most likely not selected any units yet — that is the first thing to check before assuming
a bug.

---

## Mapping onto `IChannelProvider`

Streamline fits our existing `IChannelProvider` contract, but two members need care.

| Contract member | Streamline implementation |
|---|---|
| `PullListingsAsync` | Walk the Advertisers index → Listing index → Listing content feeds |
| `PullAvailabilityAsync` | Unit Availability index → Unit Availability content feed |
| `CheckAvailabilityAsync` | `VerifyPropertyAvailability`, optionally `GetPreReservationPrice` for the quote |
| `PushBookingAsync` | `MakeReservationDistributionChannel` |
| `PullBookingUpdatesAsync` | `GetDistributionChannelReservationList` — **see caveat below** |

`PullBookingUpdatesAsync(changedSinceUtc)` does not map cleanly.
`GetDistributionChannelReservationList` takes only `startdate`/`enddate`, capped at a one-year range,
and those filter on **stay dates, not modification dates**. There is no "changed since" parameter and
no webhooks anywhere in this integration. Detecting host-side cancellations means periodically
re-listing a rolling stay window and diffing `status_id` against what we hold.

Relevant `status_id` values: `4` Booked, `5` Closed, `6` Deleted, `7` Modified, `8` Cancelled,
`9` Non-Blocked Request, `10` Blocked Request, `12` No-Show, `13` Quote Sent.

### Pushing a booking as merchant of record

Lagedra collects payment, so we never send card data. `MakeReservationDistributionChannel` accepts
credit-card fields but all of them are optional; the relevant flags are:

| Param | Value | Why |
|---|---|---|
| `disable_payments` | `1` | Stops Streamline attempting to charge anything |
| `total` | our gross total | Room rent plus all taxes and fees |
| `force_adjustment` | `1` | Required when using Streamline's "total" logic |
| `rate_only` | `1` | Required alongside `force_adjustment` for total logic |
| `final_price` | must equal `total` | Price validation |
| `reservation_id` | Lagedra booking id | Becomes the cross-reference ID in the PM's Streamline |
| `commission` | our fee | Deducted from room rent on the Streamline commission tab |
| `hear_about_new` | `Lagedra` | Marks the reservation source for the PM |
| `status_id` | `4`, or `10` per PM | Some PMs want partner bookings as Blocked Requests instead of Booked |

`distributor_code` is required on the quote, booking, and reservation-list calls. It is issued per
distribution channel (Distribution Manager → Distributors in Streamline) and one distributor can
have several codes, so treat it as connection configuration alongside the token set, not a constant.

---

## Onboarding differences

Our other providers let a host paste their own API key. Streamline cannot work that way:

- Token sets are issued by the **property manager's** Streamline account granting our company
  access, or emailed to us through Streamline's self-service onboarding — the host cannot generate
  one from a settings page.
- Streamline's partner agreement forbids vendors accessing data through a client account; we must
  use our own Partner X account, and this is audited weekly.
- Units must be opted into the integration inside Streamline before they appear in our feeds.

So a Streamline connection is closer to a per-PM provisioned tenant than a self-service key paste.
`ChannelConnection` will need to hold the token pair, the `distributor_code`, and the PM's
advertiser/assigned ID, and the UI needs a "request access" path rather than a key field.

---

## Gotchas

- **Two date formats.** OLB methods use `MM/DD/YYYY`; the feeds use `YYYY-MM-DD` (and
  `yyyy-MM-ddTHH:mm:ssZ` for `lastUpdatedDate`). Do not share a formatter.
- **Money is decimal strings with a separate `<currency>`**, not minor units. Our `*Cents` fields
  need explicit conversion, and rounding has to be decided before we quote.
- **Two incompatible rate structures.** A single PM can have some units on Nightly rates and others
  on Length-of-Stay rates, so the rate parser must switch per unit rather than per PM.
- **Availability arrays are positional and inconsistently delimited.** `<availability>` and
  `<changeOver>` are character runs (`YYYNNN…`, `CCIIOO…`) while `minStay`, `maxStay`,
  `minPriorNotify`, and `availableUnitCount` are comma-separated. The docs describe `<availability>`
  as "comma-separated" but the examples are not — trust the examples. Each position maps to a day in
  `<dateRange>`, up to 1096 days, with defaults applying outside the range.
- **`listingExternalId` and `unitExternalId` are the same value** in the availability feed.
- **`stayIncrement` is documented but explicitly not supported.**
- **Response shapes vary between single item and list**, a well-known quirk of this API; parse
  defensively.
- **JSON examples are mostly missing from the portal.** Most method pages have XML/JSON tabs where
  the JSON tab is a dead link with no pane in the DOM; only `GetTokenExpiration` actually ships JSON
  samples. The XML examples are authoritative for field names, and the JSON envelope is the
  mechanical `methodName` + `params` translation shown above.
- **`VerifyPropertyAvailability` returns `<id>` and `<message>`,** not a boolean. Semantics of `id`
  are undocumented; confirm with Streamline support before relying on it.
- **The "FEEDs Documentation" tree in Partner X is deprecated** and its pages are now empty stubs.
  The live reference is the General → Listings tree, which is what `reference/` mirrors.

---

## Still needed from Streamline

Blocking — nothing can be built or tested without these:

1. **Feed Basic-auth username and password** for the Listings feeds, from our Engagement Manager.
2. **Our partner slug** for the feed root. The Advertisers Content Index lives at
   `https://web.streamlinevrs.com/partner/{partnerName}/4.2.1/getAdvertisersContentIndex`; portal
   examples use `streampal`, and we have not been told ours.
3. **A test property manager**: token set, `distributor_code`, `advertiser_id`, and opted-in units.
4. **Sandbox or test environment confirmation.** Portal examples reference a "Gueststream Sandbox"
   advertiser; we do not know whether we get one or must test against a live PM.

Design-blocking — answers change the implementation:

5. **How do we cancel or modify a reservation we created?** There is no cancel or modify method in
   our entitlement, only `MakeReservationDistributionChannel`. The reservation list reports
   `Cancelled` and `Modified` statuses, so those transitions must be possible somehow.
6. **Is polling the only change-detection mechanism?** No webhooks are documented and
   `GetDistributionChannelReservationList` filters on stay dates, not modification dates.
7. **`GetPaymentTypes` access.** `GetPreReservationPrice` documents `payment_type_id` as coming from
   `GetPaymentTypes`, which is not in our entitlement or documentation.
8. **Confirm the "total" logic contract** — `force_adjustment`, `rate_only`, and `final_price`. The
   docs say `rate_only` "will be discussed in the development process", which suggests per-PM setup.
9. **`VerifyPropertyAvailability` response semantics.** It returns `<id>` and `<message>` rather than
   a boolean, and `id` is undocumented.

Nice to have:

10. **The 4.2.1 XSD files.** Every feed cites a "Listing XSD version" but the schemas are not in the
    portal. They would let us generate parsers instead of hand-rolling them.
11. **Feed regeneration cadence** — how quickly a PM-side change is reflected in `lastUpdatedDate`.
12. **Identifier clarification.** `advertiserAssignedId` appears as `3b3ghi_VRBO` while the feed URLs
    use `advertiser_id=3b3ghi`. Confirm which is the stable key and what the suffix means.

**Not an ask:** the IP allow list is self-service under
[Administration → Allowed IPs](https://partner.streamlinevrs.com/admin_pages/allowed_ips) for admin
users, and accepts individual addresses or CIDR ranges. We add our own egress IPs; we only need to
confirm our portal user has admin rights.

Support goes through the Partner X [support form](https://partner.streamlinevrs.com/support/contact_support)
or `integrationpartners@streamlinevrs.com`.

---

## Reference

Verbatim captures, one file per portal page. Credential-looking values are replaced with
`YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

**Overview and auth**

- [getting-started.md](./reference/getting-started.md) — endpoints, tokens, rate limits, IP allow list
- [integration-overview.md](./reference/integration-overview.md) — feature summary of both halves
- [listings-authentication.md](./reference/listings-authentication.md) — feed Basic auth
- [partner-olb-authentication.md](./reference/partner-olb-authentication.md) — token auth
- [api-token-renewal.md](./reference/api-token-renewal.md) — 90-day renewal rules

**Partner OLB API**

- [partner-olb.md](./reference/partner-olb.md), [price-quote-api.md](./reference/price-quote-api.md),
  [partner-olb-reservation-query.md](./reference/partner-olb-reservation-query.md) — group pages
- [verify-property-availability.md](./reference/verify-property-availability.md)
- [get-pre-reservation-price.md](./reference/get-pre-reservation-price.md)
- [make-reservation-distribution-channel.md](./reference/make-reservation-distribution-channel.md)
- [get-distribution-channel-reservation-list.md](./reference/get-distribution-channel-reservation-list.md)
- [get-token-expiration.md](./reference/get-token-expiration.md), [renew-expired-token.md](./reference/renew-expired-token.md)

**Listings XML feeds (v4.2.1)**

- [listings-content-retrieval.md](./reference/listings-content-retrieval.md) — how index and content files relate
- [listings-xml-reference.md](./reference/listings-xml-reference.md) — feed overview
- [advertisers-content-index.md](./reference/advertisers-content-index.md) — entry point
- [listing-index.md](./reference/listing-index.md) · [listing-content.md](./reference/listing-content.md)
- [lodging-configuration-index.md](./reference/lodging-configuration-index.md) · [lodging-configuration-content.md](./reference/lodging-configuration-content.md)
- [lodging-rate-index.md](./reference/lodging-rate-index.md) · [lodging-rate-content.md](./reference/lodging-rate-content.md)
- [unit-availability-index.md](./reference/unit-availability-index.md) · [unit-availability-content.md](./reference/unit-availability-content.md)
- [listings-enumerations.md](./reference/listings-enumerations.md) — amenity, property-type, and code lists

See [hosthub-integration.md](../../hosthub-integration.md) for the shape our other PMS integrations
follow, and [api-guide.md](../../api-guide.md#channels-pms--channel-managers) for the shared channel
contract.
