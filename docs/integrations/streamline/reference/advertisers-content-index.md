# Advertisers Content Index

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Advertisers-Content-Index`

---
The Advertisers Content index is an index of all Property Mangers within Streamline that have specifically allowed you to distribute and book their properties on your platform.

Example Content Index: https://web.streamlinevrs.com/partner/partnersName/4.2.1/getAdvertisersContentIndex IMPORTANT: You will only see a PM in your feed once they have used our self-service onboarding wizard to generate API tokens which will be emailed to you as well as opt in any properties they wish to distribute. Taxes and Fees are also a key component to initial setup and onboarding.

This index contains links to the Listing, Lodging Configuration, Lodging Rate, and Unit Availability index for each specified PM and for each unit they opt-in to the integration.

*Note: "Advertiser" is commonly associated with a "Property Manager" throughout this documentation.

### ELEMENT <advertisersContentIndex>

```xml
<?xml version="1.0" encoding="UTF-8"?>
<advertisersContentIndex>
    <documentVersion>{version}</documentVersion>
    <advertiserIndexEntry>{entry}</advertiserIndexEntry>
    ...
</advertisersContentIndex>
```

#### Child Elements

| Name | Description |
|---|---|
| <documentVersion> | Listing XSD version. 4.2.1 Type: string |
| <advertiserIndexEntry> | Container for the content URLs for this advertiser. Type: <advertiserIndexEntry> |

### ELEMENTS <advertiserIndexEntry>

```xml
<advertiserIndexEntry>
 <advertiserAssignedId>{id}</advertiserAssignedId>
 <advertiserName>{name}</advertiserName>
 <advertiserListingContentIndexUrl>{url}</advertiserListingContentIndexUrl>
 <advertiserLodgingConfigurationContentIndexUrl>{url}</advertiserLodgingConfigurationContentIndexUrl>
 <advertiserLodgingRateContentIndexUrl>{url}</advertiserLodgingRateContentIndexUrl>
 <advertiserUnitAvailabilityContentIndexUrl>{url}</advertiserUnitAvailabilityContentIndexUrl>
</advertiserIndexEntry>
```

#### Child Elements

| Name | Description |
|---|---|
| <advertiserAssignedId> | Unique ID of the property manager (PM) We specify up to 255 characters. Type: string |
| <advertiserName> | Name of the individual PM. This field is informational only. Type: string |
| <advertiserListingContentIndexUrl> | URL to the Listing index. Type: string |
| <advertiserLodgingConfigurationContentIndexUrl> | URL to the Lodging Configuration index. Type: string |
| <advertiserLodgingRateContentIndexUrl> | URL to the Lodging Rate index. Type: string |
| <advertiserUnitAvailabilityContentIndexUrl> | URL to the Unit Availability index. Type: string |

### Example

```xml
<advertisersContentIndex>
<documentVersion>4.2.1</documentVersion>
<advertiserIndexEntry>
<advertiserAssignedId>3b3ghi_VRBO</advertiserAssignedId>
<advertiserName>
Gueststream Sandbox
</advertiserName>
<advertiserListingContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/listingContentIndex?advertiser_id=3b3ghi
</advertiserListingContentIndexUrl>
<advertiserLodgingConfigurationContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingConfigurationContentIndex?advertiser_id=3b3ghi
</advertiserLodgingConfigurationContentIndexUrl>
<advertiserLodgingRateContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContentIndex?advertiser_id=3b3ghi
</advertiserLodgingRateContentIndexUrl>
<advertiserUnitAvailabilityContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailabilityContentIndex?advertiser_id=3b3ghi
</advertiserUnitAvailabilityContentIndexUrl>
</advertiserIndexEntry>
<advertiserIndexEntry>
<advertiserAssignedId>479ghi_VRBO</advertiserAssignedId>
<advertiserName>zzSue</advertiserName>
<advertiserListingContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/listingContentIndex?advertiser_id=479ghi
</advertiserListingContentIndexUrl>
<advertiserLodgingConfigurationContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingConfigurationContentIndex?advertiser_id=479ghi
</advertiserLodgingConfigurationContentIndexUrl>
<advertiserLodgingRateContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContentIndex?advertiser_id=479ghi
</advertiserLodgingRateContentIndexUrl>
<advertiserUnitAvailabilityContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailabilityContentIndex?advertiser_id=479ghi
</advertiserUnitAvailabilityContentIndexUrl>
</advertiserIndexEntry>
<advertiserIndexEntry>
<advertiserAssignedId>737ghi_VRBO</advertiserAssignedId>
<advertiserName>Owner Direct Az (ownerdirectaz.com)</advertiserName>
<advertiserListingContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/listingContentIndex?advertiser_id=737ghi
</advertiserListingContentIndexUrl>
<advertiserLodgingConfigurationContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingConfigurationContentIndex?advertiser_id=737ghi
</advertiserLodgingConfigurationContentIndexUrl>
<advertiserLodgingRateContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContentIndex?advertiser_id=737ghi
</advertiserLodgingRateContentIndexUrl>
<advertiserUnitAvailabilityContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailabilityContentIndex?advertiser_id=737ghi
</advertiserUnitAvailabilityContentIndexUrl>
</advertiserIndexEntry>
<advertiserIndexEntry>
<advertiserAssignedId>781ghi_VRBO</advertiserAssignedId>
<advertiserName>zzzJackie</advertiserName>
<advertiserListingContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/listingContentIndex?advertiser_id=781ghi
</advertiserListingContentIndexUrl>
<advertiserLodgingConfigurationContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingConfigurationContentIndex?advertiser_id=781ghi
</advertiserLodgingConfigurationContentIndexUrl>
<advertiserLodgingRateContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/lodgingRateContentIndex?advertiser_id=781ghi
</advertiserLodgingRateContentIndexUrl>
<advertiserUnitAvailabilityContentIndexUrl>
https://web.streamlinevrs.com/partner/streampal/4.2.1/unitAvailabilityContentIndex?advertiser_id=781ghi
</advertiserUnitAvailabilityContentIndexUrl>
</advertiserIndexEntry>
</advertisersContentIndex>
```
