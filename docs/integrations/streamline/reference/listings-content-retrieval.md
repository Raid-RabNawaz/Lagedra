# Listings: Content Retrieval

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings`

---
Listings Reference

Listings integration captures all of the content needed to display rental units on your channel. The current version of the Listings integration is 4.2.1 Index files - Provide metadata and URLs so that you can retrieve the corresponding content files.

- Advertisers index
- Listing index
- Lodging Configuration index (also includes default policies and rules for all listings)
- Lodging Rate index
- Unit Availability index

Content files - Provides all settings and content for rental unit listings.

- Listing content - Listing information including the headline, description, photo, and amenities.
- Lodging Configuration content - Policies and rules that are applied to a listing/unit combination, which override default settings in the Lodging Configuration index.
- Lodging Rate content - Nightly rates, fees, discounts, and taxes used to calculate the rental amount charged to travelers.
- Unit Availability content - Availability dates for a particular unit type and rules that must be enforced for the stay

Here is an overview of how the files reference each other: The Advertisers index contains links to the Listing, Lodging Configuration, Lodging Rate, and Unit Availability index for each specified PM. On each PM's endpoint, each index file provides a link to the corresponding content file, so that you can retrieve the content for the listing.

You will first retrieve the index of the targeted content. That content could be listing static content, rates, and availability. Each of these feeds are set up to run independently from one another.

You will also see the specific name of the property management company associated within <advertiserName>.

Example Advertiser ID: 3b3ghi

```xml
<advertisersContentIndex>
<documentVersion>4.2.1</documentVersion>

<advertiserIndexEntry>
<advertiserAssignedId>3b3ghi</advertiserAssignedId>
<advertiserName>Gueststream Sandbox</advertiserName>

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

...

</advertisersContentIndex>
```

The indexes contain some basic meta data about the content/listing to be updated. The most important pieces of content in this index are:

- Listing/unit identifiers
- Last Update Date
- URL to retrieve the content

Having this information in the “index” allows you to optimize your integration to do any of the following:

1. Only run the integration for a single property
2. Only run the integration for listings which have changed in a particular timeframe
3. Run several jobs in parallel based on specific index ranges to accelerate synchronization
4. Run a full re-sync of data on demand
5. Do summary reporting purely based on the index

### Content Retrieval

Once you have retrieved the index and the scope is defined, you will sequentially make calls to retrieve and process the content.
