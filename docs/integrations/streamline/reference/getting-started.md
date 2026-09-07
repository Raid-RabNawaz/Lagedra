# Getting Started (Partner OLB)

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/getting_started/getting-started-partner-olb`

---
## Welcome to the Streamline Partner X Portal!

Streamline’s API allows Streamline customers and 3rd party partners programmatic access to data contained in the Streamline application. Through our connected API customers and 3rd party partners can build connected websites, innovative applications, and can connect to external outside technologies.

### Token Key and Secret

All API methods require a token key and token secret. Token key and secret are obtained from the Streamline connected customer who has allowed your company access to their API. If you work with more than one client, you will have a unique token set for each property manager you work with. Our token sets are rate limited and IP restricted.

In order to deliver maximum security, each API user is required to provide and obtain the following three authentication items:

1. Provide Allowed IP Addresses: Access to our API is restricted by IP Address.
2. Addressing Method: IPv4 / IPv6
3. Token Key: This key will provide access to utilize the API paired with the secret key.
4. Token Secret Key: This key allows Streamline to associate your API request with a specific company.

### Token Expiration

As an added level of security, you will need to refresh your token set at predefined intervals. By default, it is set to 90 days. You will utilize the RenewExpiredToken method for renewing the set.

### API Call Request Limiting

Our token sets limit default to 100 requests per minute. If you need the ability to process more requests per minute, please contact our support team and offer justification on why a higher rate limit request is required. Our expectations is that application which make more than 100 requests per minute implement local storage of data to reduce the need for additional API calls.

### API Endpoints

JSON ENDPOINT https://web.streamlinevrs.com/api/json XML ENDPOINT https://web.streamlinevrs.com/api/1.1 Each of our endpoints request a customer specific token key and secret to gain authorization.

### API Allowed IP Address(es) IPv4 / IPv6

The IP address of your server must be included in our IP allowed list. Your IP allowed list may be an individual IP address, any number of IP address or any number of IP ranges as specified by the CIDR (Classless Inter-Domain Routing) convention. You can view and manage your IP allowed list within this portal on the Administration tab.

A new modification has been implemented on our API Endpoints to enable additional secure services and IPv6 protocol compatibility.

- If in your environment is not used IPv6 disregard this message.
- If your software has preference to use IPv6 while doing requests to our API please verify your whitelist Ip Addresses to include the IPv6 IP's in the IP Allowed List within this portal.
- API Tokens using our API Endpoints compatible with IPv6 may require an update to your IP Allowed list to include IPv6 Addresses.
- API requests will be monitored and some IPv6 Whitelists could be updated to avoid issues. We will send direct communication with users detected using possible misconfiguration.

### API Method Usage

The method name is included in the API request body under the variable "methodName".

```json
{
    "methodName": "GetPreReservationPrice"
}
```

### API Parameters

```
"params": {
        "reservation_id": "111211"
    }
```

### API Example Call

```json
{
    "methodName": "VerifyPropertyAvailability",
    "params": {
        "token_key": "YOUR_TOKEN_KEY",
        "token_secret": "YOUR_TOKEN_SECRET",
        "unit_id": "288515",
        "startdate": "08/01/2022",
        "enddate": "08/03/2022"
    }
}
```

​
