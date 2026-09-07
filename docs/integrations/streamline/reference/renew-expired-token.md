# RenewExpiredToken

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/RenewExpiredToken`

---
### RenewExpiredToken

Used for renewing your and key simultaneously.

| Name | Data Type | Description | Required? |
|---|---|---|---|
| token_key | String (255) | The expiring token key credential provided to you by Streamline | Yes |
| token_secret | String (255) | The expiring token secret credential provided to you by Streamline | Yes |

#### Request Example

- XML
- JSON

```xml
<?xml version="1.0" encoding="UTF-8"?>
<methodCall>
 <methodName>RenewExpiredToken</methodName>
 <params>
  <token_key>YOUR_TOKEN_KEY</token_key>
  <token_secret>YOUR_TOKEN_SECRET</token_secret>
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
    <token_key>YOUR_TOKEN_KEY</token_key>
    <token_secret>YOUR_TOKEN_SECRET</token_secret>
    <startdate>11/20/2019</startdate>
    <enddate>02/20/2020</enddate>
  </data>
</Response>
```
