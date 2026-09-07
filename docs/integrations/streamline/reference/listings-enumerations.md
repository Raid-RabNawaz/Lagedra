# Listings Enumerations

Captured from the Streamline Partner X portal on 2026-08-27. Example credential values are
replaced with `YOUR_TOKEN_KEY`, `YOUR_TOKEN_SECRET`, and `YOUR_DISTRIBUTOR_CODE`.

Source: `https://partner.streamlinevrs.com/apidocs/general/Listings/Listings-Enumerations`

---
.Cancellation Policy Type Values

| Policy value | Description |
|---|---|
| STRICT | 100% refund if the reservation is canceled at least 60 days before the arrival date |
| FIRM | 100% refund if the reservation is canceled at least 60 days before the arrival date 50% refund if the reservation is canceled at least 30 days before the arrival date |
| MODERATE | 100% refund if the reservation is canceled at least 30 days before the arrival date 50% refund if the reservation is canceled at least 14 days before the arrival date |
| RELAXED | 100% refund if the reservation is canceled at least 14 days before the arrival date 50% refund if the reservation is canceled at least 7 days before the arrival date |
| NO_REFUND | No refund |
| CUSTOM | Terms of the policy defined by the <policyPeriods> child element of <cancellationPolicy> in the Lodging Configuration |

#### Card Code Type Values

| VISA |
|---|
| MASTERCARD |
| DISCOVER |
| AMEX |

#### Currency Type Values

| USD (Only USD supported) |
|---|

#### Pricing Policy Type Values

| GUARANTEED |
|---|

#### Distance Unit Values

| KILOMETRES |
|---|
| MILES |
| METRES |
| MINUTES |

#### Payment Method Type Values

| VISA |
|---|
| MASTERCARD |
| DISCOVER |
| AMEX |
| DEBIT_CARD |
| CREDIT_CARD |

#### Payment Schedule Due Type Values

| BEFORE_CHECKIN |
|---|
| AFTER_CHECKIN |
| AT_BOOKING |
| AT_CHECKOUT |
| AFTER_BOOKING |
| AFTER_CHECKOUT |
| AT_CHECKIN |

#### Place Type Values

| AIRPORT |
|---|
| BEACH |
| GOLF |
| RESTAURANT |
| TRAIN |
| BAR |
| FERRY |
| HIGHWAY |
| SKI |

#### Product Code Type Values

† Not available for use in <fees>:<otherFees> ‡ Not available for use in <fees>:<otherFees> or <fees>:<percentOfRentFees>

| Product code | Display name |
|---|---|
| ADDITIONAL_BED | Additional Bed |
| ADMINISTRATIVE | Administrative Fee |
| AIR_CONDITIONING | Air Conditioning Fee |
| ARRIVAL_EARLY | Early Arrival |
| ARRIVAL_LATE | Late Arrival |
| ASSOCIATION_PROPERTY | Property Association |
| BABY_BED | Baby Bed |
| BOOKING | Booking Fee |
| BOOKING_EARLY | Early Booking |
| BOOKING_LATE | Late Booking |
| CLASS | Class |
| CLEANING† | Cleaning Fee |
| CLUB | Club |
| CONSUMPTION | Consumption Fee |
| CONCIERGE | Concierge |
| DAILY_CLEANING | Daily Cleaning Fee |
| DEPOSIT_DAMAGE§ | Refundable damage deposit |
| DEPARTURE_EARLY | Early Departure |
| DEPARTURE_LATE | Late Departure |
| ELECTRICITY | Electricity Fee |
| ENERGY | Energy |
| EQUIPMENT | Equipment |
| FINAL_CLEANING | Final Cleaning Fee |
| FOOD | Food Fee |
| GARDENING | Gardening |
| GAS | Gas |
| GUEST‡ | Additional Guest Fee |
| HEATING | Heating Fee |
| HIGH_CHAIR | High Chair |
| HOT_TUB | Hot Tub |
| INTERNET | Internet Usage Fee |
| LABOR | Labor |
| LAUNDRY | Laundry Fee |
| LINENS | Linen Fee |
| LINENS_BATH | Bath Linens |
| LINENS_BED | Bed Linens |
| MANAGEMENT | Management |
| OIL | Oil |
| ON_SITE_PAYMENT_METHOD | On-site Payment Method |
| PARKING | Parking Fee |
| PET‡ | Pet Fee |
| PHONE | Phone |
| POOL | Pool |
| POOL_HEATING | Pool Heating |
| PROPERTY_FEE | Property Fee |
| RENT | *Not included in RENT |
| RESERVATION | Reservation Fee |
| RESORT | Resort Fee |
| SERVICE | Service fee |
| SPA | Spa |
| TAX | Taxes |
| TOILETRIES | Toiletries |
| TOUR | Tour |
| TRANSPORTATION | Transportation |
| UTENSILS_CLEANING | Cleaning Utensils |
| UTENSILS_FOOD | Food Utensils |
| VEHICLE | Vehicle Usage Fee |
| WAIVER_DAMAGE | Damage Waiver |
| WATER | Water |
| WATER_CRAFT | Water Craft |
| WATER_CRAFT_MOORING | Water Craft Mooring |
| WATER_DRINKING | Drinking Water |

#### Property Type Values

| PROPERTY_TYPE_APARTMENT |
|---|
| PROPERTY_TYPE_BARN |
| PROPERTY_TYPE_BED_AND_BREAKFAST |
| PROPERTY_TYPE_BOAT |
| PROPERTY_TYPE_BUILDING |
| PROPERTY_TYPE_BUNGALOW |
| PROPERTY_TYPE_CABIN |
| PROPERTY_TYPE_CAMPGROUND |
| PROPERTY_TYPE_CARAVAN |
| PROPERTY_TYPE_CASTLE |
| PROPERTY_TYPE_CHACARA |
| PROPERTY_TYPE_CHALET |
| PROPERTY_TYPE_CHATEAU |
| PROPERTY_TYPE_CONDO |
| PROPERTY_TYPE_CORPORATE_APARTMENT |
| PROPERTY_TYPE_COTTAGE |
| PROPERTY_TYPE_ESTATE |
| PROPERTY_TYPE_FARMHOUSE |
| PROPERTY_TYPE_GUESTHOUSE |
| PROPERTY_TYPE_HOSTEL |
| PROPERTY_TYPE_HOTEL |
| PROPERTY_TYPE_HOUSE |
| PROPERTY_TYPE_HOUSE_BOAT |
| PROPERTY_TYPE_LODGE |
| PROPERTY_TYPE_MAS |
| PROPERTY_TYPE_MILL |
| PROPERTY_TYPE_MOBILE_HOME |
| PROPERTY_TYPE_RECREATIONAL_VEHICLE |
| PROPERTY_TYPE_RESORT |
| PROPERTY_TYPE_RIAD |
| PROPERTY_TYPE_STUDIO |
| PROPERTY_TYPE_TOWER |
| PROPERTY_TYPE_TOWNHOME |
| PROPERTY_TYPE_VILLA |
| PROPERTY_TYPE_YACHT |

#### Room Type Values

| Other Sleeping Area (OTHER_SLEEPING_AREA) |
|---|
| Living Sleeping Combo (LIVING_SLEEPING_COMBO) |
| Bedroom (BEDROOM) |

#### Bedroom Feature Values

| King (AMENITY_KING) |
|---|
| Queen (AMENITY_QUEEN) |
| Double (AMENITY_DOUBLE) |
| Twin Single (AMENITY_TWIN_SINGLE) |
| Crib (AMENITY_CRIB) |
| Child Bed (AMENITY_CHILD_BED) |
| Sleep Sofa (AMENITY_SLEEP_SOFA) |
| Bunk Bed (AMENITY_BUNK_BED) |
| Murphy Bed (AMENITY_MURPHY_BED) |

#### Bathroom Type Values

| Full Bath (FULL_BATH) |
|---|
| Half Bath (HALF_BATH) |
| Shower Indoor or Outdoor (SHOWER_INDOOR_OR_OUTDOOR) |

#### Bathroom Feature Values

| Toilet (AMENITY_TOILET) |
|---|
| Tub (AMENITY_TUB) |
| Jetted Tub (AMENITY_JETTED_TUB) |
| Outdoor Shower (AMENITY_OUTDOOR_SHOWER) |
| Combo Tub Shower (AMENITY_COMBO_TUB_SHOWER) |
| Shower (AMENITY_SHOWER) |
| Bidet (AMENITY_BIDET) |

#### Safety Feature Values

| Carbon Monoxide Detector (CARBON_MONOXIDE_DETECTOR) |
|---|
| First Aid Kit (FIRST_AID_KIT) |
| Fire Extinguisher (FIRE_EXTINGUISHER) |
| Smoke Detector (SMOKE_DETECTOR) |
| Deadbolt Lock (DEADBOLT_LOCK) |
| Outdoor Lighting (OUTDOOR_LIGHTING) |

#### Cleanliness Feature Values

| Enhanced Cleaning Practices (ENHANCED_CLEANING_PRACTICES) |
|---|
| Cleaning Disinfection (CLEANING_DISINFECTION) |
| Self Check In / Check Out (SELF_CHECKIN_CHECKOUT) |
| Guest Gap Period - 24 Hours (GUEST_GAP_PERIOD_24_HOURS) |
| Guest Gap Period - 48 Hours (GUEST_GAP_PERIOD_48_HOURS) |
| Guest Gap Period - 72 Hours (GUEST_GAP_PERIOD_72_HOURS) |
| All towels and bedding washed in hot water that’s at least 60ºC (LINENS_HIGH_TEMP_WASH) |
| High-touch surfaces cleaned with disinfectant (COMMON_SURFACE_DISINFECTANT_CLEANED) |

#### Emergency Feature Values

| Emergency Exit Route (EMERGENCY_EXIT_ROUTE) |
|---|
| Emergency Medical Contact (MEDICAL_EMERGENCY_CONTACT) |
| Emergency Police Contact (POLICE_EMERGENCY_CONTACT) |
| Emergency Fire Contact (FIRE_EMERGENCY_CONTACT) |

#### Unit Feature Values

| Kitchen and Dining |
|---|
| Kitchen (KITCHEN_DINING_KITCHEN) |
| Dining Area (KITCHEN_DINING_AREA) |
| Refrigerator (KITCHEN_DINING_REFRIGERATOR) |
| Coffee Maker (KITCHEN_DINING_COFFEE_MAKER) |
| Microwave (KITCHEN_DINING_MICROWAVE) |
| Dishwasher (KITCHEN_DINING_DISHWASHER) |
| Dishes Utensils (KITCHEN_DINING_DISHES_UTENSILS) |
| Spices (KITCHEN_DINING_SPICES) |
| Stove (KITCHEN_DINING_STOVE) |
| Ice Maker (KITCHEN_DINING_ICE_MAKER) |
| Highchair (KITCHEN_DINING_HIGHCHAIR) |
| Toaster (KITCHEN_DINING_TOASTER) |
| Oven (KITCHEN_DINING_OVEN) |
| Room (KITCHEN_DINING_ROOM) |

| Amenities |
|---|
| Internet (AMENITIES_INTERNET) |
| Fireplace (AMENITIES_FIREPLACE) |
| Wood Stove (AMENITIES_WOOD_STOVE) |
| Air Conditioning (AMENITIES_AIR_CONDITIONING) |
| Heating (AMENITIES_HEATING) |
| Washer (AMENITIES_WASHER) |
| Dryer (AMENITIES_DRYER) |
| Parking (AMENITIES_PARKING) |
| Garage (AMENITIES_GARAGE) |
| Telephone (AMENITIES_TELEPHONE) |
| Living Room (AMENITIES_LIVING_ROOM) |
| Game Room (AMENITIES_GAME_ROOM) |
| Fitness Room (AMENITIES_FITNESS_ROOM) |
| Hair Dryer (AMENITIES_HAIR_DRYER) |
| Iron Board (AMENITIES_IRON_BOARD) |
| Linens (AMENITIES_LINENS) |
| Towels (AMENITIES_TOWELS) |
| Elevator (AMENITIES_ELEVATOR) |

| Entertainment |
|---|
| Television (ENTERTAINMENT_TELEVISION) |
| Stereo (ENTERTAINMENT_STEREO) |
| Video Library (ENTERTAINMENT_VIDEO_LIBRARY) |
| Music Library (ENTERTAINMENT_MUSIC_LIBRARY) |
| Games (ENTERTAINMENT_GAMES) |
| Video Games (ENTERTAINMENT_VIDEO_GAMES) |
| Toys (ENTERTAINMENT_TOYS) |
| Pool Table (ENTERTAINMENT_POOL_TABLE) |
| Ping Pong Table (ENTERTAINMENT_PING_PONG_TABLE) |
| DVD (ENTERTAINMENT_DVD) |
| Satellite or Cable (ENTERTAINMENT_SATELLITE_OR_CABLE) |
| Foosball (ENTERTAINMENT_FOOSBALL) |
| Books (ENTERTAINMENT_BOOKS) |

| Outdoor |
|---|
| Grill (OUTDOOR_GRILL) |
| Bicycle (OUTDOOR_BICYCLE) |
| Deck Patio Uncovered (OUTDOOR_DECK_PATIO_UNCOVERED) |
| Balcony (OUTDOOR_BALCONY) |
| Garden (OUTDOOR_GARDEN) |
| Tennis (OUTDOOR_TENNIS) |
| Boat (OUTDOOR_BOAT) |
| Kayak Canoe (OUTDOOR_KAYAK_CANOE) |
| Snow Sports Gear (OUTDOOR_SNOW_SPORTS_GEAR) |
| Water Sports Gear (OUTDOOR_WATER_SPORTS_GEAR) |
| Golf (OUTDOOR_GOLF) |
| Veranda (OUTDOOR_VERANDA) |

| Pool/Spa |
|---|
| Communal Pool (POOL_SPA_COMMUNAL_POOL) |
| Hot Tub (POOL_SPA_HOT_TUB) |
| Indoor Pool (POOL_SPA_INDOOR_POOL) |
| Private Pool (POOL_SPA_PRIVATE_POOL) |
| Sauna (POOL_SPA_SAUNA) |
| Heated Pool (POOL_SPA_HEATED_POOL) |

| Accommodations |
|---|
| Breakfast Booking Possible (ACCOMMODATIONS_BREAKFAST_BOOKING_POSSIBLE) |
| Breakfast Included in Price (ACCOMMODATIONS_BREAKFAST_INCLUDED_IN_PRICE) |
| House Cleaning Included (ACCOMMODATIONS_HOUSE_CLEANING_INCLUDED) |
| House Cleaning Optional (ACCOMMODATIONS_HOUSE_CLEANING_OPTIONAL) |
| Other Services Chauffeur (ACCOMMODATIONS_OTHER_SERVICES_CHAUFFEUR) |
| Other Services Concierge (ACCOMMODATIONS_OTHER_SERVICES_CONCIERGE) |
| Other Services Private Chef (ACCOMMODATIONS_OTHER_SERVICES_PRIVATE_CHEF) |
| Other Services Massage (ACCOMMODATIONS_OTHER_SERVICES_MASSAGE) |
| Other Services Car Available (ACCOMMODATIONS_OTHER_SERVICES_CAR_AVAILABLE) |

| Themes |
|---|
| Family (THEMES_FAMILY) |
| Romantic (THEMES_ROMANTIC) |
| Historic (THEMES_HISTORIC) |

| Suitability |
|---|
| Accessibility Wheelchair Accessible (SUITABILITY_ACCESSIBILITY_WHEELCHAIR_ACCESSIBLE) |
| Accessibility Wheelchair Inaccessible (SUITABILITY_ACCESSIBILITY_WHEELCHAIR_INACCESSIBLE) |

#### Listing Feature Values

| Sports and Adventure |
|---|
| Basketball Court (SPORTS_BASKETBALL_COURT) |
| Cycling (SPORTS_CYCLING) |
| Deepsea Fishing (SPORTS_DEEPSEA_FISHING) |
| Fishing (SPORTS_FISHING) |
| Fishing Fly (SPORTS_FISHING_FLY) |
| Fishing Freshwater (SPORTS_FISHING_FRESHWATER) |
| Golf (SPORTS_GOLF) |
| Golf Optional (SPORTS_GOLF_OPTIONAL) |
| Hiking (SPORTS_HIKING) |
| Hunting (SPORTS_HUNTING) |
| Ice Skating (SPORTS_ICE_SKATING) |
| Jet Skiing (SPORTS_JET_SKIING) |
| Mountain Biking (SPORTS_MOUNTAIN_BIKING) |
| Mountain Climbing (SPORTS_MOUNTAIN_CLIMBING) |
| Mountaineering (SPORTS_MOUNTAINEERING) |
| Pier Fishing (SPORTS_PIER_FISHING) |
| Rafting (SPORTS_RAFTING) |
| Sailing (SPORTS_SAILING) |
| Scuba or Snorkeling (SPORTS_SCUBA_OR_SNORKELING) |
| Ski Lift Priviledges (SPORTS_SKI_LIFT_PRIVILEGES) |
| Ski Lift Priviledges Optional (SPORTS_SKI_LIFT_PRIVILEGES_OPTIONAL) |
| Skiing (SPORTS_SKIING) |
| Snorkeling (SPORTS_SNORKELING) |
| Fishing Bay (SPORTS_FISHING_BAY) |
| Spelunking (SPORTS_SPELUNKING) |
| Fishing Surf (SPORTS_FISHING_SURF) |
| Surfing (SPORTS_SURFING) |
| Swimming (SPORTS_SWIMMING) |
| Skiing Water (SPORTS_SKIING_WATER) |
| Tubing Water (SPORTS_TUBING_WATER) |
| Whitewater Rafting (SPORTS_WHITEWATER_RAFTING) |
| Wind Surfing (SPORTS_WIND_SURFING) |
| Cross Country Skiing (SPORTS_CROSS_COUNTRY_SKIING) |
| Parasailing (SPORTS_PARASAILING) |
| Rock Climbing (SPORTS_ROCK_CLIMBING) |
| Kayaking (SPORTS_KAYAKING) |
| Snowboarding (SPORTS_SNOWBOARDING) |
| Snowmobiling (SPORTS_SNOWMOBILING) |
| Snorkeling / Diving (SPORTS_SNORKELING_DIVING) |

| Car |
|---|
| Necessary (CAR_NECESSARY) |
| Not Necessary (CAR_NOT_NECESSARY) |
| Recommended (CAR_RECOMMENDED) |

| General |
|---|
| EV_CAR_CHARGER |
| FIRE_PIT |

| Location Types |
|---|
| Beach (LOCATION_TYPE_BEACH) |
| Downtown (LOCATION_TYPE_DOWNTOWN) |
| Lake (LOCATION_TYPE_LAKE) |
| Mountain (LOCATION_TYPE_MOUNTAIN) |
| Near Ocean (LOCATION_TYPE_NEAR_OCEAN) |
| Resort (LOCATION_TYPE_RESORT) |
| River (LOCATION_TYPE_RIVER) |
| Rural (LOCATION_TYPE_RURAL) |
| Town (LOCATION_TYPE_TOWN) |
| Village (LOCATION_TYPE_VILLAGE) |
| Waterfront (LOCATION_TYPE_WATERFRONT) |
| Beach Front (LOCATION_TYPE_BEACH_FRONT) |
| Beach View (LOCATION_TYPE_BEACH_VIEW) |
| Golf Course Front (LOCATION_TYPE_GOLF_COURSE_FRONT) |
| Golf Course View (LOCATION_TYPE_GOLF_COURSE_VIEW) |
| Lake Front (LOCATION_TYPE_LAKE_FRONT) |
| Lake View (LOCATION_TYPE_LAKE_VIEW) |
| Ocean Front (LOCATION_TYPE_OCEAN_FRONT) |
| Ocean View (LOCATION_TYPE_OCEAN_VIEW) |
| Ski In (LOCATION_TYPE_SKI_IN) |
| Ski Out (LOCATION_TYPE_SKI_OUT) |
| Water View (LOCATION_TYPE_WATER_VIEW) |
| Mountain View (LOCATION_TYPE_MOUNTAIN_VIEW) |
| Ski In / Out (LOCATION_TYPE_SKI_IN_OUT) |

| Attractions |
|---|
| Bay (ATTRACTIONS_BAY) |
| Coin Laundry (ATTRACTIONS_COIN_LAUNDRY) |
| Duty Free (ATTRACTIONS_DUTY_FREE) |
| Marina (ATTRACTIONS_MARINA) |
| Museums (ATTRACTIONS_MUSEUMS) |
| Theme Parks (ATTRACTIONS_THEME_PARKS) |
| Water Parks (ATTRACTIONS_WATER_PARKS) |
| Winery Tours (ATTRACTIONS_WINERY_TOURS) |
| Zoo (ATTRACTIONS_ZOO) |
| Health Beauty Spa (ATTRACTIONS_HEALTH_BEAUTY_SPA) |

| Leisure |
|---|
| Antiquing (LEISURE_ANTIQUING) |
| Bird Watching (LEISURE_BIRD_WATCHING) |
| Eco Tourism (LEISURE_ECO_TOURISM) |
| Gambling (LEISURE_GAMBLING) |
| Horseback Riding (LEISURE_HORSEBACK_RIDING) |
| Outlet Shopping (LEISURE_OUTLET_SHOPPING) |
| Paddle Boating (LEISURE_PADDLE_BOATING) |
| Sledding (LEISURE_SLEDDING) |
| Whale Watching (LEISURE_WHALE_WATCHING) |
| Boating (LEISURE_BOATING) |
| Shopping (LEISURE_SHOPPING) |
| Water Sports (LEISURE_WATER_SPORTS) |
| Wildlife Viewing (LEISURE_WILDLIFE_VIEWING) |

| Local Features |
|---|
| Fitness Center (LOCAL_FITNESS_CENTER) |
| Hospital (LOCAL_HOSPITAL) |
| Laundromat (LOCAL_LAUNDROMAT) |
