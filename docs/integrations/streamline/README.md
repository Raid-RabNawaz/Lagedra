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

Streamline confirmed on 2026-09-10 that **`CancelReservation` and `ModifyReservation` also exist**,
but they are not in our Partner X entitlement and are undocumented for us. We have asked for both to
be enabled. Streamline's guidance is that cancellations and modifications are "normally good to have
on the PM side to recalculate everything visually", so expect PM-side handling to be the default and
API-side to be the exception.

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

Streamline confirmed the feeds are near real-time: a PM-side change updates the modification
timestamp in their database immediately, the index feeds render dynamically or cache on short
intervals, and changes typically surface in `<lastUpdatedDate>` **within minutes**. Daily sync is
therefore a floor, not a ceiling — we can poll more aggressively for availability if the 100
requests/minute budget allows.

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

Streamline confirmed this on 2026-09-10: polling is the only mechanism, "as the core Streamline
Connect API was built in that way", and they have **no recommended polling pattern** to offer yet.
The reservation list is explicitly intended as the full audit/sync surface — a PM editing dates,
unit assignment, or rates in the Streamline admin UI moves the reservation to status `7` (Modified),
and a cancellation from either the API or the admin UI moves it to `8` (Cancelled).

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
- **Strip the advertiser suffix.** `advertiserAssignedId` appears as `3b3ghi_VRBO` while content URLs
  use `advertiser_id=3b3ghi`. Streamline confirmed the **6-character base code is the stable
  identifier** for the property management company across their platform, database, and feed query
  parameters. The `_VRBO` suffix scopes the entry to a distribution channel (Vrbo/HomeAway) and is
  omitted or stripped for generic Partner OLB feeds. Key our `ChannelListingMap` on the base code.
- **`stayIncrement` is documented but explicitly not supported.**
- **Response shapes vary between single item and list**, a well-known quirk of this API; parse
  defensively.
- **JSON examples are mostly missing from the portal.** Most method pages have XML/JSON tabs where
  the JSON tab is a dead link with no pane in the DOM; only `GetTokenExpiration` actually ships JSON
  samples. The XML examples are authoritative for field names, and the JSON envelope is the
  mechanical `methodName` + `params` translation shown above.
- **`VerifyPropertyAvailability` returns `<id>` and `<message>`,** not a boolean. Streamline
  clarified that `<id>` echoes back the `unit_id` when the unit **is** available, and `<message>`
  carries non-blocking informational notices and is usually empty. They did **not** describe the
  unavailable response, so treat "`id` equals the requested `unit_id`" as the availability signal
  and log anything else until we have observed a real negative response.
- **The "FEEDs Documentation" tree in Partner X is deprecated** and its pages are now empty stubs.
  The live reference is the General → Listings tree, which is what `reference/` mirrors.

---

## Provisioning status

We hold a Partner X **documentation** account (M10947) only. The account is **not yet connected to
the Streamline integration itself** — Streamline creates that once we answer their commercial
classification questions, and only then do the feed credentials and partner slug exist.

### Blocked on us

1. **Commercial classification.** Streamline must decide whether we operate as a *Wholesale Agent* or
   a *Post-Deduct Wholesale Agent*. They asked four questions to determine it: whether we take a
   commission and how it is collected, how we settle with the PM, whether we share guest payment
   methods with the PM, and whether we need to cancel reservations. See
   [Our commercial model](#our-commercial-model) for the facts to give them.
2. ~~**A static egress IP.**~~ **Done for staging: `13.52.31.177`.** Send this to Streamline to seed
   the allow list. `deploy/aws/11-egress-nat.sh` provisioned private subnets behind a NAT Gateway
   holding one Elastic IP, and moved `lagedra-api-staging` into them. Production still runs with
   `assignPublicIp=ENABLED` and ephemeral IPs — see [Egress](#egress) below.

### Blocked on Streamline

3. **Feed Basic-auth username and password** — pending account creation.
4. **Our partner slug** for `https://web.streamlinevrs.com/partner/{partnerName}/4.2.1/…` — not yet
   created.
5. **A test property manager**: token set, `distributor_code`, `advertiser_id`, opted-in units, and
   configured taxes and fees. Confirmed we will get one.
6. **Sandbox endpoint.** Confirmed one exists; the URL has not been supplied yet.
7. **`CancelReservation` and `ModifyReservation`** enabled on our account, with documentation. Both
   methods exist but are outside our current entitlement.
8. **`GetPaymentTypes` access** — escalated to a superior, no answer yet.
9. **The "total" logic contract** — `force_adjustment`, `rate_only`, `final_price` — escalated, no
   answer yet.
10. **The 4.2.1 XSD files** — escalated, no answer yet.
11. **A recommended polling pattern** — Streamline has none to offer today and will consult
    internally after our integration is working.
12. **The unavailable-case response** for `VerifyPropertyAvailability` — only the available case was
    explained.

**Not an ask:** our Partner X user already has admin rights over the
[IP allow list](https://partner.streamlinevrs.com/admin_pages/allowed_ips), which accepts individual
addresses or CIDR ranges. Streamline only needs to seed the first entry.

### Egress

Streamline refuses calls from non-allow-listed IPs, and Fargate tasks otherwise get a fresh public IP
on every deploy. `deploy/aws/11-egress-nat.sh` fixes this by routing tasks through a NAT Gateway that
holds a single Elastic IP.

| | Egress IP | Status |
|---|---|---|
| Staging (`lagedra-api-staging`) | **13.52.31.177** | Done — private subnets, `assignPublicIp=DISABLED`, verified healthy |
| Production (`lagedra-api`, `lagedra-worker`) | ephemeral | **Not migrated.** Run `bash deploy/aws/11-egress-nat.sh` to move them onto the same NAT and IP |

Both environments share one NAT Gateway (`nat-061756117396e24e5`) and therefore one egress IP, so
allow-listing `13.52.31.177` covers production too once it is migrated. Supporting resources:
private subnets `subnet-062b0fc5604a47394` (us-west-1a) and `subnet-088509025bb8c32f5` (us-west-1c),
route table `rtb-0850dbed890457504`, Elastic IP `eipalloc-009449c7f2a87ac63`.

The private route table carries only a local route, a default route to the NAT, and an S3 gateway
endpoint, so no task can reach the internet by any other path.

Support goes through the Partner X [support form](https://partner.streamlinevrs.com/support/contact_support)
or `integrationpartners@streamlinevrs.com`. Our contact is Alex.

---

## Our commercial model

Streamline's classification hinges on how money moves, so these are the facts as implemented, not as
marketed. Note that our other channel docs describe Lagedra as "merchant of record"; that is true of
the guest relationship but **not** of card settlement, and the distinction matters here.

| Streamline's question | Lagedra's answer |
|---|---|
| Do you have a commission, and how is it collected? | We take **no commission out of the PM's rent**. We add a tenant service fee and a stay-protection fee **on top** of rent, retained via the Stripe `ApplicationFeeAmount`. Hosts pay a **flat monthly subscription**, not a per-booking cut. |
| How do you settle with a PM? | **No settlement in arrears.** Stripe Connect destination charges split funds **at the moment of payment**: first month's rent and the security deposit transfer straight to the host's connected account. We never hold PM funds. |
| Do you share guest payment methods with the PM? | **No.** We hold Stripe tokens only, never card numbers, and the booking push carries no payment instrument. |
| Do you need to cancel reservations? | **Yes**, hence the request for `CancelReservation` and `ModifyReservation`. |

Implementation: `StripeService.CreateDestinationPaymentIntentAsync` uses `TransferData.Destination`
plus `OnBehalfOf` set to the host, which makes the **host the settlement merchant of record**, with
`ApplicationFeeAmount` covering our fees. See `DealFinancials` for the tenant total breakdown.

Because we neither buy at a net rate nor deduct commission from the folio, we fit **neither** of
Streamline's two wholesale models cleanly. The `commission` parameter on
`MakeReservationDistributionChannel` would be `0` or omitted for us.

### The mid-term mismatch

Streamline is a nightly vacation-rental system; Lagedra is a **mid-term marketplace** with monthly
rent and stays clamped to **30–180 days** (`ListingImporter`), plus lease agreements. Other channel
imports approximate monthly rent as `nightlyCents * 30`. Two consequences worth raising with
Streamline before building:

- **Rate structures.** Length-of-Stay rates matter far more to us than nightly rates, and monthly
  rate plans may not be expressible in the 4.2.1 feed.
- **Tax treatment.** Many US jurisdictions exempt stays over 30 days from transient occupancy tax.
  We need to know whether `GetPreReservationPrice` applies those exemptions automatically for long
  stays, or whether it will quote nightly-stay taxes that we would then be collecting incorrectly.

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
