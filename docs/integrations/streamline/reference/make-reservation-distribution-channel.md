# MakeReservationDistributionChannel

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/MakeReservationDistributionChannel`

---
### MakeReservationDistributionChannel

PartnerOLB: This method is used by partners to post a reservation into Streamline.

| Name | Data Type | Description | Required? |
|---|---|---|---|
| token_key | String (255) | The expiring token key credential provided to you by Streamline | Yes |
| token_secret | String (255) | The expiring token secret credential provided to you by Streamline | Yes |
| distributor_code | String | The code tied to a particular distribution channel. Distribution channels are tied to a reservation type. If you are passing distributor_code, you do not need to pass made_type or make_type. | Yes |
| status_id | Number | (4 = Booked) By Default: will be 4 booked if no status_id is passed. (10 = Blocked Request) Some PM's will want you to pass bookings as "Blocked Requests". (9 = Non Blocked Request) - Will not block dates on the unit. | No |
| reservation_id | Number | reservation_id can be used to pass in the reservation number from "partners" system. This will be used as a cross-reference ID within the PM's Streamline system. | No |
| client_comments | String | client comments | No |
| hear_about_new | String | Optional parameter sets the source of the reservation. Important for property managers to know where reservations are coming from. | No |
| unit_id | Number | The unique ID for the home you are making a reservation or getting information from. | Yes |
| startdate | String | start date | Yes |
| enddate | String | end date (check out date) | Yes |
| occupants | Number | The number of adult guests staying at the property. | Yes |
| occupants_small | Number | The number of child guests staying at the property. | Yes |
| pets | Number | Specifies the number of pets that belong to a reservation. | Yes |
| first_name | String | First name of main guest | Yes |
| last_name | String | Last name of main guest | Yes |
| address | String | Postal Address - If passing CC details - must match CC address | Yes |
| city | String | The city for guest address information. Most users will require this. | Yes |
| zip | String | The zipcode of the traveler or lead in the Streamline system. Most users require this. | Yes |
| state_name | String | Specifies the state_name of a traveler or lead in the Streamline system. Most users will require this. | Yes |
| country_name | String | Country name (2 Characters ISO format) | Yes |
| email | String | email address | Yes |
| mobile_phone | String | The cellphone associated with the reservation. Most users will use mobile_phone and home_phone interchangeably. If you only receive one phone number, we recommend to use this field as well as home_phone and pass in both. We recommend you pass the correct country code as well. | Yes |
| phone | String | phone | No |
| payment_comments | String (25550) | Information about how the reservation was or will be paid for, if relevant. | No |
| commission | Number | Partner commission amount, will be deducted from room rent in the Streamline reservation folio and shown on the commission tab. | No |
| total | Number | Total amount of the reservation including room rent + all taxes and fees. | Yes |
| final_price | Number | Final validation of price. The value should match "total" param. Required when using Streamline "total" logic. | No |
| force_adjustment | Boolean | Required value of 1 when using Streamline "total" logic. | Yes |
| rate_only | Boolean | 1 = true 0 = false. Will be discussed in the development process. Required value of 1 when using Streamline "total" logic. | No |
| credit_card_type_id | Number | 1 = Visa 2 = MasterCard 3 = AmericanExpress 4 = Discover | No |
| credit_card_number | Number | 16 Digit CC Number | No |
| credit_card_expiration_month | Number | Credit Card Expiration Month | No |
| credit_card_expiration_year | Number | Credit Card Expiration Year | No |
| credit_card_cid | Number | Credit Card CID | No |
| virtual_credit_card | Boolean | Pass this as true if the card you are posting in is a virtual credit card. | No |
| disable_payments | Number | If this parameter is set to 1, then any reservation with credit card information will not attempt to charge the card automatically. This parameter is highly recommended to be built into, because there are many property managers who don't want to auto charge credit cards. | No |

#### Request Example

- XML
- JSON

```xml
<?xml version="1.0" encoding="UTF-8"?>
<methodCall>
 <methodName>MakeReservationDistributionChannel</methodName>
 <params>
  <token_key>YOUR_TOKEN_KEY</token_key>
  <token_secret>YOUR_TOKEN_SECRET</token_secret>
  <distributor_code>YOUR_DISTRIBUTOR_CODE</distributor_code>
  <reservation_id>1234567</reservation_id>
  <hear_about_new>StreamlineX</hear_about_new>
  <unit_id>422487</unit_id>
  <startdate>08/02/202</startdate>
  <enddate>08/04/2020</enddate>
  <occupants>2</occupants>
  <occupants_small>0</occupants_small>
  <pets>0</pets>
  <first_name>Darth</first_name>
  <last_name>Vader</last_name>
  <address>777 E. Washington Avenue</address>
  <city>Los Angeles</city>
  <zip>90210</zip>
  <state_name>CA</state_name>
  <country_name>US</country_name>
  <email>darthvader@nomail.com</email>
  <mobile_phone></mobile_phone>
  <payment_comments>Paid via check</payment_comments>
  <commission>200</commission>
  <total>3333.50</total>
  <force_adjustment>1</force_adjustment>
  <rate_only>1</rate_only>
  <credit_card_type_id>1</credit_card_type_id>
  <credit_card_number>4111111111111111</credit_card_number>
  <credit_card_expiration_month>06</credit_card_expiration_month>
  <credit_card_expiration_year>2025</credit_card_expiration_year>
  <credit_card_cid>123</credit_card_cid>
  <virtual_credit_card>1</virtual_credit_card>
  <disable_payments>1</disable_payments>
 </params>
</methodCall>
```

#### Response Example

- XML
- JSON

```xml
<?xml version="1.0" ?>
<Response>
	<data>
		<reservation>
			<confirmation_id>2712</confirmation_id>
			<location_name>1B01</location_name>
			<condo_type_name>1 bedroom 1B01</condo_type_name>
			<unit_name>1B01</unit_name>
			<startdate>08/02/2020</startdate>
			<enddate>08/04/2020</enddate>
			<occupants>1</occupants>
			<occupants_small>0</occupants_small>
			<price_common>622.19</price_common>
			<price_balance>622.19</price_balance>
			<travelagent_name>Partner TA</travelagent_name>
		</reservation>
	</data>
</Response>
```
