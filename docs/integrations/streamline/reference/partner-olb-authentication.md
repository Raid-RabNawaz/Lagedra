# Partner OLB: Authentication

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/Partner-OLB-API-Authentication`

---
### Partner OLB API-Authentication

All direct Streamline API methods used with the Partner OLB portion of this integration require a token_key and token_secret for authentication.

These tokens are linked to your IP allowed list within this portal: https://partner.streamlinevrs.com/admin_pages/allowed_ips

A token set will only work if the IP calling the method is added to your IP allowed list.

Token key and secret are obtained from the Streamline connected property manager who has allowed your company access to their API for the sake of this integration. If you work with more than one client, you will have a unique token set for each property manager

Streamline Marketplace Preferred Partners will receive token keys via our client self-service on boarding process. In this case token keys will be emailed to you.

```
"params": {
      "token_key": "YOUR_TOKEN_KEY",
```

```
"token_secret": "YOUR_TOKEN_SECRET",
```
