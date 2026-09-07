# API Token Renewal

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/API-Token-Renewal`

---
### API Token Renewal

The

RenewExpiredToken

method is used for renewing your "token_key" and "token_secret" keys simultaneously. Token sets are unique to each Property Management company you work with in Streamline. If you are a third party vendor and working with multiple clients, you will have a unique token set for each company. Each token se twill need to be renewed individually.

- Once you renew your token, your old token set will become invalid.
- The response will return you with a new token and token secret key for the Property Management companies' system you are renewing it for.
- This method must be called from an IP which is white-listed to the current token set you are trying to renew.
- Token sets are unique to each 'Property Management' company.
- Token sets are valid for 90 days.
- You must renew your tokens within 90 days or they will expire.
- You can renew an expired token with the last valid token.
- When you renew a token, a new token_key and token_secret key will be returned in the response which you must capture and use as the token.
- When you generate a new token it will be valid for another 90 days from the time it was renewed.

*You manage your IP allowed list within this portal if you are an admin user: https://partner.streamlinevrs.com/admin_pages/allowed_ips
