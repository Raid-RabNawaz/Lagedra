# GetTokenExpiration

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/GetTokenExpiration`

---
### GetTokenExpiration

Returns the expiration date and company id when passing a set of tokens.

| Name | Data Type | Description | Required? |
|---|---|---|---|
| token_key | String (255) | The expiring token key credential provided to you by Streamline | Yes |
| token_secret | String (255) | The expiring token secret credential provided to you by Streamline | Yes |

#### XML Request Example

```xml
<?xml version="1.0" encoding="UTF-8"?>
<methodCall>
 <methodName>GetTokenExpiration</methodName>
 <params>
  <token_key>YOUR_TOKEN_KEY</token_key>
  <token_secret>YOUR_TOKEN_SECRET</token_secret>
 </params>
</methodCall>}
```

#### XML Response Example

```xml
<?xml version="1.0" ?>
<Response>
	<data>
		<id>329</id>
		<expiration>03/31/2020</expiration>
	</data>
</Response>
```

#### JSON Request Example

```json
{
    "methodName": "GetTokenExpiration",
    "params": {
        "token_key": "YOUR_TOKEN_KEY",
        "token_secret": "YOUR_TOKEN_SECRET"
    }
}
```

#### JSON Response Example

```json
{
  "Response": {
    "data": {
      "id": "329",
      "expiration": "03/31/2020"
    }
  }
}
```

*You can ignore the "id" in the response.
