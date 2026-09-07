# Listing Index

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Listing-Index`

---
The Advertiser Listing Content index (or "Listing index") includes metadata and a link to the Listing content for each specified listing. You can utilize the <lastUpdatedDate> in any of the feeds to determine if an update is required at any time. It is highly recommended to retrieve and refresh these feeds at least once a day.

### ELEMENT <advertiserListingContentIndex>

This is the root element of the index and contains child elements that point to the content URLs.

```xml
<?xml version="1.0" encoding="UTF-8"?>
    <advertiserListingContentIndex>
    <documentVersion>{version}</documentVersion>
    <advertiser>{advertiser}</advertiser>
</advertiserListingContentIndex>
```

### Child Elements

| Name | Description |
|---|---|
| <documentVersion> | Listing XSD version. 4.2.1 Type: string |
| <advertiser> | Container that provides metadata about the property manager (PM) and a link to the listing content. Type: <advertiser> |

### ELEMENT <advertiser>

This element provides metadata about the property manager and an entry for each of the PM's listings.

```xml
<advertiser>
   <assignedId>{id}</assignedId>
   <listingContentIndexEntry>{entry}</listingContentIndexEntry>
   ...
</advertiser>
```

### Child Elements

| Name | Description |
|---|---|
| <assignedId> | Unique ID assigned to the PM within Streamline. Type: string |
| <listingContentIndexEntry> | Information about the PM's Listing index. Type: <listingContentIndexEntry> |

### ELEMENT <listingContentIndexEntry>

This element contains metadata about a listing and a link to the relevant content to be imported. The index entry refers to the static (listing) content for a property. This element is provided for each of the PM's listings.

```xml
<listingContentIndexEntry>
   <listingExternalId>{id}</listingExternalId>
   <active>{boolean}</active>
   <lastUpdatedDate>{dateTime}</lastUpdatedDate>
   <listingUrl>{url}</listingUrl>
</listingContentIndexEntry>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | Unique ID of this listing in the integration partner's system. Type: string |
| <active> | Whether the listing should be active and displayed on your sites. Each unit in your feed will show as "TRUE". Units are enabled into these feeds via the Travel Agent created for your distribution channels settings. You will not see a "FALSE" here. Type: boolean |
| <lastUpdatedDate> | Date and time when the referenced content was last updated. If the specified date is within the last three days, content is retrieved and processed (if the listing is active). Format is yyyy-MM-ddTHH:mm:ssZ. Type: date Time |
| <listingUrl> | URL to the Listing content for this listing. Type: string |

### Example

```xml
<advertiserListingContentIndex>
<documentVersion>4.2.1</documentVersion>
<advertiser>
<assignedId>3b3ghi_VRBO</assignedId>
<listingContentIndexEntry>
<listingExternalId>154139</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T16:24:10Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=154139&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>424301</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T03:03:25Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=424301&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>424304</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-23T11:18:24Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=424304&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>424303</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-23T11:18:24Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=424303&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>422494</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=422494&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>422491</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=422491&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>422490</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=422490&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>422489</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=422489&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
<listingContentIndexEntry>
<listingExternalId>422493</listingExternalId>
<active>true</active>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<listingUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/getListing?listing_id=422493&code=3b3ghi
</listingUrl>
</listingContentIndexEntry>
</advertiser>
</advertiserListingContentIndex>
```
