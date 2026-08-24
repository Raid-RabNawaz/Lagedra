# Hosthub integration

Partner notes for connecting Lagedra to [Hosthub](https://www.hosthub.com/) (PMS / channel manager). Public API: [hosthub.com/docs/api](https://www.hosthub.com/docs/api/).

Hosthub is registered as `providerKey: hosthub`. Hosts paste their own Hosthub API key on **Import from your PMS**. Platform config is only the Hosthub origin (`Channels__Hosthub__BaseUrl`) and an optional `SourceId`.

---

## Environments

| | Staging (development) | Production |
|---|---|---|
| App | https://eric.hosthub.com/ | https://app.hosthub.com/ |
| API base | `https://eric.hosthub.com/api/2019-03-01` | `https://app.hosthub.com/api/2019-03-01` |
| Rentals UI | https://eric.hosthub.com/z/rentals | Hosthub rentals |
| Bookings UI | https://eric.hosthub.com/bookings | Hosthub bookings |

`2019-03-01` is Hosthub’s current API version. They keep it until a breaking change ships under a new date (for example `2027-…`). All calls append the resource path to the base above.

Auth is **ApiKeyAuth**. Lagedra sends the key as `Authorization` (raw), then retries `Bearer` on 401/403 and caches the scheme that works.

Default `Channels__Hosthub__BaseUrl` is production (`https://app.hosthub.com`) so hosts’ live keys work. Point it at `https://eric.hosthub.com` only when testing against Hosthub staging.

---

## Staging setup (Hosthub)

1. Go to https://eric.hosthub.com/ and register a new account.
2. Sign in and open **Settings**.
3. Under **API keys**, create a new API key.
4. Create rentals at https://eric.hosthub.com/z/rentals.
5. Create bookings manually at https://eric.hosthub.com/bookings.
6. Call `https://eric.hosthub.com/api/2019-03-01` with that key.

API keys are created in the Hosthub app by the **account owner** (property manager). The same owner pastes the key into Lagedra.

---

## How Lagedra will use the API

Same contract as Hostaway / Guesty / OwnerRez / Smoobu (`IChannelProvider`):

| Lagedra step | Hosthub resources |
|---|---|
| Validate the key / identify the account | `GET /users` |
| Import listings | `GET /rentals`, then `GET /rentals/{id}` (and rate plans / photos as needed) |
| Availability | `GET /rentals/{id}/calendar-events`, `GET /rate-plans/{id}/rates` |
| Push a paid Lagedra booking (merchant of record; no card data) | `POST /rentals/{id}/calendar-events` with `type: Booking` |
| Pull Hosthub-side changes (cancels, new bookings, holds) | `GET /calendar-events?updated_gt=…` |
| Optional blocks | `POST /rentals/{id}/calendar-events` with `type: Hold` |

Money on Hosthub is `{ cents, currency }`, which matches Lagedra’s `*Cents` fields.

List rental payloads are thin (name, city, country, lat/lng, currency, check-in/out, max guests). Detail, rate-plan, and calendar calls will likely be required to fill a Lagedra draft.

---

## User flow (for Hosthub’s documentation)

Copy the following to Hosthub. It matches Lagedra’s existing PMS connect path on **Import from your PMS** (`/app/channels`).

### Connecting Hosthub to Lagedra

Lagedra is a mid-term rental marketplace. Property managers connect Hosthub once so listings, calendars, and paid bookings stay in sync. Lagedra is the merchant of record: guests pay on Lagedra, and Lagedra writes the stay back to Hosthub. Card data is never sent to Hosthub.

**Who creates the API key:** the Hosthub account owner (property manager), inside the Hosthub app.

1. In Hosthub, the account owner opens **Settings → API keys** and creates a new API key. The key is shown once; they copy it.
2. In Lagedra they sign in as a host and open **Import from your PMS**.
3. On **Connect Hosthub** they paste the API key (optional label) and choose **Connect & import listings**.
4. Lagedra stores the key encrypted, never displays it again, and pulls Hosthub rentals as **draft listings**.
5. The host reviews each draft, fills anything missing, and submits it for approval / publish.
6. They can **Sync from Hosthub** anytime (or wait for the scheduled sync) to update existing drafts and import new rentals.
7. When a guest books and pays on Lagedra, Lagedra creates a Hosthub calendar event (`type: Booking`) on that rental so the dates are blocked on Hosthub and other channels.
8. Lagedra also reads Hosthub calendar events so Hosthub-side bookings, holds, and cancellations keep Lagedra availability accurate.
9. One Hosthub connection per Lagedra host. To change account or rotate a key, they disconnect in Lagedra and connect again — imported listings stay. They can also revoke the key in Hosthub.

**Disconnect:** **Disconnect** on the Hosthub card in Lagedra, or revoke the key in Hosthub Settings. Imported Lagedra listings are not deleted.

---

## Lagedra API

No new public routes. Hosts use the existing channel endpoints with `providerKey: hosthub`:

| Method | Path | Notes |
|---|---|---|
| GET | `/v1/channels/providers` | Includes `hosthub` when registered |
| POST | `/v1/channels` | `providerKey`, `secret` (API key), optional display name |
| POST | `/v1/channels/{id}/sync` | Pull rentals into drafts |
| GET | `/v1/channels/{id}/listings` | Imported maps |
| POST | `/v1/channels/{id}/enable` · `/disable` | Pause without deleting |
| DELETE | `/v1/channels/{id}` | Disconnect |

See [api-guide.md](./api-guide.md#channels-pms--channel-managers) for the shared channel contract.
