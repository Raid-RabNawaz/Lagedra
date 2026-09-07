# Lodging Rate Index

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Lodging-Rate-Index`

---
## Lodging Rate index

The Advertiser Lodging Rate index contains a list of the metadata and a link to the Lodging Rate content, which defines rates, taxes, and fees for each specified listing.

You should retrieve this index several times per day from your endpoint.

### ELEMENT <advertiserLodgingRateContentIndex>

The root element of the index.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<advertiserLodgingRateContentIndex>
<documentVersion>{version}</documentVersion>
<advertiser>{advertiser}</advertiser>
</advertiserLodgingRateContentIndex>
```

### Child Elements

| Name | Description |
|---|---|
| <documentVersion> | Value: 4.2.1 Type: string |
| <advertiser> | Container that provides metadata about the property manager (PM) and a link to the listing content. Type: <advertiser> |

### ELEMENT <advertiser>

This element provides metadata about the PM and a link to the Lodging Rate content.

```xml
<advertiser>
<assignedId>{id}</assignedId>
<lodgingRateContentIndexEntry>{entry}</lodgingRateContentIndexEntry>
...
</advertiser>
```

### Child Elements

| Name | Description |
|---|---|
| <assignedId> | Unique ID assigned to the PM by Streamline. Type: string |
| <lodgingRateContentIndexEntry> | Lodging Rate index entry for a particular PM. Type: <lodgingRateContentIndexEntry> |

### ELEMENT <lodgingRateContentIndexEntry>

Contains metadata about the rate data for a single unit type and includes a link to the relevant content to be imported. The index entry refers only to Lodging Rate content.

```xml
<lodgingRateContentIndexEntry>
	<listingExternalId>{id}</listingExternalId>
	<unitExternalId>{id}</unitExternalId>
	<lastUpdatedDate>{dateTime}</lastUpdatedDate>
	<lodgingRateContentUrl>{url}</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | Unique ID of this listing. Type: string |
| <unitExternalId> | Unique ID of the unit within Streamline. Type: string |
| <lastUpdatedDate> | Date and time when the referenced content was last updated. Format is yyyy-MM-ddTHH:mm:ssZ. Type: dateTime |
| <lodgingRatesContentUrl> | URL to the Lodging Rate content for this unit. Type: string |

### Example

```xml
<advertiserLodgingRateContentIndex>
<documentVersion>4.2.1</documentVersion>
<advertiser>
<assignedId>3b3ghi_VRBO</assignedId>
<lodgingRateContentIndexEntry>
<listingExternalId>154139</listingExternalId>
<unitExternalId>154139</unitExternalId>
<lastUpdatedDate>2020-01-24T16:24:10Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=154139&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>424301</listingExternalId>
<unitExternalId>424301</unitExternalId>
<lastUpdatedDate>2020-01-24T03:03:25Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=424301&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>424304</listingExternalId>
<unitExternalId>424304</unitExternalId>
<lastUpdatedDate>2020-01-23T11:18:24Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=424304&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>424303</listingExternalId>
<unitExternalId>424303</unitExternalId>
<lastUpdatedDate>2020-01-23T11:18:24Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=424303&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>422494</listingExternalId>
<unitExternalId>422494</unitExternalId>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=422494&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>422491</listingExternalId>
<unitExternalId>422491</unitExternalId>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=422491&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>422490</listingExternalId>
<unitExternalId>422490</unitExternalId>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=422490&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>422489</listingExternalId>
<unitExternalId>422489</unitExternalId>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=422489&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
<lodgingRateContentIndexEntry>
<listingExternalId>422493</listingExternalId>
<unitExternalId>422493</unitExternalId>
<lastUpdatedDate>2020-01-24T15:40:20Z</lastUpdatedDate>
<lodgingRateContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContent?listing_id=422493&code=3b3ghi
</lodgingRateContentUrl>
</lodgingRateContentIndexEntry>
</advertiser>
</advertiserLodgingRateContentIndex>
```
