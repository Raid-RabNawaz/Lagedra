# Unit Availability Content

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Unit-Availability-Content`

---
This file sets the rental unit's calendar in the feed. We specify dates that are available for a particular unit and the rules that must be enforced for the stay.

- Availability – available or not available
- Change over – this is a check-in day, checkout day, or both
- Minimum length of stay – how many days that the traveler must stay
- Minimum lead time – minimum lead time required before a traveler can book a reservation

For the best traveler booking experience, we recommend the following:

- Update availability regularly

The Unit Availability index contains a link to this file for each listing.

### ELEMENT <unitAvailabilityContent>

```xml
<?xml version="1.0" encoding="UTF-8"?>
<unitAvailabilityContent>
<listingExternalId>{id}</listingExternalId>
<unitExternalId>{id}</unitExternalId>
<unitAvailability>{availability}</unitAvailability>
</unitAvailabilityContent>
```

### Child Elements

| Name | Description |
|---|---|
| <listingExternalId> | The unique external ID of the listing within Streamline. Same as <unitExternalId>Type: string |
| <unitExternalId> | The unique external ID of the unit in Streamline. Same as <listingExternalId>Type: string |
| <unitAvailability> | Availability (calendar) and stay information for the listing.Type: <unitAvailability> |

### ELEMENT <unitAvailability>

```xml
<unitAvailability>
	<availabilityDefault>{default}</availabilityDefault>
	<changeOverDefault>{default}</changeOverDefault>
	<dateRange>
		<beginDate>{arrivalDate}</beginDate>
		<endDate>{departureDate}</endDate>
	</dateRange>
	<stayIncrementDefault>{default}</stayIncrementDefault> //Not Supported//
	<unitAvailabilityConfiguration>{configuration}</unitAvailabilityConfiguration>
</unitAvailability>
```

### Child Elements

| Name | Description |
|---|---|
| <availabilityDefault> | The default value for daily availability for days not specified within <dateRange>. Valid values include Y for available and N for not available. If <availabilityDefault> is not provided, a default value of Y is used.Type: string |
| <changeOverDefault> | Default value for <changeOver> (check-in day) for days not specified within <dateRange>. Valid values include X for no action possible, C for check-in/out, O for checkout only, and I for check-in only. If <changeOverDefault> is not provided, a default value of C is used.Type: string |
| <dateRange> | Date range for the specified availability. Specify the <beginDate> and <endDate>child elements to define the date range, and the format is YYYY-MM-DD. you can include up to three years (1096 days) in the range. Default values take effect for dates beyond the date range until the end of the calendar length (three years/1096 days).Type: object |
| <unitAvailabilityConfiguration> | Availability settings for the unit.Type: <unitAvailabilityConfiguration> |

### ELEMENT <unitAvailabilityConfiguration>

This element provides up to three years of unit availability information for the unit. If dates are not included in the configuration's date range, default values from <unitAvailability>'s child elements are used.

```xml
<unitAvailabilityConfiguration>
	<availability>{availability}</availability>
	<availableUnitCount>{counts}</availableUnitCount>
	<changeOver>{values}</changeOver>
	<maxStay>{stays}</maxStay>
	<minPriorNotify>{values}</minPriorNotify>
	<minStay>{stays}</minStay>
	<stayIncrement>{increments}</stayIncrement> //Not Supported//
</unitAvailabilityConfiguration>
```

### Child Elements

<dateRange>

| Name | Description |
|---|---|
| <availability> | Comma-separated list of availability codes for every day in <dateRange>. Supported codes include Y for available and N for not available. Example: YYYNNNYYYNNNYYYNNNYYYNNNYYYType: string |
| <changeOver> | Comma-separated list of change-over data (check-in days) for every day in <dateRange>. Valid codes include X for no action possible, C for check-in/out, O for checkout only, and I for check-in only. Example: CCIIOOXCCIIOOXCCIIOOXCCIIOOXCCIIOOXCCIIOOXCCIIOType: string |
| <minPriorNotify> | Comma-separated list of the minimum numbers of days required to book the stay before the check-in date. Valid values include 0-999, where 0 means no prior notification required. If, for example, you specify "2" and the traveler books on a Wednesday, he can check-in on Friday. Example: 2,2,3,7,7,7,7,2,2,3,7,7,7,7,2,2,3,7,7,7,7,2,2,3,7,7,7,7,2,2 …Type: string |
| <minStay> | Comma-separated list of the minimum number of days allowed in a stay for each day in <dateRange>. Valid values include 0-999, where 0 means no minimum stay. Example: 2,2,3,7,7,7,7,2,2,3,7,7,7,7,2,2,3,7,7,7,7,2,2,3,7,7,7,7,2,2Type: string |

### EXAMPLE

```xml
<?xml version="1.0" encoding="UTF-8"?>
<unitAvailabilityContent>
             <listingExternalId>501</listingExternalId>
             <unitExternalId>501a</unitExternalId>
             <unitAvailability>
                         <availabilityDefault>Y</availabilityDefault>
                         <changeOverDefault>X</changeOverDefault>
                         <dateRange>
                                     <beginDate>2017-08-01</beginDate>
                                     <endDate>2017-08-31</endDate>
                         </dateRange>
                         <maxStayDefault>28</maxStayDefault>
                         <unitAvailabilityConfiguration>
                                     <availability>NNYYYYYYYYNNNNNNNYYYYYYYYYYYYYY</availability>
                                     <availableUnitCount>0,0,1,2,2,3,3,3,3,3,0,0,0,0,0,0,0,1,1,1,2,2,2,2,3,3,3,3,3,1,1</availableUnitCount>
                                     <changeOver>CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC</changeOver>
                                     <maxStay>28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28</maxStay>
                                     <minPriorNotify>14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14,14</minPriorNotify>
                                     <minStay>3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3</minStay>
                                     <stayIncrement>DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD</stayIncrement> //Not Supported//
                         </unitAvailabilityConfiguration>
             </unitAvailability>
 </unitAvailabilityContent>
```
