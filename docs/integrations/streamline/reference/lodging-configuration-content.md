# Lodging Configuration Content

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Lodging-Configuration-Content`

---
This file defines the set of policies and rules that apply to a specific listing/unit combination. The Lodging Configuration content overrides settings in the Lodging Configuration defaults, which are provided in the Lodging Configuration index along with a link to this content file.

### ELEMENT <lodgingConfigurationContent>

```xml
<?xml version="1.0" encoding="UTF-8"?>
<lodgingConfigurationContent>
<listingExternalId>{id}</listingExternalId>
<unitExternalId>{id}</unitExternalId>
<lodgingConfiguration>{config}</lodgingConfiguration>
</lodgingConfigurationContent>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | Unique ID of the listing in Streamline. Type: string |
| <unitExternalId> | Unique ID of the listing in Streamline. Same as <listingExternalId> Type: string |
| <lodgingConfiguration> | Lodging configuration information for this particular listing/unit type. Type: <lodgingConfiguration> |

### ELEMENT <lodgingConfiguration>

```xml
<lodgingConfiguration>
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
</lodgingConfiguration>
```

### Child Elements

| Name | Description |
|---|---|
| <acceptedPaymentForms> | Payment forms that are permitted for this property manager. Type: <acceptedPaymentForms> |
| <bookingPolicy> | Booking policy for this PM - Instant Booking. With Instant Booking, the reservation is immediately confirmed and no action is required by the PM. We specify the <policy> child element to set the booking policy. By default, this will be "Instant". Type: object |
| <cancellationPolicy> | Cancellation policy for this PM. Type: <cancellationPolicy> |
| <checkInTime> | Check-in time for this PM. Time must use the 24-hour, HH:MM format. Example: 14:00. Type: string |
| <checkOutTime> | Checkout time for this PM. Time must use the 24-hour, HH:MM format. Example: 14:00. Type: string |
| <childrenAllowedRule> | Whether children are allowed in reservations for this PM. We specify the <allowed> child element, which is a boolean that indicates whether children are allowed. Valid values include true or false. We optionally specify the <note>child element, which is a string and describes the child policy (up to 50 characters). Type: object |
| <eventsAllowedRule> | Whether events are allowed in reservations for this PM. We specify the <allowed> child element, which is a boolean that indicates whether events are allowed. Valid values include true or false. PMs can optionally specify the <note>child element, which is a string and describes the events policy (up to 50 characters). Type: object |
| <locale> | Locale to use for all listings for this PM, such as en or fr. Format is the standard ISO language codes (two-characters). See Locale values for valid values. Type: string |
| <maximumOccupancyRule> | Maximum occupancy allowed for this PM. We specify the <guests> child element, which is an integer and specifies the maximum number of guests allows. PM's can also specify the <adults> child element, which is an integer and specifies the maximum number of adults allowed, and the <note> child element, which is a string that explains the maximum occupancy policy (up to 50 characters). Type: object |
| <minimumAgeRule> | Minimum age allowed for the primary renter for this PM's listings. We specify the <age> child element, which is an integer and specifies the minimum age allowed. We also specify the <note> child element, which is a string that explains the minimum age policy (up to 50 characters), such as if you want to set the minimum guest age to 25 years old. Type: object |
| <petsAllowedRule> | Pet policy for this PM. We specify the <allowed> child element, which is a boolean and specifies whether pets are allowed (true or false). PMs can also specify the <note> child element, which is a string that explains the pet policy (up to 50 characters). Type: object |
| <pricingPolicy> | Pricing policy for this PM - Guaranteed .Type: object |
| <rentalAgreementFile> | Rental agreement file for this PM if localized policies are not required. We specify the locale attribute to indicate the locale for which to use the rental agreement. See Locale values for valid values. We specify the <rentalAgreementPdfUrl> child element, which is a string and specifies the URL to the PM-hosted PDF of the rental agreement. Type: object |
| <smokingAllowedRule> | Smoking policy for this PM. We specify the <allowed> child element, which is a boolean and specifies whether smoking is allowed (true or false).We also specify the <note> child element, which is a string that explains the smoking policy (up to 50 characters). Type: object |

### ELEMENT <acceptedPaymentForms>

<paymentCardDescriptor>

```xml
<acceptedPaymentForms>
<paymentCardDescriptor>
<paymentFormType>{type}</paymentFormType>
<cardCode>{card}</cardCode>
<cardType>{type}</cardType>
</paymentCardDescriptor>
<paymentFormType>{type}</paymentFormType>
<paymentNote>{note}</paymentNote>
</acceptedPaymentForms>
```

### Child Elements

| Name | Description |
|---|---|
| <paymentCardDescriptor> | Information about the accepted credit card. These child elements are available: • <paymentFormType> - Specifies the form of payment for this type (CARD). This child element is required. • <cardCode> - Specifies the code associated with the provider of this card, such as VISA or MASTERCARD. See Card Code values. • <cardType> - Specifies the type of the card (CREDIT or DEBIT). * At least one <paymentCardDescriptor> or <paymentInvoiceDescriptor> element is specified. Type: object |

### ELEMENT <cancellationPolicy>

```xml
<cancellationPolicy>
<nightlyOverrides>{overrides}</nightlyOverrides>
<policy>{type}</policy>
</cancellationPolicy>
```

### Child Elements

| Name | Description |
|---|---|
| <nightlyOverrides> | Optional nightly overrides for the cancellation policy. Type: <nightlyOverrides> |
| <policy> | Enforcement level of the cancellation policy, such as STRICT or RELAXED. See Cancellation Policy Type values. Note: A rental agreement (PDF) is provided during checkout. When specifying the enforcement level here, select a level that most closely matches the terms in the rental agreement. If the terms do not match an available enforcement level, specify a stricter level. The rental agreement will remain the source of truth for cancellations that occur. Type: string |

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
| <stayIncludesNights> | One or more date ranges (up to 100) that specify the maximum and minimum dates for the nightly overrides. For each date range, specify the <range> child element, which contains the <max> and <min> child elements. <max> and <min> are dates in the format of yyyy-MM-dd. Type: object |
