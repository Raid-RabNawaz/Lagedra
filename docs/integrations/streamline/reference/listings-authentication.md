# Listings: Authentication

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Listings-Authentication`

---
## Listings-Authentication

The listing endpoints provided use basic authentication. It will be set in the HTTP Authorization header of requests you send to those endpoint(s).

Your Engagement Manager will provide a basic authentication username and password during onboarding.

All listing indexes require basic authentication. Include the access token in every request; no anonymous access is allowed.

The basic authentication credentials are global and used across all integrated property managers contained within your xml feeds. These credentials will not change unless requested.

If authentication fails, we return a status code "E0034" (Invalid username or password)

```xml
<Response>    <status>        <code>E0034</code>        <description>Invalid username or password. Please contact with our managers..</description>    </status></Response>
```

Please submit a support ticket if you receive this error: https://partner.streamlinevrs.com/support/contact_support
