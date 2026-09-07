# VerifyPropertyAvailability

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/VerifyPropertyAvailability`

---
### VerifyPropertyAvailability

Verify Property Availability - Fast availablity check before making a booking.

| Name | Data Type | Description | Required? |
|---|---|---|---|
| token_key | String (255) | The expiring token key credential provided to you by Streamline | Yes |
| token_secret | String (255) | The expiring token secret credential provided to you by Streamline | Yes |
| unit_id | Number | unit id (property) | Yes |
| startdate | String | start date of reservation (check-in) | Yes |
| enddate | String | end date (check-out) | Yes |
| occupants | String | occupants | Yes |
| occupants_small | Number | occupants small | Yes |
| pets | Number | pets | Yes |

#### Request Example

- XML
- JSON

```xml
<?xml version="1.0" encoding="UTF-8"?>
<methodCall>
 <methodName>VerifyPropertyAvailability</methodName>
 <params>
  <token_key>YOUR_TOKEN_KEY</token_key>
  <token_secret>YOUR_TOKEN_SECRET</token_secret>
  <unit_id>288531</unit_id>
  <startdate>01/10/2020</startdate>
  <enddate>01/17/2020</enddate>
  <occupants>2</occupants>
  <occupants_small>1</occupants_small>
  <pets>1</pets>
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
    <id>385256</id>
    <message></message>
  </data>
</Response>
```
