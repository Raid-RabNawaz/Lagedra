# Partner OLB Reservation Query

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/Partner-OLB-Reservation-Query`

---
You can use the API method "GetDistributionChannelReservationList" to query the integrated property managers system for any bookings made specifically by you in their Streamline system.

The response only returns reservations with the "Reservation Type" associated to your integration. This is also linked to the "distributor_code" you use in the "MakeReservationDistributionChannel" and "GetPreReservationPrice" Partner OLB API methods.
