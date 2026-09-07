# Listing Content

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Listing-Content`

---
The Listing Content file provides most of the content needed to create the listing for a rental unit. Content in this file is static and includes the headline, description, photo, amenities ("feature values"), and safety features for each rental unit. The Listing index contains a link to this file for each listing that has been opted-in to the integration by the property manager within Streamline. When creating this XML file, keep the following in mind, each listing and unit are identified by <listingExternalId> and <unitExternalId>, respectively. External IDs (unit id's) are assigned by Streamline globally across all PM's and will never change.

### ELEMENT<listing>

This is the root element of the index.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<listing>
   <inserted>{dateTime}</inserted>
   <updated>{dateTime}</updated>
   <externalId>{id}</externalId>
   <active>{boolean}</active>
   <adContent>{content}</adContent> //Not Supported//
   <featureValues>{amenities}</featureValues>
   <location>{location}</location>
   <images>{images}</images>
   <units>{units}</units>
 </listing>
```

### Child Elements

| Name | Description |
|---|---|
| <inserted> | For internal use only Type: dateTime |
| <updated> | For internal use only Type: dateTime |
| <externalId> | Unique ID of the listing within Streamline. This value will correspond with the <externalId> value used in the index. Type: integer |
| <active> | Whether the listing is active. Type: boolean |
| <adContent> | Descriptive fields of the listing. Type: <adContent> |
| <featureValues> | Standardized property features (amenities) that distinguish the listing. Type: <featureValues> |
| <location> | Information about the location of the rental unit. Type: <location> |
| <images> | A collection of images for the listing. Type: <images> |
| <units> | Details about the rental unit. Only one unit is supported per listing (we do not specify more than one <unit> child element). Type: <units> |

### ELEMENT <adcontent>

The ad content for each listing, including the listing's headline and description, includes important details that attract travelers.

```xml
<adContent>
	<description>
		<texts>
		<text locale="{code}">
		<textValue>{description}</textValue>
		</text>
		...
		</texts>
	</description>
	<headline>
		<texts>
		<text locale="{code}">
		<textValue>{headline}</textValue>
		</text>
		...
		</texts>
	</headline>
	<ownerListingStory>
		<texts>
		<text locale="{code}">
		<textValue>{story}</textValue>
		</text>
		...
		</texts>
	</ownerListingStory>
	<propertyName>
		<texts>
		<text locale="{code}">
		<textValue>{name}</textValue>
		</text>
		...
		</texts>
	</propertyName>
 </adContent>
```

### Child Elements

| Name | Description |
|---|---|
| <description> | Description of the property. At least 400 characters are required, although PM's can specify up to 10,000 characters. Type: string |
| <headline> | Headline for the advertisement. At least 20 characters are required for the listing. Though the integration allows for up to 400 characters. Type: string |
| <ownerListingStory> | Story describing how the owner came to own the property and the role the property has played in his or her life. PM can specify up to 2,000 characters. Currently not supported - coming soon Type: string |
| <propertyName> | Name of the property. PM can specify up to 400 characters. Type: string |

### ELEMENT <featurevalues>

```xml
<?xml version="1.0" encoding="UTF-8"?>
<listing>
	<inserted>{dateTime}</inserted>
	<updated>{dateTime}</updated>
	<externalId>{id}</externalId>
	<active>{boolean}</active>
	<adContent>{content}</adContent>
	<featureValues>{amenities}</featureValues>
	<location>{location}</location>
	<images>{images}</images>
	<matterportUrl>{url}</matterportUrl>
	<units>{units}</units>
</listing>
```

### Child Elements

<featureValues>

<featureValue>

<featureValues>

| Name | Description |
|---|---|
| <count> | Quantity of the feature represented. Type: integer |
| <description> | Description of the feature. PM's can specify up to 1,000 characters. Type: string |
| <listingFeatureName> | Pre-defined value for each listing feature. Type: string |

### ELEMENT <location>

<location>

- <geoCode>(longitude and latitude coordinates)

```xml
<location>
<address>{address}</address>
<description>
	<texts>
	<text locale="{code}">
	<textValue>{description}</textValue>
	</text>
	...
	</texts>
</description>
<geoCode>
	<latLng>
	<latitude>{latitude}</latitude>
	<longitude>{longitude}</longitude>
	</latLng>
</geoCode>
<nearestPlaces>{places}</nearestPlaces>
<showExactLocation>{boolean}</showExactLocation>
</location>
```

### ELEMENT <address>

```xml
<address>
	<addressLine1>{addrLine}</additionalAddressLine1>
	<addressLine2>{street}</addressLine2>
	<city>{city}</city>
	<stateOrProvince>{state}</stateOrProvince>
	<country>{country}</country>
	<postalCode>{code}</postalCode>
</address>
```

### Child Elements

| Name | Description |
|---|---|
| <addressLine1> | Line one of the physical street address. PM can specify up to 225 characters. Type: string |
| <addressLine2> | Line two of the physical street address. PM can specify up to 225 characters. Type: string |
| <city> | City where the property is located. PM can specify up to 80 characters. Type: string |
| <stateOrProvince> | State or province of the property. PM can specify up to 80 characters. Type: string |
| <country> | Two-character country code. See Country ISO Code values. Type: string |
| <postalCode> | Postal code of the address. PM can specify up to 50 characters. Type: string |

### ELEMENT <nearestPlaces>

```xml
<nearestPlaces>
	<nearestPlace placeType="{type}">
	<distance>{decimal}</distance>
	<distanceUnit>{unit}</distanceUnit>
	<name>
		<texts>
		<text locale="{code}">
		<textValue>{name}</textValue>
		</text>
		...
		</texts>
	</name>
	</nearestPlace>
...
</nearestPlaces>
```

### Child Elements

<nearestPlaces>

<nearestPlace>

<nearestPlaces>

placeType

<nearestPlace>

| Name | Description |
|---|---|
| <distance> | Distance to place of interest, expressed in the given <distanceUnit> element. Type: decimal |
| <distanceUnit> | Unit of measure in which the distance to the place of interest is expressed, such as KILOMETERS. See Distance Unit values. Type: string |
| <name> | Localized name(s) of place of interest. For each localized name, we specify the <text> child element in the <texts> element. We always specify the locale attribute for each <text> element. For the name, a PM can specify up to 200 characters. Type: string |

### ELEMENT <images>

<externalId>

Also, be aware of the following:

- We support image types: JPG, GIF, and PNG.
- Minimum photo resolution is 1920 x 1080 (HD), though Streamline temporarily accepts photos with a lower resolution. As new photos are taken, we recommend a resolution of 3840 x 2160 (UHD).
- Each image must be less than 20MB.
- The first image in the collection is used as the thumbnail image for the listing. This is the first image in the list on the gallery tab of the unit within Streamline. Ordered sequentially from the top down.
- We recommend at least six images be uploaded per unit. Up to 50 images will pass in the feed per unit.

```xml
<images>
	<image>
		<externalId>{id}</externalId>
		<title>
		<texts>
			<text locale="{code}">
			<textValue>{title}</textValue>
		</text>
		...
		</texts>
		</title>
		<uri>{uri}</uri>
	</image>
...
</images>
```

### Child Elements

<images>

<image>

<images>

| Name | Description |
|---|---|
| <externalId> | Unique external ID of the image. PM can specify up to 255 characters (alphanumeric characters, dashes, and underscores only). Type: string |
| <title> | Localized descriptive title(s) for the image. For each localized name, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). For the title, a PM can specify up to 400 characters. Type: object |
| <uri> | Valid URI for the image. Type: string |

### ELEMENT <units>

```xml
<units>
	<unit>
	<inserted>{dateTime}</inserted>
	<updated>{dateTime}</updated>
	<externalId>{id}</externalId>
	<active>{boolean}</active>
	<area>{integer}</area>
	<areaUnit>{unit}</areaUnit>
		<bathroomDetails>
		<texts>
		<text locale="{code}">
		<textValue>{details}</textValue>
		</text>
		...
		</texts>
	</bathroomDetails>
	<bathrooms>{details}</bathrooms>
	<bedroomDetails>
		<texts>
		<text locale="{code}">
		<textValue>{details}</textValue>
		</text>
		...
		</texts>
	</bedroomDetails>
	<bedrooms>{details}</bedrooms>
	<description>
		<texts>
		<text locale="{code}">
		<textValue>{description}</textValue>
		</text>
		...
		</texts>
	</description>
	<featureValues>{amenities}</featureValues>
	<safetyFeatureValues>{safety_amenities}</safetyFeatureValues>
	<images>{images}</images>
	<propertyType>{type}</propertyType>
	<registrationExpirationDate>{date}</registrationExpirationDate>
	<registrationNumber>{number}</registrationNumber>
	<representedUnits>{integer}</representedUnits>
	<unitMonetaryInformation>
	<currency>{currency}</currency>
	</unitMonetaryInformation>
	<unitName>
		<texts>
		<text locale="{code}">
		<textValue>{description}</textValue>
		</text>
		...
		</texts>
	</unitName>
	</unit>
</units>
```

### Child Elements

<units>

| Name | Description |
|---|---|
| <inserted> | For Internal Use Only Type: dateTime |
| <updated> | For Internal Use Only Type: dateTime |
| <externalId> | The unique external ID of the unit assigned by the property manager in Streamline. Type: integer |
| <active> | Whether the unit is active. Type: boolean |
| <area> | Usable area of the unit. Type: integer |
| <areaUnit> | Unit of measure in which the unit’s area is expressed. Valid values include METERS_SQUARED or SQUARE_FEET. Type: string |
| <bathroomDetails> | Localized details about the bathroom(s) in the unit. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). Type: object |
| <bathrooms> | Structured Room data about the bathroom(s) in the unit. Type: <bathrooms> |
| <bedroomDetails> | Localized details about the bedroom(s) in the unit. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). Type: object |
| <bedrooms> | Structured Room data about the bedroom(s) in the unit. Type: <bedrooms> |
| <description> | Used to specify the listing's description in the <adContent> element. Type: object |
| <featureValues> | Standardized property features distinguishing the unit. Type: <featureValues> |
| <safetyFeatureValues> | Safety features, including locations and instructions, provided in the unit. Type: <safetyFeatureValues> |
| <images> | Unsupported. Specify the listing's images in the <images> element. Type: <images> |
| <propertyType> | Type of property this unit represents. Additional property types may also be set under Unit Feature Values. See Property Type values. Type: string |
| <registrationExpirationDate> "PM optional" | Date of expiration for the <registrationNumber>. Format is yyyy-MM-dd. Type: date |
| <registrationNumber> "PM optional" | Property's registration number. PM's specify up to 25 characters. This element is required if the property is located in a jurisdiction where registration is required. Type: string |
| <unitMonetaryInformation> | Currency in which monetary values are presented for the unit. Only USD supported. Type: object |
| <unitName> | Localized name(s) for the unit. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). PM can specify up to 250 characters. Type: object |

### ELEMENT <bathrooms>

Individual descriptions and amenities can be established for each bathroom in the unit, as specified by the <bathroom> child element(s). Bathroom amenities provide detailed information about how Listing content is displayed.

```xml
<bathrooms>
	<bathroom>
		<amenities>
			<amenity>
			<count>{integer}</count>
			<bathroomFeatureName>{name}</bathroomFeatureName>
			</amenity>
			...
		</amenities>
	<name>
		<texts>
		<text locale="{code}">
		<textValue>{name}</textValue>
		</text>
		...
		</texts>
	</name>
	<note>
		<texts>
		<text locale="{code}">
		<textValue>{note}</textValue>
		</text>
		...
		</texts>
	</note>
	<roomSubType>{type}</roomSubType>
	</bathroom>
...
</bathrooms>
```

### Child Elements

<bathrooms>

<bathroom>

<bathrooms>

| Name | Description |
|---|---|
| <amenities> | Amenities associated with the bathroom. For each amenity, specify the <amenity> child element and then provide <count>, which is the number (integer) of this amenity that is present in the bathroom (not required), and <bathroomFeatureName>, which is required and is the standardized feature name that describes the amenity (see Bathroom Feature values). Type: object |
| <name> | Localized name(s) for the bathroom. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). Pm can specify up to 60 characters.Type: object |
| <note> | Localized note(s) about the bathroom. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). For the note, PM can specify up to 60 characters.Type: object |
| <roomSubType> | Sub-category for the amenity, such as FULL_BATH. See Bathroom Type values. Type: string |

### ELEMENT <bedrooms>

<bedroom>

```xml
<bedrooms>
	<bedroom>
	<amenities>
		<amenity>
		<count>{integer}</count>
		<bedroomFeatureName>{name}</bedroomFeatureName>
		</amenity>
	...
	</amenities>
	<name>
		<texts>
		<text locale="{code}">
		<textValue>{name}</textValue>
		</text>
		...
		</texts>
	</name>
	<note>
		<texts>
		<text locale="{code}">
		<textValue>{note}</textValue>
		</text>
		...
		</texts>
	</note>
	<roomSubType>{type}</roomSubType>
	</bedroom>
...
</bedrooms>
```

### Child Elements

<bedrooms>

<bedroom>

<bedrooms>

| Name | Description |
|---|---|
| <amenities> | Amenities associated with the bedroom. For each amenity, we specify the <amenity> child element and then provide <count>, which is the number (integer) of this amenity that is present in the bathroom (not required), and <bedFeatureName>, which is required and is the standardized feature name that describes the amenity (see Bedroom Feature values). Type: object |
| <name> | Localized name(s) for the bedroom. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). PM can specify up to 60 characters. Type: object |
| <note> | Localized note(s) about the bedroom. For each localized string, we specify the <text> child element in the <texts> element. We specify the locale attribute for each <text> element (see Locale values). For the note, PM can specify up to 60 characters. Type: object |
| <roomSubType> | Sub-category for the amenity, such as BEDROOM. See Bedroom Type values. Type: string |

### ELEMENT <featureValues>

```xml
<featureValues>
	<featureValue>
		<count>{integer}</count>
		<description>{description}</description>
		<unitFeatureName>{name}</unitFeatureName>
	</featureValue>
	...
</featureValues>
```

### Child Elements

<featureValues>

<featureValue>

<featureValues>

| Name | Description |
|---|---|
| <count> | Quantity of the feature represented.Type: integer |
| <description> | Description of the feature. PM can specify up to 1,000 characters.Type: string |
| <unitFeatureName> | Pre-defined value for each unit feature. See Unit feature values. Type: string |
