# Listings XML Reference

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Listings-XML-Reference`

---
Listings XML Reference

XML Structure

This section provides an overview of the Listings feed which defines the structure of the XML files that we provide for you to create listings on your platform. Detailed information about the structure of these feeds is located within the subpages of the the Listings XML Reference category of this documentation.

- Advertisers Content Index
- Listing Index and Content
- Lodging Configuration Index and Content
- Lodging Rate Index and Content
- Unit Availability Index and Content

Advertisers Content Index

The Advertisers Content Index is provided by Streamline to connected partner. This file is an index of indexes. It contains links to the listing content, lodging configuration, rates, and availability information for all Streamline property managers who have opted into your booking integration.

Listings Index and Content

The Listing Index and Content files contain the information needed to create property listings on your platform.

- Headline
- Description
- Photos
- Property features and amenities

Lodging Configuration Index and Content

The Lodging Configuration Index and Content files enable a property manager to specify booking policies, accepted forms of payment, rental agreements, cancellation policies and various other backend configurations within their Streamline system.

Default global values are provided that are applied to every listings for a property manager in the Lodging Configuration index however defaults can be overwritten on a per-unit basis. You can see these variances within the Lodging Configuration Content file per unit.

Lodging Rate Index and Content

The Lodging Rate Index and Content files enable property managers to specify rates for properties. This includes room rent, taxes and fees as well as the rules associated. The lodging Rate Index contains a list of the metadata and URL locations for a PM's rate data, which can be used to calculate consistent pricing from search to book.

There are two pricing structures you will see in the Lodging Rate Content files per unit.

- Nightly Rates
- Length-of-Stay Rates

It is possible for one Property Manager to have units that are using a Nightly Rate structure and other units using a Length-Of-Stay Rates structure. You will need to be able dynamically shift when parsing the feeds to detect Nightly Rates or Length-Of-Stay Rates per property for the same Property Manager.

*Can be used independently or combined with the GetPreReservationPrice live price quote.

Unit Availability Index and Content

The Unit Availability Index and Content files enable a property manager to specify calendar information for all integrated properties. The file contains metadata and a link to the Unit Availability Content file for each opted-in unit.

NOTE: Each unit contained within your indexes has been specifically opted into the integration by the Property Manager within Streamline. If you do not see a company in the feed who has enabled the integration, they most likely have not selected the units they wish to integrate.
