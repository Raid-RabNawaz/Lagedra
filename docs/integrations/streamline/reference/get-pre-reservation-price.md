# GetPreReservationPrice

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Partner OLB/GetPreReservationPrice`

---
### GetPreReservationPrice

API method "GetPreReservationPrice". Used with distributor_code to return the price of prospective reservation rates, taxes, and fees based on what is defined for your specific distributor_code and reservation type.

| Name | Data Type | Description | Required? |
|---|---|---|---|
| token_key | String (255) | The expiring token key credential provided to you by Streamline | Yes |
| token_secret | String (255) | The expiring token secret credential provided to you by Streamline | Yes |
| distributor_code | String (255) | The code tied to a particular distribution channel. Distribution channels are tied to a reservation type. | Yes |
| pricing_model | String | Defines the pricing model to be used for the reservation. We only support daily pricing with distribution channels. You must pass a value of 1 for pricing_model in each request. | Yes |
| unit_id | Number | The unique ID for the home you are making a reservation or getting information from. | Yes |
| startdate | String (255) | The checkin date for the reservation you are getting pricing for. | Yes |
| enddate | String (255) | The checkout date for the reservation you are getting pricing for. | Yes |
| occupants | Number | The number of adult guests staying at the property. | Yes |
| occupants_small | Number | The number of child guests staying at the property. | Yes |
| pets | Number | Specifies the number of pets that belong to a reservation. | Yes |
| return_payments | Boolean | Must be passed to receive expected charges in the response. | Yes |
| show_due_today | Boolean | Will show a due_today value in the response regardless of auto charging logic and is based upon the companies expected charging rules. Pass a value of: 1, true or yes to activate this logic. | No |
| payment_type_id | Number | The payment type that will be used to pay for the reservation. Important for users using charge type logic on fees, most commonly for a credit card surcharge. The list of payment types for a company can be gotten by using the GetPaymentTypes API method. | No |
| separate_taxes | Boolean | Optional parameter, returns taxes_details_value and required_fees_value, allowing to see the totaled values for each type. | No |

#### Request Example

- XML
- JSON

```xml
<?xml version="1.0" encoding="UTF-8"?>
<methodCall>
 <methodName>GetPreReservationPrice</methodName>
 <params>
  <token_key>YOUR_TOKEN_KEY</token_key>
  <token_secret>YOUR_TOKEN_SECRET</token_secret>
  <distributor_code>YOUR_DISTRIBUTOR_CODE</distributor_code>
  <pricing_model>1</pricing_model>
  <unit_id>288531</unit_id>
  <startdate>01/10/2020</startdate>
  <enddate>01/17/2020</enddate>
  <occupants>2</occupants>
  <occupants_small></occupants_small>
  <pets>1</pets>
  <return_payments>true</return_payments>
  <show_due_today>1</show_due_today>
  <payment_type_id>1</payment_type_id>
  <separate_taxes>1</separate_taxes>
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
        <unit_id>288531</unit_id>
        <price>4189</price>
        <taxes>228.67</taxes>
        <coupon_discount>0.00</coupon_discount>
        <total>4417.67</total>
        <first_day_price>594.31</first_day_price>
        <unit_name>Home</unit_name>
        <location_name>Eutopia</location_name>
        <unit_rewards>1</unit_rewards>
        <company_rewards>1</company_rewards>
        <reward_points_discount></reward_points_discount>
        <required_fees>
            <id>122001</id>
            <name>Cleaning Fees</name>
            <value>100.00</value>
            <description></description>
            <damage_waiver>0</damage_waiver>
            <travel_insurance>0</travel_insurance>
            <cfar>0</cfar>
        </required_fees>
        <taxes_details>
            <id>122031</id>
            <name>Y State Tax</name>
            <value>85.78</value>
            <description></description>
            <damage_waiver>0</damage_waiver>
            <travel_insurance>0</travel_insurance>
            <cfar>0</cfar>
        </taxes_details>
        <taxes_details>
            <id>122027</id>
            <name>Z City Tax</name>
            <value>42.89</value>
            <description></description>
            <damage_waiver>0</damage_waiver>
            <travel_insurance>0</travel_insurance>
            <cfar>0</cfar>
        </taxes_details>
        <reservation_days>
            <date>01/10/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <reservation_days>
            <date>01/11/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <reservation_days>
            <date>01/12/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <reservation_days>
            <date>01/13/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <reservation_days>
            <date>01/14/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <reservation_days>
            <date>01/15/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <reservation_days>
            <date>01/16/2020</date>
            <season_id>16212674</season_id>
            <season>Winter</season>
            <price>577.00</price>
            <extra>0.00</extra>
            <discount>0</discount>
        </reservation_days>
        <currency>USD</currency>
        <security_deposits>
            <security_deposit>
                <ledger_id>2901</ledger_id>
                <description>Guest Security Deposit</description>
                <deposit_required>0.00</deposit_required>
            </security_deposit>
        </security_deposits>
        <security_deposit_text>Security Deposit Required:</security_deposit_text>
        <due_today>4417.67</due_today>
    </data>
</Response>
```
