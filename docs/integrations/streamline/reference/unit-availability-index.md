# Unit Availability Index

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Unit-Availability-Index`

---
The Advertiser Unit Availability Content index (or "Unit Availability index") contains metadata and a link to the Unit Availability content for each specified listing. You should retrieve this index several times per day from your endpoint. If the <lastUpdatedDate> in this index is within three days of the current date, you should use the GET method to retrieve Unit Availability content files.

### ELEMENT <advertiserUnitAvailabilityContentIndex>

This is the root element of the index.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<advertiserUnitAvailabilityContentIndex>
	<documentVersion>{version}</documentVersion>
	<advertiser>{advertiser}</advertiser>
</advertiserUnitAvailabilityContentIndex>
```

### Child Elements

| Name | Description |
|---|---|
| <documentVersion> | Listing XSD version. 4.2.1 Type: string |
| <advertiser> | Container that provides metadata about the property manager and a link to the unit availability content. Type: <advertiser> |

### ELEMENT <advertiser>

This element provides metadata about the advertiser (property manager) and a link to the unit availability content.

```xml
<advertiser>
<assignedId>{id}</assignedId>
<unitAvailabilityContentIndexEntry>{entry}</lunitAvailabilityContentIndexEntry>
...
</advertiser>
```

### Child Elements

| Name | Description |
|---|---|
| <assignedId> | Unique ID globally assigned in Streamline when a unit is created. Type: string |
| <unitAvailabilityContentIndexEntry> | Unit availability index entry for a property manager. Type: <unitAvailabilityContentIndexEntry> |

### ELEMENT <unitAvailabilityContentIndexEntry>

This element contains metadata about unit availability and includes a link to the relevant content to be imported. The index entry refers only to availability information.

```xml
<unitAvailabilityContentIndexEntry>
<listingExternalId>{id}</listingExternalId>
<unitExternalId>{id}</unitExternalId>
<lastUpdatedDate>{dateTime}</lastUpdatedDate>
<unitAvailabilityContentUrl>{url}</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | The unique ID of this listing in Streamline. Type: string |
| <unitExternalId> | Unique ID of this unit in Streamline. Same as <listingExternalId> Type: string |
| <lastUpdatedDate> | Date and time when the referenced content was last updated. Format is yyyy-MM-ddTHH:mm:ssZ. Type: dateTime |
| <unitAvailabilityContentUrl> | URL to the unit availability content for this unit. Type: string |

### EXAMPLE

```xml
<advertiserUnitAvailabilityContentIndex>
<documentVersion>4.2.1</documentVersion>
<advertiser>
<assignedId>3b3ghi_VRBO</assignedId>
<unitAvailabilityContentIndexEntry>
<listingExternalId>154139</listingExternalId>
<unitExternalId>154139</unitExternalId>
<lastUpdatedDate>2020-01-24T15:24:10Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=154139&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>424301</listingExternalId>
<unitExternalId>424301</unitExternalId>
<lastUpdatedDate>2020-01-24T02:03:25Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=424301&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>424304</listingExternalId>
<unitExternalId>424304</unitExternalId>
<lastUpdatedDate>2020-01-23T10:18:24Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=424304&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>424303</listingExternalId>
<unitExternalId>424303</unitExternalId>
<lastUpdatedDate>2020-01-23T10:18:24Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=424303&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>422494</listingExternalId>
<unitExternalId>422494</unitExternalId>
<lastUpdatedDate>2020-01-24T14:40:20Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=422494&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>422491</listingExternalId>
<unitExternalId>422491</unitExternalId>
<lastUpdatedDate>2020-01-24T14:40:20Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=422491&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>422490</listingExternalId>
<unitExternalId>422490</unitExternalId>
<lastUpdatedDate>2020-01-24T14:40:20Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=422490&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>422489</listingExternalId>
<unitExternalId>422489</unitExternalId>
<lastUpdatedDate>2020-01-24T14:40:20Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=422489&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
<unitAvailabilityContentIndexEntry>
<listingExternalId>422493</listingExternalId>
<unitExternalId>422493</unitExternalId>
<lastUpdatedDate>2020-01-24T14:40:20Z</lastUpdatedDate>
<unitAvailabilityContentUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailability?listing_id=422493&code=3b3ghi
</unitAvailabilityContentUrl>
</unitAvailabilityContentIndexEntry>
</advertiser>
</advertiserUnitAvailabilityContentIndex>
```
