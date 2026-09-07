# Lodging Rate Content

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Lodging-Rate-Content`

---
## Lodging Rate content

The Lodging Rate Content files specifies base rates, fees, and taxes that are used to calculate the rental amount charged to travelers. We specify a payment schedule and any fees that are collected during the rental period. When reviewing this XML file, be aware of the following implementation details:

- Separation of rates, fees, and taxes:Fees and taxes are not built into rates.
- Currency for rates and fees: USD is the only currency supported.

- Date ranges:We specify the last date in the range first (<max>) and then specify the first date in the range (<min>).We group consecutive date ranges together to simplify the XML for Nightly Rate tables.For LoS pricing, we hardcode 180 nights of pricing per date of LoS rates defined.

### Proper Use of Date Range Example

```xml
<range>
<max>2017-02-28</max>
<min>2017-01-01</min>
</range>
```

### ELEMENT <lodgingRateContent>

This is the root element of the Lodging Rate content.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<lodgingRateContent>
	<listingExternalId>{id}</listingExternalId>
	<unitExternalId>{id}</unitExternalId>
	<lodgingRate>{rates}</lodgingRate>

</lodgingRateContent>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | Unique external ID of the listing within Streamline. Also knows as a unit id. These are global across the entire Streamline ecosystem. Type: string |
| <unitExternalId> | Unique external ID of the unit. Same as <listingExternalId> Type: string |
| <lodgingRate> | Nightly rate pricing for the listing, including fees, taxes, discounts, and payment schedule information. * Either <lodgingRate> or <lodgingRateLos> is required, but only one is allowed for a listing. Type: <lodgingRate> |
| <lodgingRateLos> | Length-of-stay (LoS) pricing that provides total rental rates based on the arrival date, duration of a guest's stay, and occupancy. * Either <lodgingRate> or <lodgingRateLos> is required, but only one is allowed for a listing. |

### ELEMENT <lodgingRate>

This element specifies the nightly rate, fee, tax, and payment schedule information for the listing. We feed up to 3 years of rate data if defined per unit.

```xml
<lodgingRate>
  <currency>{code}</currency>
  <discounts>{discounts}</discounts>
  <externalId>{id}</externalId>
  <externalUpdateDate>{dateTime}</externalUpdateDate>
  <fees>{fees}</fees>
  <language>{code}</language>
  <nightlyRates>{rates}</nightlyRates>
  <paymentSchedule>{schedule}</paymentSchedule>
  <taxRules>{rules}</taxRules>
</lodgingRate>
```

### Child Elements

| Name | Description |
|---|---|
| <currency> | USD is the only currency supported currently. Type: string |
| <discounts> | Discounts offered for the listing. Does not reflect in the response of GetPreReservationPrice API. We only support <percentOfRentDiscounts>. |
| <externalId> | Unique ID of the rate. This ID is provided for information purposes only. Type: string |
| <externalUpdateDate> | Date and time when this rate was updated in the integration partner’s system. Format is yyyy-MM-ddTHH:mm:ss.s. Type: dateTime |
| <fees> | Additional fees associated with the listing. Type: <fees> |
| <language> | Two-character ISO language code of the locale that is used by the listing's host site. Type: string |
| <nightlyRates> | Nightly rental amounts for this listing. Type: <nightlyRates> |
| <paymentSchedule> | Payment schedule required for this listing. Type: <paymentSchedule> |
| <taxRules> | Taxes collected for the listing. Type: <taxRules> |

### ELEMENT <lodgingRateLos>

The <lodgingRateLos> element defines length-of-stay (LoS) rates, which are computed total rental rates based on the arrival date, duration of stay, and occupancy. We can also specify fees, taxes and payment schedule information that are applied to the rates. The maximum length of stay using LoS data is 180 days. We default to always passing 180 days of pricing if defined. If you see less than 180 days defined for a date the PM's rates have not been defined for that period. Availability is provided separately in the Unit Availability content file.)

Using the <lodgingRateLos> element, we show you the following:

- Up to 180 explicit stay rates, which provides one pre-calculated room rent rate for each combination of occupancy levels and check-in date. These rates are included in the night price fields of each <lengthOfStayBaseRentRow> element.
- Up to 735 check-in dates (with 10 occupancy levels for each) could be present if configured by the PM as such. This means that up to 73550 <lengthOfStayBaseRent> elements could be present per unit.
- Not all units within the same PM are required to use LoS. It can be a mixture of the Nightly and LOS rates across properties.

```xml
<lodgingRateLos>
  <currency>{code}</currency>  <externalId>{id}</externalId>  <externalUpdateDate>{dateTime}</externalUpdateDate>  <fees>{fees}</fees>  <language>{code}</language>  <lengthOfStayBaseRent>    <lengthOfStayBaseRentRow>{rate_schedule}</lengthOfStayBaseRentRow>    ...  </lengthOfStayBaseRent>  <paymentSchedule>{schedule}</paymentSchedule>  <taxRules>{rules}</taxRules></lodgingRateLos>
```

### Child Elements

| Name | Description |
|---|---|
| <currency> | USD Type: string |
| <externalID> | Unique ID of the property within Streamline. Type: string |
| <externalUpdateDate> | Date and time when this rate was updated in the Property Managers Streamline system. Format is yyyy-MM-ddTHH:mm:ss.s. Type: date Time |
| <fees> | Additional fees associated with the listing. Type: <fees> |
| <language> | Two-character ISO language code of the locale that is used by the listing host. Type: string |
| <lengthOfStayBaseRent> | Explicit length-of-stay rates for the listing, for every combination of stay date, occupancy level, and check-in date. This element can provide p to 7350 <lengthOfStayBaseRentRow> child elements, which is two year's of data and allows us to specify up to 735 check-in dates with 10 occupancy levels for each. The format of the string that we specify in <lengthOfStayBaseRentRow>:{check-in date},{occupancy},{1-night price},{2-nights price},{3-nights price},....{check-in date}: field - We specify this format: yyyy-mm-dd. PMs can specify the same check-in date up to 10 times, to include pricing for different occupancy levels. However, this is rare and rows that duplicate dates and occupancy are not supported: If specified, the last date's prices should be used.{occupancy} field - Maximum number of guests (1-99) for the specified prices. For example, if a PM specifies 4 in the {occupancy} field, 1-4 guests can be included.{#-night price} fields - Each field provides the total rental rate (0-999999999.99) for the specified number of nights, excluding taxes and fees. 0 in a price field indicates that the rate is not quotable/bookable for that length of stay. Two decimal places are specified and commas are excluded if the price is more than 999.We specify 180 {#-night price} fields.Note: We do not specify only one night of pricing: instead, we specify two nights with 0 as the second night's price. Example: <lengthOfStayBaseRentRow>20211-11-01,6,11277.75,0</lengthOfStayBaseRentRow> Type: object |
| <paymentSchedule> | Payment schedule required for the listing.Type: <paymentSchedule> |
| <taxRules> | Taxes collected for the listing.Type: <taxRules> |

### ELEMENT <percentOfRentDiscounts>

These discounts are applied to the nightly rate. A PM can specify up to 15 percent-of-rent discounts per <lodgingRate> element. Each discount (as specified by the <discount> child element) can be applied per night for nightly rates , with nightly overrides per discount amount.

```xml
<percentOfRentDiscounts>
	<discount>
		<externalId>{id}</externalId>
		<name>{name}</name>
		<appliesPerNight>{discounts}</appliesPerNight>
		<appliesPerStay>{discounts}</appliesPerStay>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<percent>{amount}</percent>
	</discount>
...
</percentOfRentDiscounts>
```

### Child Elements

| Name | Description |
|---|---|
| <externalId> | Unique ID of the unit. Type: string |
| <name> | Name of the discount. Type: string |
| <appliesPerNight> | Discount applied per night for nightly rates. * This child element is required for nightly rates only (in the <lodgingRate> element). It is not supported for LoS rates. Type: <appliesPerNight> |
| <appliesPerStay> | Discount applied per stay for LoS rates. * This child element is required for LoS rates only (in the <lodgingRateLos> element). It is not supported for nightly rates. Type: <appliesPerStay> |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the amount associated with the discount for specific dates. Type: <nightlyOverrides> |
| <percent> | Percent of the rental rate that will be discounted. Type: decimal |

### ELEMENT <fees>

Fees are used to collect mandatory charges that are assessed based on different stay configurations. Each distinct fee has an individual breakdown of applications for that fee. Note the following when implementing fees:

- If a property allows pets, the <petsAllowedRule>element must be configured in the Lodging Configurations index. Then, to collect a pet fee, configure the <petFees> element in the Lodging Rate content.
- To prevent a guest fee from being charged multiple times, we specify <forGuestNumber>values in a range.
- The <displayInRent> element is available for property managers who choose fees that they want included into the rent line item.
- We do not include fees with a value of (0).

```xml
<fees>
  <cleaningFees>{fees}</cleaningFees>
  <flatRefundableDamageDepositFees>{deposits}</flatRefundableDamageDepositFees>
  <guestFees>{fees}</guestFees>
  <otherFees>{fees}</otherFees>
  <percentOfRentFees>{fees}</percentOfRentFees>
  <petFees>{fees}</petFees>
</fees>
```

### Child Elements

| Name | Description |
|---|---|
| <cleaningFees> | Mandatory cleaning fees for the stay. Type: <cleaningFees> |
| <flatRefundableDamageDepositFees> | Deposits that will be refunded to the traveler if no damage is sustained to the property during the stay. Type: <flatRefundableDamageDepositFees> |
| <guestFees> | Fees that are assessed based on guest characteristics, such as the number of guests. Type: <guestFees> |
| <otherFees> | Fees that are not captured by cleaning, guest, percent of rent, and pet fees. Type: <otherFees> |
| <percentOfRentFees> | Fees that are charged as a percentage of rent. Type: <percentOfRentFees> |
| <petFees> | Fees that are charged if a pet is included in the stay. Type: <petFees> |

### ELEMENT <cleaningFees>

This element defines mandatory cleaning fees that are collected before the stay (up to 15).

```xml
<cleaningFees>
	<fee>
		<externalId>{id}</externalId>
		<amount>{amount}</amount>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<appliesPerGuestPerNight>{fees}</appliesPerGuestPerNight>
		<appliesPerGuestPerStay>{fees}</appliesPerGuestPerStay>
		<appliesPerNight>{fees}</appliesPerNight>
		<appliesPerPetPerNight>{fees}</appliesPerPetPerNight>
		<appliesPerPetPerStay>{fees}</appliesPerPetPerStay>
		<appliesPerStay>{fees}</appliesPerStay>
	</fee>
	...
</cleaningFees>
```

### Child Elements

The <fee> child element defines each cleaning fee you want to apply to the stay. Here are the child elements available in the <fee> element. One and only one <appliesPer> child element is required and allowed per fee.|

| Name | Description |
|---|---|
| <externalId> | Unique ID of the fee. Type: string |
| <amount> | Cleaning fee amount. Type: decimal |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the amount associated with the cleaning fee for specific dates. Type: <nightlyOverrides> |
| <appliesPerGuestPerNight> | * Fee that is applied per guest per night. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerGuestPerNight> |
| <appliesPerGuestPerStay> | Fee that is applied per guest per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerGuestPerStay> |
| <appliesPerNight> | Fee that is applied per night. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerNight> |
| <appliesPerPetPerNight> | Fee that is applied per night if pets are included in the reservation. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerPetPerNight> |
| <appliesPerPetPerStay> | Fee that is applied per stay if pets are included in the reservation. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerPetPerStay> |
| <appliesPerStay> | Fee that is applied per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerStay> |

### ELEMENT <flatRefundableDamageDepositFees>

These deposits should be refunded to the traveler if no damage is sustained to the property during the stay. Refundable damage deposits are optional fees applied per stay, with optional nightly overrides. Up to 15 flat refundable damage deposit fees may be specified.

```xml
<flatRefundableDamageDepositFees>
	<fee>
		<externalId>{id}</externalId>
		<amount>{amount}</amount>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<appliesPerStay>{deposits}</appliesPerStay>
	</fee>
...
</flatRefundableDamageDepositFees>
```

### Child Elements

The <fee> child element defines each deposit you want to apply to the stay. Here are the child elements available in the <fee>element.

| Name | Description |
|---|---|
| <externalId> | Unique ID of the fee. Type: string |
| <amount> | Refundable damage deposit amount. Type: decimal |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the amount associated with the deposit for specific dates. Type: <nightlyOverrides> |
| <appliesPerStay> | Deposit that is applied per stay. Type: <appliesPerStay> |

### ELEMENT <guestFees>

This element defines mandatory guest fees and how they are applied. You can specify up to 15 fee amounts, which apply per guest either per stay or per night, with further individual breakdown per application. You will not see a Product Code listed for guestFees, PetFees and CleaningFees. It is assumed that fees in those specific categories are linked to Cleaning, Guest and Pet Product codes in the tax rules of the LodgingRateContent.

```xml
<guestFees>
	<fee>
		<externalId>{id}</externalId>
		<amount>{amount}</amount>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<appliesPerGuestPerNight>{fees}</appliesPerGuestPerNight>
		<appliesPerGuestPerStay>{fees}</appliesPerGuestPerStay>
	</fee>
...
</guestFees>
```

### Child Elements

The <fee> child element defines each guest fee you want to apply to the stay. Here are the child elements available in the <fee>element. One and only one <appliesPer> child element is required and allowed per guest fee.

| Name | Description |
|---|---|
| <externalId> | External ID for fee. Type: string |
| <amount> | Guest fee amount. Type: decimal |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the amount associated with the fee for specific dates. Type: <nightlyOverrides> |
| <appliesPerGuestPerNight> | * Fee that is applied per guest per night. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerGuestPerNight> |
| <appliesPerGuestPerStay> | * Fee that is applied per guest per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerGuestPerStay> |

### ELEMENT <otherFees>

Other fees are special fees charged for the rental rate that do not fit into the categories of cleaning fees, guest fees, or pet fees. PMs can specify up to 15 other fees per rate.

```xml
<otherFees>
    <fee>
		<externalId>{id}</externalId>
		<amount>{amount}</amount>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<appliesPerGuestPerNight>{fees}</appliesPerGuestPerNight>
		<appliesPerGuestPerStay>{fees}</appliesPerGuestPerStay>
		<appliesPerNight>{fees}</appliesPerNight>
		<appliesPerPetPerNight>{fees}</appliesPerPetPerNight>
		<appliesPerPetPerStay>{fees}</appliesPerPetPerStay>
		<appliesPerStay>{fees}</appliesPerStay>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<name>{name}</name>
		<productCode>{code}</productCode>
	</fee>
...
</otherFees>
```

### Child Elements

The <fee> child element defines each fee you want to apply to the stay. Here are the child elements available in the <fee>element. One and only one <appliesPer> child element is required and allowed per fee.

| Name | Description |
|---|---|
| <externalId> | Unique ID of the fee. This ID is persisted in the booking request and can help you distinguish the fee from others. Specify up to 64 characters; spaces are not allowed. Type: string |
| <amount> | Fee amount. Type: decimal |
| <appliesPerGuestPerNight> | * Fee that is applied per guest per night. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerGuestPerNight> |
| <appliesPerGuestPerStay> | .* Fee that is applied per guest per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerGuestPerStay> |
| <appliesPerNight> | Fee that is applied per night. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerNight> |
| <appliesPerPetPerNight> | * Fee that is applied per night if pets are included in the reservation. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerPetPerNight> |
| <appliesPerPetPerStay> | Fee that is applied per stay if pets are included in the reservation. Type: <appliesPerPetPerStay> |
| <appliesPerStay> | * Fee that is applied per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerStay> |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the fee amount for specific dates. Type: <nightlyOverrides> |
| <name> | Name of the fee. Specify up to 64 characters. This is for your information only and used primarily for logging and troubleshooting. Type: string |
| <productCode> | Code that associates the fee with a product code for merchandising purposes, such as WATER or ADMINISTRATIVE. This code is persisted (as <productId>) in the booking request. See Product Code Type values for valid values. Type: string |

### ELEMENT <percentOfRentFees>

These fees are charged as a percentage of the rent that is collected. You can specify up to 15 of these fees per rate. They are applied per night with optional nightly overrides.

```xml
<percentOfRentFees>
	<fee>
		<externalId>{id}</externalId>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<percent>{percent}</percent>
		<appliesPerNight>{fees}</appliesPerNight>
		<appliesPerStay>{fees}</appliesPerStay>
		<name>{name}</name>
		<productCode>{code}</productCode>
	</fee>
...
</percentOfRentFees>
```

### Child Elements

The <fee> child element defines each fee you want to apply to the stay. Here are the child elements available in the <fee>element.

| Name | Description |
|---|---|
| <externalId> | Unique ID of this fee. This ID is persisted in the booking request and can help you distinguish the fee from others. Specify up to 64 characters; spaces are not allowed. Type: string |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the fee amount for specific dates. Type: <nightlyOverrides> |
| <percent> | Percentage of the rent to be charged for this fee. Type: decimal |
| <appliesPerNight> | *. Fee that is applied per night for nightly rates. * This child element is required for nightly rates only (in the <lodgingRate> element). It is not supported for LoS rates. Type: <appliesPerNight> |
| <appliesPerStay> | *. Fee that is applied per stay for LoS rates. * This child element is required for LoS rates only (in the <lodgingRateLos> element). It is not supported for nightly rates. Type: <appliesPerStay> |
| <name> | Name of this fee. Specify up to 64 characters. This is for your information only. Type: string |
| <productCode> | Code that characterizes the fee, such as WATER or ADMINISTRATIVE. This code is persisted (as <productId>) in the booking request. See Product Code Type values for valid values. Type: string |

### ELEMENT <petFees>

These fees are charged if a pet is included in the stay. You can specify up to 15 pet fees for each <lodgingRate>. They can apply per pet per night or per pet per stay, with optional nightly overrides.

```xml
<petFees>
	<fee>
		<externalId>{id}</externalId>
		<amount>{amount}</amount>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<appliesPerPetPerNight>{fees}</appliesPerPetPerNight>
		<appliesPerPetPerStay>{fees}</appliesPerPetPerStay>
	</fee>
	...
</petFees>
```

### Child Elements

The <fee> child element defines each fee you want to apply to the stay. Here are the child elements available in the <fee>element. One and only one <appliesPer> child element is required and allowed per fee.

| Name | Description |
|---|---|
| <externalId> | Unique ID of this pet fee. Specify up to 64 characters; spaces are not allowed. Type: string |
| <amount> | Amount to be charged for the pet fee. Type: decimal |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the fee amount for specific dates. Type: <nightlyOverrides> |
| <appliesPerPetPerNight> | Fee that is applied per night if pets are included in the reservation. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerPetPerNight> |
| <appliesPerPetPerStay> | Fee that is applied per stay if pets are included in the reservation. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerPetPerStay> |

### ELEMENT <percentOfRentFees>

This element defines the fees that will be collected during a stay that are a percentage of the rental amount. You can specify up to 10 fees in the <percentOfRentFees> element.

```xml
<percentOfRentFees>
	<fee>
		<days>{integer}</days>
		<due>{dueDate}</due>
		<externalId>{id}</externalId>
		<levied>{levyDate}</levied>
		<name>{name}</name>
		<productCode>{code}</productCode>
		<supportedPaymentMethods>
		<method>{method}</method>
		...
		</supportedPaymentMethods>
		<appliesPerNight>{amount}</appliesPerNight>
		<appliesPerStay>{amount}</appliesPerStay>
		<nightlyOverrides>{overrides}</nightlyOverrides>
		<percent>{amount}</percent>
	</fee>
	...
</percentOfRentFees>
```

### Child Elements

The <fee> child element defines each percent-of-rent fee you want to apply. Here are the child elements available in the <fee>element. One and only one <appliesPer> child element is required and allowed per fee.

| Name | Description |
|---|---|
| <days> | Number of days after check-in or checkout when the fee will be collected, if AFTER_CHECKIN or AFTER_CHECKOUT is specified as the value of <due>. Type: integer |
| <due> | When the fee is due, such as AFTER_CHECKIN. See Stay-collected Fee Due Type values. Type: string |
| <externalId> | Unique ID of the fee. This ID is persisted in the booking request and can help you distinguish the fee from others. Type: string |
| <name> | Name of the flat fee. Specify up to 64 characters. This is for your information only. Type: string |
| <productCode> | Code that describes the fee, such as WATER or ADMINISTRATIVE. This code is persisted (as <productId>) in the booking request. See Product Code Type values for valid values. Type: string |
| <supportedPaymentMethods> | Payment methods supported for the fee. Specify a <method> child element for each payment method (up to 34 methods), such as AMEX or BANKTRANSFER. See Payment Method Type values. Type: object |
| <appliesPerNight> | Fee that is applied per night for nightly rates. * This child element is not supported for LoS rates, and only one <appliesPer> child element is required and allowed. Type: <appliesPerNight> |
| <appliesPerStay> | Fee that is applied per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerStay> |
| <nightlyOverrides> | Rules that are applied on a nightly basis and that override the fee amount for specific dates. Type: <nightlyOverrides> |
| <percent> | Percentage of the rental amount to use for the fee. Type: decimal |

### ELEMENT <nightlyRates>

A nightly rate defines the base rental amount that is due on a night-by-night basis, exclusive of discounts, fees, and taxes.

```xml
<nightlyRates>
	<fri>{amount}</fri>
	<mon>{amount}</mon>
	<nightlyOverrides>{overrides}</nightlyOverrides>
	<sat>{amount}</sat>
	<sun>{amount}</sun>
	<thu>{amount}</thu>
	<tue>{amount}</tue>
	<wed>{amount}</wed>
</nightlyRates>
```

### Child Elements

*NOTE: You should be calculating pricing out of the <nightlyOverrides>. The amount fed in fri,sat,sun,mon,tues,wed,thu, are simply placeholders and should not be used as pricing.

| Name | Description |
|---|---|
| <fri> | Standard nightly rate for a Friday. This value must be greater than zero (0). Type: decimal |
| <mon> | Standard nightly rate for a Monday. This value must be greater than zero (0). Type: decimal |
| <nightlyOverrides> | Rules that override the standard nightly rate for the specified day. Type: <nightlyOverrides> |
| <sat> | Standard nightly rate for a Saturday. This value must be greater than zero (0). Type: decimal |
| <sun> | Standard nightly rate for a Sunday. This value must be greater than zero (0). Type: decimal |
| <thu> | Standard nightly rate for a Thursday. This value must be greater than zero (0). Type: decimal |
| <tue> | Standard nightly rate for a Tuesday. This value must be greater than zero (0). Type: decimal |
| <wed> | Standard nightly rate for a Wednesday. This value must be greater than zero (0). Type: decimal |

### ELEMENT <paymentSchedule>

This element defines one or more payments that are due after a booking. For payment schedule examples, see the example below.

```xml
<paymentSchedule>
	<externalId>{id}</externalId>
	<payments>{payments}</payments>
</paymentSchedule>
```

### Child Elements

| Name | Description |
|---|---|
| <externalId> | Unique ID of the payment schedule. Type: string |
| <payments> | Payments due during or after booking. Type: <payments> |

### ELEMENT <payments>

This element defines the payments that are due during or after booking. You can specify up to five payments in the <payments> element.

```xml
<payments>
	<payment>
	<days>{integer}</days>
	<dueType>{dueDate}</dueType>
	<externalId>{id}</externalId>
	<requiresFlatAmountOf>
	<amount>{amount}</amount>
	</requiresFlatAmountOf>
	<requiresPercentOfTotalBooking>
	<percent>{amount}</percent>
	</requiresPercentOfTotalBooking>
	<requiresRemainder/>
	</payment>
...
</payments>
```

### Child Elements

| Name | Description |
|---|---|
| <days> | Number of days before or after check-in that payment is due. Used in conjunction with <dueType> when <dueType> is set to BEFORE_CHECKIN, AFTER_CHECKIN, and AFTER_CHECKOUT. Type: integer |
| <dueType> | When the payment is due, such as AT_CHECKIN. See Payment Schedule Due Type values. Note: One <payment> child element must specify <dueType>AT_BOOKING</dueType>. Type: string |
| <externalId> | Unique ID of the payment. Type: string |
| <requiresFlatAmountOf> | * Flat amount is due for this payment as specified by the <amount> child element. * Only one <requires> element is required and allowed. Type: decimal Example: To require $100 seven days before check-in and the rest at check-in:\| <payments> <payment> <days>7</days> <dueType>BEFORE_CHECKIN</dueType> <requiresFlatAmountOf> <amount>100</amount> </requiresFlatAmountOf> </payment> <payment> <dueType>AT_CHECKIN</dueType> <requiresRemainder/> </payment> </payments> |
| <requiresPercentOfTotalBooking> | * Percentage of the total rental amount due for this payment as specified by the <percent> child element. * Only one <requires> element is required and allowed. Type: decimal Examples: To require 50% of the rental amount at booking and the rest at check-in:<payments> <payment> <dueType>AT_BOOKING</dueType> <requiresPercentOfTotalBooking> <percent>50</percent> </requiresPercentOfTotalBooking> </payment> <payment> <dueType>AT_CHECKIN</dueType> <requiresRemainder/> </payment> </payments> To require 100% of the payment 14 days before check-in: <payments> <payment> <dueType>AT_BOOKING</dueType> <requiresPercentOfTotalBooking> <percent>0</percent> </requiresPercentOfTotalBooking> </payment> <payment> <days>14</days> <dueType>BEFORE_CHECKIN</dueType> <requiresRemainder/> </payment> </payments> |
| <requiresRemainder> | Empty element that signifies that the remainder of the balance must be paid with this payment. * Only one <requires> element is required and allowed. Type: null |

### ELEMENT <taxRules>

This element is used to estimate taxes on behalf of the integration partner. It is recommended that you apply taxes per stay instead of per night (for nightly and length-of-stay rates). Streamline provides the ability for PM's to split rental amounts from taxes (including VAT) in booking data sent in this file. Each PM must provide information about compliance, and they must supply instructions and individual tax requirements for their integrated properties. Note: Taxes are collected on fees depending on how the fee and tax rule are expressed:

- A fee that is expressed as <appliesPerStay>is taxed only if the tax rule is expressed as <appliesPerStay>.
- A fee that is expressed as <appliesPerNight>is taxed if the tax rule is expressed as either <appliesPerStay> or <appliesPerNight>.

```xml
<taxRules>
	<percentOfFeesTaxRules>{payments}</percentOfFeesTaxRules>
	<percentOfRentTaxRules>{rules}</percentOfRentTaxRules>
</taxRules>
```

### Child Elements

| Name | Description |
|---|---|
| <percentOfFeesTaxRules> | Taxes that are applied to the rental amount based on a percentage of fees. Type: <percentOfFeesTaxRules> |
| <percentOfRentTaxRules> | Taxes that are applied to the rental amount that are based on a percentage of the rental rate. Type: <percentOfRentTaxRules> |

### ELEMENT <percentOfFeesTaxRules>

This element is used to express tax rules that are defined as a percentage of fees. PM can provide up to 15 tax rules in <percentOfFeesTaxRules>.

```xml
<percentOfFeesTaxRules>
	<rule>
		<activeLocalDateRange>
		<max>{date}</max>
		<min>{date}</min>
		</activeLocalDateRange>
		<currency>{code}</currency>
		<externalId>{id}</externalId>
		<name>{name}</name>
		<appliesToFeesPerNight>{taxes}</appliesToFeesPerNight>
		<appliesToFeesPerStay>{taxes}</appliesToFeesPerStay>
		<percent>{amount}</percent>
	</rule>
...
</percentOfFeesTaxRules>
```

### Child Elements

| Name | Description |
|---|---|
| <activeLocalDateRange> | Date range describing when this rule should be considered active. This range contains two child elements (<max> and <min>) of type <date> with format YYYY-MM-DD. Either <max>or <min> or both are required. Type: object |
| <currency> | Only USD currently supported. Type: string |
| <externalId> | Unique ID of this tax rule. This ID is persisted in the booking request and can help you distinguish the tax from others. Type: string |
| <name> | Name assigned to the tax rule. This is for your information only. Type: string |
| <appliesToFeesPerNight> | Taxes that are applied to fees per night. * One and only one <appliesTo> child element is required and allowed. Type: <appliesToFeesPerNight> |
| <appliesToFeesPerStay> | Taxes that are applied to fees per stay. * One and only one <appliesTo> child element is required and allowed. Type: <appliesToFeesPerStay> |
| <percent> | Percentage of fee amount required for this tax rule. Type: decimal |

### ELEMENT <percentOfRentTaxRules>

This element is used to express tax rules that are defined as a percentage of the rent. PM's can specify up to 15 tax rules in <percentOfRentTaxRules>.

```xml
<percentOfRentTaxRules>
	<rule>
		<activeLocalDateRange>
		<max>{date}</max>
		<min>{date}</min>
		</activeLocalDateRange>
		<currency>{code}</currency>
		<externalId>{id}</externalId>
		<name>{name}</name>
		<percent>{amount}</percent>
		<appliesPerNight>{taxes}</appliesPerNight>
		<appliesPerStay>{taxes}</appliesPerStay>
	</rule>
...
</percentOfRentTaxRules>
```

### Child Elements

| Name | Description |
|---|---|
| <activeLocalDateRange> | Date range describing when this rule should be considered active. This range contains two child elements (<max> and <min>) of type <date> with format YYYY-MM-DD. Either <max> or <min> or both are required. Type: object |
| <currency> | Only USD currently supported. Type: string |
| <externalId> | Unique ID of this tax rule. This ID is persisted in the booking request and can help you distinguish the tax from others. Type: string |
| <name> | Name assigned to the tax rule. Specify up to 64 characters. This is for your information only. Type: string |
|  | Percentage of rent amount required for this tax rule. Type: decimal |
| <appliesPerNight> | * Taxes that are applied to rent per night for nightly rates. * This is unsupported for LoS rates, and only one <appliesPer> child element is required and allowed. Type: <appliesPerNight> |
| <appliesPerStay> | * Taxes that are applied to rent per stay. * One and only one <appliesPer> child element is required and allowed. Type: <appliesPerStay> |

### ELEMENT <appliesPerGuestPerNight>

This element defines discounts, fees, and taxes that are applied per guest per night.

```xml
<appliesPerGuestPerNight>
<forDaysOfWeek>
<day>{day}</day>
...
</forDaysOfWeek>
<forGuestNumber>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forGuestNumber>
<forGuestsOfAge>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forGuestsOfAge>
<forNightNumber>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forNightNumber>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forNightsBookedInAdvance>
<forStaysOfNights>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDateIn>
</appliesPerGuestPerNight>
```

### Child Elements

| Name | Description |
|---|---|
| <forDaysOfWeek> | If the day of week is within the days specified in this element, this rule (of the parent element) takes effect. PM's can specify up to seven <day> child elements, one for each of these values: MON, TUE, WED, THU, FRI, SAT, or SUN. Type: object |
| <forGuestNumber> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged per guest within or beyond the range. For example, a PM can charge for the third and fourth guests or for each guest beyond the fifth guest. PM's can specify up to five <range> child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <forGuestsOfAge> | If the age of any guest is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum age of guests. Type: object |
| <forNightNumber> | If the the night number is within the specified integer range, this rule (of the parent element) takes effect for those night numbers. You can specify up to five <range>child elements, each of night number in the stay. Type: object |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. You can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PMs can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDateIn> | If any of the stay dates fall within the specified date range, this rule (of the parent element) takes effect, and it works in conjunction <applies*PerNight> elements. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesPerGuestPerStay>

This element defines discounts, fees, and taxes that are applied (one-time) per guest per stay.

```xml
<appliesPerGuestPerStay>
<forGuestNumber>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forGuestNumber>
<forGuestsOfAge>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
```

### Child Elements

| Name | Description |
|---|---|
| <forGuestNumber> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged per guest within or beyond the range. For example, PMs can charge for the third and fourth guests or for each guest beyond the fifth guest. You can specify up to five <range> child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PMs can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesPerNight>

This element defines discounts, fees, and taxes that are applied per night.

```xml
<appliesPerNight>
<forDaysOfWeek>
<day>{day}</day>
...
</forDaysOfWeek>
<forNightNumber>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forNightNumber>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forNightsBookedInAdvance>
<forStaysOfNights>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDateIn>
</appliesPerNight>
```

### Child Elements

| Name | Description |
|---|---|
| <forDaysOfWeek> | If the day of week is within the days specified in this element, this rule (of the parent element) takes effect. PM's can specify up to seven <day> child elements, one for each of these values: MON, TUE, WED, THU, FRI, SAT, or SUN. Type: object |
| <forNightNumber> | If the the night number is within the specified integer range, this rule (of the parent element) takes effect for those night numbers. PMs can specify up to five <range>child elements, each of night number in the stay. Type: object |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDateIn> | If any of the stay dates fall within the specified date range, this rule (of the parent element) takes effect, and it works in conjunction <applies*PerNight> elements. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesPerPetPerNight>

This element defines discounts, fees, and taxes that are applied per night. Note: The discount, fee, or tax is applied if a pet is included in the reservation, not according to the number of pets, because the HomeAway site does not collect number of pets.

```xml
<appliesPerPetPerNight>
<forDaysOfWeek>
<day>{day}</day>
...
</forPetNumber>
<forStaysOfNights>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDateIn>
</appliesPerPetPerNight>
```

### Child Elements

| Name | Description |
|---|---|
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PMs can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDateIn> | If any of the stay dates fall within the specified date range, this rule (of the parent element) takes effect, and it works in conjunction <applies*PerNight> elements. PM s can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesPerPetPerStay>

This element defines discounts, fees, and taxes that are applied per stay. Note: The discount, fee, or tax is applied if a pet is included in the reservation, not according to the number of pets, because the HomeAway site does not collect number of pets.

```xml
<appliesPerPetPerStay>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forPetNumber>
<forStaysOfNights>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDatesIntersect>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDatesIntersect>
</appliesPerPetPerStay>
```

### Child Elements

| Name | Description |
|---|---|
| <forPetNumber> | If a pet is included in the reservation, this rule (of the parent element) takes effect. You can specify up to five <range> child elements, each of which defines a minimum and maximum number of pets. |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PMs can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PMNs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDateIn> | If any of the stay dates fall within the specified date range, this rule (of the parent element) takes effect, and it works in conjunction <applies*PerNight> elements. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesPerStay>

This element defines discounts, fees, and taxes that are applied (one-time) per stay.

```xml
<appliesPerStay>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDatesIntersect>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDatesIntersect>
</appliesPerStay>
```

### Child Elements

| Name | Description |
|---|---|
| <forNightsBookedInAdvance> | If the number of nights booked in advance is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights in advance of the booking. Type: object |
| <forPetNumber> | If the number of pets is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged per pet within or beyond the range. For example, you can charge for the second and third pets or for each pet beyond the second pet. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of pets. Type: object |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PMs can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDatesIntersect> | If the arrival or departure date intersects or overlaps with any of the specified date ranges, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesToFeesPerNight>

This element defines fees that are applied per night during a stay.

```xml
<appliesToFeesPerNight>
<forDaysOfWeek>
<day>{day}</day>
...
</forDaysOfWeek>
<forNightNumber>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forNightNumber>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDateIn>
<whenProductCodeIn>
<code>{code}</code>
...
</whenProductCodeIn>
</appliesToFeesPerNight>
```

### Child Elements

| Name | Description |
|---|---|
| <forNightNumber> | If the the night number is within the specified integer range, this rule (of the parent element) takes effect for those night numbers. PMs can specify up to five <range>child elements, each of night number in the stay. Type: object |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PMs can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PMs can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDateIn> | If any of the stay dates fall within the specified date range, this rule (of the parent element) takes effect, and it works in conjunction <applies*PerNight> elements. PMs can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenProductCodeIn> | If the fee's product code is present in the list of codes in this element, the rule (of the parent element) takes effect. PMs can specify up to 59 <code> child elements, though they cannot specify a code more than once. See Product code type values for the list of valid values. Type: object |

### ELEMENT <appliesToFeesPerStay>

This element defines fees that are applied to fees (one-time) during a stay.

```xml
<appliesToFeesPerStay>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenStayDatesIntersect>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDatesIntersect>
<whenProductCodeIn>
<code>{code}</code>
...
</whenProductCodeIn>
</appliesToFeesPerStay>
```

### Child Elements

| Name | Description |
|---|---|
| <forNightsBookedInAdvance> | If the number of nights booked in advance is within the specified integer range, this rule (of the parent element) takes effect. PM's can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights in advance of the booking. Type: object |
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PM's can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PM's can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenStayDatesIntersect> | If the arrival or departure date intersects or overlaps with any of the specified date ranges, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenProductCodeIn> | If the fee's product code is present in the list of codes in this element, the rule (of the parent element) takes effect. PM's can specify up to 59 <code> child elements, though they cannot specify a code more than once. See Product code type values for the list of valid values. Type: object |

### ELEMENT <appliesToRentAndFeesPerNight>

This element defines fees that are applied to rent and fees per night during a stay.

```xml
<appliesToRentAndFeesPerNight>
<forDaysOfWeek>
<day>{day}</day>
...
</forNightNumber>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenProductCodeIn>
<code>{code}</code>
...
</whenProductCodeIn>
<whenStayDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDateIn>
</appliesToRentAndFeesPerNight>
```

### Child Elements

| Name | Description |
|---|---|
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PM's can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenProductCodeIn> | If the fee's product code is present in the list of codes in this element, the rule (of the parent element) takes effect. PM's can specify up to 59 <code> child elements, though you cannot specify a code more than once. See Product code type values for the list of valid values. Type: object |
| <whenStayDateIn> | If any of the stay dates fall within the specified date range, this rule (of the parent element) takes effect, and it works in conjunction <applies*PerNight> elements. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <appliesToRentAndFeesPerStay>

This element defines fees that are applied to rent and fees (one-time) during a stay.

```xml
<appliesToRentAndFeesPerStay>
<forNightsBookedInAdvance>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysOfNights>
<forStaysWithNumberOfGuests>
<range>
<max>{integer}</max>
<min>{integer}</min>
</range>
...
</forStaysWithNumberOfGuests>
<whenBookingDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenBookingDateIn>
<whenCheckinDateIn>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenCheckinDateIn>
<whenProductCodeIn>
<code>{code}</code>
...
</whenProductCodeIn>
<whenStayDatesIntersect>
<range>
<max>{date}</max>
<min>{date}</min>
</range>
...
</whenStayDatesIntersect>
</appliesToRentAndFeesPerStay>
```

### Child Elements

| Name | Description |
|---|---|
| <forStaysOfNights> | If the length of stay is within the specified integer range, this rule (of the parent element) takes effect. PM's can specify up to five <range> child elements, each of which defines a minimum and maximum number of nights. Type: object |
| <forStaysWithNumberOfGuests> | If the number of guests is within the specified integer range, this rule (of the parent element) takes effect. The amount is charged once (for the stay) if the number of guests falls within the range or beyond the maximum. PM's can specify up to five <range>child elements, each of which defines a minimum and maximum number of guests. Type: object |
| <whenBookingDateIn> | If the booking date is within the specified date range, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenCheckinDateIn> | If the check-in date is within the specified date range, this rule (of the parent element) takes effect. PM 's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <whenProductCodeIn> | If the fee's product code is present in the list of codes in this element, the rule (of the parent element) takes effect. PM's can specify up to 59 <code> child elements, though you cannot specify a code more than once. See Product code type values for the list of valid values. Type: object |
| <whenStayDatesIntersect> | If the arrival or departure date intersects or overlaps with any of the specified date ranges, this rule (of the parent element) takes effect. PM's can specify up to 50 <range> child elements, each of which defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |

### ELEMENT <nightlyOverrides>

Nightly overrides enable you to override nightly rates, discounts, fees, or price thresholds (guardrails) by specifying an amount or a percentage for a date range. Be aware of the following when defining overrides:

- Do not use the same date for <max>and <min> in different date ranges within an override; ranges cannot overlap.
- Amount and percent values must be unique, which might necessitate multiple night ranges.
- The combined total of nightly overrides and date ranges cannot exceed 2000.

```xml
<nightlyOverrides>
	<override>
		<amount>{amount}</amount>
		<nights>
		<range>
		<max>{date}</max>
		<min>{date}</min>
		</range>
		...
		</nights>
		<percent>{amount}</percent>
		</override>
	...
</nightlyOverrides>
```

### Child Elements

| Name | Description |
|---|---|
| <amount> | If the <nightlyOverrides> element is the child of an amount-based element, this child element is required. It specifies a flat amount that will serve as the rate, discount, fee, or price threshold for the nights specified. Type: decimal |
| <nights> | Date ranges for which the override applies PM's can specify up to 2,000 <range> child elements, though the combined total of nightly overrides and night ranges cannot exceed 2000. Each range defines a beginning (<min>) and end (<max>) date. Date format is YYYY-MM-DD. Type: object |
| <percent> | If the <nightlyOverrides> element is the child of a percent-based element, this child element is required. It specifies a percentage for the rate, discount, fee, or price threshold for the nights specified. Type: decimal |
