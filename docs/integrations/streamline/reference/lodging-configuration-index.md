# Lodging Configuration Index

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Lodging-Configuration-Index`

---
The Advertiser Lodging Configuration Content index contains metadata, Lodging Configuration defaults, and a link to the Lodging Configuration content for each specified listing. The Lodging Configuration defaults define a set of policies and rules that are applied to every listing for a property manager.

- Booking policy - Instant Booking. The booking response will have a CONFIRMED reservation status at the time of booking, though the traveler’s credit card does not need to be charged instantly.
- Pricing policy - Guaranteed.
- Cancellation policy - Type (enforcement level) of cancellation policy, such as strict or relaxed. For a description of each supported type, see Listings enumerations.?

### ELEMENT <advertiserLodgingConfigurationContentIndex>

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<advertiserLodgingConfigurationContentIndex>
<documentVersion>{version}</documentVersion>
<advertiser>{advertiser}</advertiser>
</advertiserLodgingConfigurationContentIndex>
```

### Child Elements

| Name | Description |
|---|---|
| <documentVersion> | Listing XSD version. Value will be 4.2.1 Type: string |
| <advertiser> | Container that provides metadata about the property manager (PM) and a link to the lodging configuration content. Type: <advertiser> |

### ELEMENT <advertiser>

```xml
<advertiser>
<assignedId>{id}</assignedId>
<lodgingConfigurationDefaults>{defaults}</lodgingConfigurationDefaults>
<lodgingConfigurationContentIndexEntry>{entry}</lodgingConfigurationContentIndexEntry>
...
</advertiser>
```

### Child Elements

| Name | Description |
|---|---|
| <assignedId> | Unique ID assigned to the PM by Streamline. Type: string |
| <lodgingConfigurationDefaults> | PM-level defaults to be applied across all integrated listings for this PM. Type: <lodgingConfigurationDefaults> |
| <lodgingConfigurationContentIndexEntry> | URL to the listing-level overrides for lodging configurations. Type: <lodgingConfigurationContentIndexEntry> |

### ELEMENT <lodgingConfigurationDefaults>

- Accepted payment forms
- Minimum and maximum occupancy
- Minimum age of the primary traveler
- Booking policy (Instant Booking )
- Pets policy
- Smoking policy
- Cancellation policy
- Rental agreements
- Pricing policy (Guaranteed)

```xml
<lodgingConfigurationDefaults>
<acceptedPaymentForms>{paymentForms}</acceptedPaymentForms>
<bookingPolicy>
<policy>{policy}</policy>
</bookingPolicy>
<cancellationPolicy>{policy}</cancellationPolicy>
<checkInTime>{time}</checkInTime>
<checkOutTime>{time}</checkOutTime>
<childrenAllowedRule>
<allowed>{boolean}</allowed>
<note>{note}</note>
</childrenAllowedRule>
<eventsAllowedRule>
<allowed>{boolean}</allowed>
<note>{note}</note>
</eventsAllowedRule>
<lastUpdatedDate>{dateTime}</lastUpdatedDate>
<locale>{locale}</locale>
<maximumOccupancyRule>
<adults>{number}</adults>
<guests>{number}</guests>
<note>{note}</note>
</maximumOccupancyRule>
<minimumAgeRule>
<age>{age}</age>
<note>{note}</note>
</minimumAgeRule>
<petsAllowedRule>
<allowed>{boolean}</allowed>
<note>{note}</note>
</petsAllowedRule>
<pricingPolicy>
<policy>{policy}</policy>
</pricingPolicy>
<rentalAgreementFile locale="{locale}">
<rentalAgreementPdfUrl>{url}</rentalAgreementPdfUrl>
</rentalAgreementFile>
<smokingAllowedRule>
<allowed>{boolean}</allowed>
<note>{note}</note>
</smokingAllowedRule>
</lodgingConfigurationDefaults>
```

### Child Elements

| Name | Description |
|---|---|
| <acceptedPaymentForms> | Default payment forms that are permitted for this PM. Type: <acceptedPaymentForms> |
| <bookingPolicy> | Default booking policy for this PM - Instant Booking or Quote and Hold. With Instant Booking, the reservation is immediately confirmed and no action is required by the PM. With Quote and Hold, the reservation is not confirmed until the PM manually confirms the reservation in their software. You will see QUOTEHOLD or INSTANT. A unique parameter is required in the BookingRequest if the PM does not wish to receive INSTANT bookings which load into Streamline as confirmed/booked reservation. Type: object |
| <cancellationPolicy> | Default cancellation policy for this PM. Type: <cancellationPolicy> |
| <checkInTime> | Default check-in time for this PM. Time will use the 24-hour, HH:MM format. Example: 14:00. Type: string |
| <checkOutTime> | Default checkout time for this PM. Time will use the 24-hour, HH:MM format. Example: 14:00. Type: string |
| <childrenAllowedRule> | Whether children are allowed in reservations for this PM. PMs must specify the <allowed> child element, which is a boolean that indicates whether children are allowed. Valid values include true or false. They can optionally specify the <note>child element and describes the child policy (up to 50 characters). Type: object |
| <eventsAllowedRule> | Whether events are allowed in reservations for this PM. PMs must specify the <allowed> child element, which is a boolean that indicates whether events are allowed. Valid values include true or false. They can optionally specify the <note>child element, which is a string and describes the events policy (up to 50 characters). Type: object |
| <lastUpdatedDate> | The date and time when the Lodging Configuration defaults were last updated. Format: yyyy-MM-ddTHH:mm:ssZ Type: dateTime |
| <locale> | Default locale to use for all listings for this PM, such as en or fr. Format is the standard ISO language codes (two-characters). See Locale values for valid values. Type: string |
| <maximumOccupancyRule> | Default maximum occupancy allowed for this PM. We specify the <guests>child element, which is an integer and specifies the maximum number of guests allowed. We also specify the <adults> child element, which is an integer and specifies the maximum number of adults allowed, and the <note> child element, which is a string that explains the maximum occupancy policy (up to 50 characters). Type: object |
| <minimumAgeRule> | Default minimum age allowed for the primary renter for this PM's listings. We specify the <age> child element, which is an integer and specifies the minimum age allowed. PMs can also specify the <note> child element, which is a string that explains the minimum age policy (up to 50 characters), such as if they want to set the minimum guest age to 25 years old. Type: object |
| <petsAllowedRule> | Default pet policy for this PM. We specify the <allowed> child element, which is a boolean and specifies whether pets are allowed (true or false). PMs can also specify the <note> child element, which is a string that explains the pet policy (up to 50 characters). Type: object |
| <pricingPolicy> | Default pricing policy for a PM - Guaranteed Type: object |
| <rentalAgreementFile> | Default rental agreement file for this PM. We specify the locale attribute to indicate the locale for which to use the rental agreement. See Locale values for valid values. PMs must specify the <rentalAgreementPdfUrl> child element, which is a string that specifies the URL to the PM-hosted PDF of the rental agreement. Type: object |
| <smokingAllowedRule> | Default smoking policy for this PM. PMs must specify the <allowed> child element, which is a boolean and specifies whether smoking is allowed (true or false). They can also specify the <note> child element, which is a string that explains the smoking policy (up to 50 characters). Type: object |

### ELEMENT <acceptedPaymentForms>

<paymentCardDescriptor>

```xml
<acceptedPaymentForms>
<paymentCardDescriptor>
	<paymentFormType>{type}</paymentFormType>
	<cardCode>{card}</cardCode>
	<cardType>{type}</cardType>
</paymentCardDescriptor>
</acceptedPaymentForms
```

### Child Elements

| Name | Description |
|---|---|
| <paymentCardDescriptor> | Information about the accepted credit card. These child elements are available: • <paymentFormType> - Specifies the form of payment for this type (CARD). This child element is required. • <cardCode> - Specifies the code associated with the provider of this card, such as VISA or MASTERCARD. This child element is required. See Card Code values. • <cardType> - Specifies the type of the card (CREDIT or DEBIT). This child element is required. * At least one <paymentCardDescriptor> Type: object |

### ELEMENT <cancellationPolicy>

```xml
<cancellationPolicy>
<nightlyOverrides>{overrides}</nightlyOverrides>
<policy>{type}</policy>
</cancellationPolicy>A
```

### Child Elements

| Name | Description |
|---|---|
| <nightlyOverrides> | Optional nightly overrides for the cancellation policy. Type: <nightlyOverrides> |
| <policy> | Enforcement level of the cancellation policy, such as STRICT or RELAXED. See Cancellation Policy Type values. Note: A rental agreement (PDF) can be provided during checkout. Type: string |

### ELEMENT <nightlyOverrides>

```xml
<nightlyOverrides>
<override>
	<policy>{policy}</policy>
	<stayIncludesNights>
	<range>
	<max>{date}</max>
	<min>{date}</min>
	</range>
	...
	</stayIncludesNights>
</override>
...
</nightlyOverrides>
```

### Child Elements

| Name | Description |
|---|---|
| <policy> | Enforcement level of the cancellation policy, such as STRICT or RELAXED. See Cancellation Policy Type values. Type: string |
| <stayIncludesNights> | One or more date ranges (up to 100) that specify the maximum and minimum dates for the nightly overrides. For each date range, we specify the <range> child element, which contains the <max> and <min> child elements. <max> and <min> are dates in the format of yyyy-MM-dd. Type: object |

### ELEMENT <lodgingConfigurationContentIndexEntry>

```xml
<lodgingConfigurationContentIndexEntry>
<listingExternalId>{id}</listingExternalId>
<unitExternalId>{id}</unitExternalId>
<lastUpdatedDate>{date}</lastUpdatedDate>
<lodgingConfigurationContentUrl>{url}</lodgingConfigurationContentUrl>
</lodgingConfigurationContentIndexEntry>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | Unique ID of the PM in the Streamline system. Type: string |
| <unitExternalId> | Unique ID of the rental unit in the integration partner's system. Type: string |
| <lastUpdatedDate> | Date and time when the referenced content was last updated. Required format is yyyy-MM-ddTHH:mm:ssZ. Type: dateTime |
| <lodgingConfigurationContentUrl> | URL that points to the Lodging Configuration content for this particular listing/unit type. Type: string |
