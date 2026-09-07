# GetDistributionChannelReservationList

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/GetDistributionChannelReservationList`

---
### GetDistributionChannelReservationsList

The response of this API method will return every reservation for a given 3rd party booking partner in a Property Managers system which fall within the startdate and enddate parameters. Possible Reservation status_id's: 1 Not Completed 3 Attempt To Charge But Not Available 4 Booked 5 Closed 6 Deleted 7 Modified 8 Cancelled 9 Non Blocked Request 10 Blocked Request 12 No-Shows 13 Quote Sent

| Name | Data Type | Description | Required? |
|---|---|---|---|
| token_key | String (255) | The expiring token key credential provided to you by Streamline | Yes |
| token_secret | String (255) | The expiring token secret credential provided to you by Streamline | Yes |
| distributor_code | String | This is the distributor code for a distributor, found in distribution manager -> distributors. One 'distributor' can have multiple "distributor codes' | Yes |
| startdate | String | Sets the beginning of a date range. The max range allowed is one year. | Yes |
| enddate | String | Sets the end of a date range. The Max range allowed is one year. | Yes |

#### Request Example

- XML
- JSON

```xml
<?xml version="1.0" encoding="UTF-8"?>
<methodCall>
 <methodName>GetDistributionChannelReservationsList</methodName>
 <params>
  <token_key>YOUR_TOKEN_KEY</token_key>
  <token_secret>YOUR_TOKEN_SECRET</token_secret>
  <distributor_code>n/a</distributor_code>
  <startdate>n/a</startdate>
  <enddate></enddate>
 </params>
</methodCall>
```

#### Response Example

- XML
- JSON

```xml
<?xml version="1.0" ?>
<Response>
    <data>
        <reservation>
            <reservation_id>20080773</reservation_id>
            <confirmation_id>85138</confirmation_id>
            <status_id>4</status_id>
            <status_description>Booked</status_description>
            <cross_reference_code></cross_reference_code>
            <startdate>08/16/2020</startdate>
            <enddate>08/30/2020</enddate>
            <price_nightly>9660.00</price_nightly>
            <price_total>11894.07</price_total>
        </reservation>
        <reservation>
            <reservation_id>20028080</reservation_id>
            <confirmation_id>85137</confirmation_id>
            <status_id>4</status_id>
            <status_description>Booked</status_description>
            <cross_reference_code></cross_reference_code>
            <startdate>09/14/2020</startdate>
            <enddate>09/29/2020</enddate>
            <price_nightly>10350.00</price_nightly>
            <price_total>11540.25</price_total>
        </reservation>
    </data>
</Response>
```
