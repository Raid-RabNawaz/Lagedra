import { describe, it, expect } from "vitest";
import type { AmenityDefinitionDto, ImportedListingDraftDto } from "@/api/types";
import { mapImportedDraftToForm } from "./mapImportedDraftToForm";

const amenities: AmenityDefinitionDto[] = [
  { id: "a-wifi", name: "Wifi", category: "Internet", iconKey: "wifi" },
  { id: "a-kitchen", name: "Kitchen", category: "Kitchen", iconKey: "kitchen" },
  { id: "a-parking", name: "Free Parking", category: "Parking", iconKey: "parking" },
];

function draft(overrides: Partial<ImportedListingDraftDto> = {}): ImportedListingDraftDto {
  return {
    title: null,
    description: null,
    propertyType: null,
    bedrooms: null,
    bathrooms: null,
    squareFootage: null,
    maxGuests: null,
    checkInTime: null,
    checkOutTime: null,
    monthlyRentCents: null,
    nightlyRateCents: null,
    currency: null,
    approxAddress: null,
    amenityHints: null,
    photos: null,
    sourceUrl: null,
    sourceHost: null,
    ...overrides,
  };
}

describe("mapImportedDraftToForm", () => {
  it("derives monthly rent from a nightly rate (nightly x 30)", () => {
    const result = mapImportedDraftToForm(
      draft({ nightlyRateCents: 18000 }), // $180/night
      amenities,
    );

    // 18000 cents * 30 / 100 = 5400 dollars
    expect(result.values.monthlyRentDollars).toBe(5400);
    expect(result.monthlyDerivedFromNightly).toBe(true);
  });

  it("prefers an explicit monthly rent and does not flag derivation", () => {
    const result = mapImportedDraftToForm(
      draft({ monthlyRentCents: 250000, nightlyRateCents: 18000 }),
      amenities,
    );

    expect(result.values.monthlyRentDollars).toBe(2500);
    expect(result.monthlyDerivedFromNightly).toBe(false);
  });

  it("matches amenity hints case-insensitively and drops unknown ones", () => {
    const result = mapImportedDraftToForm(
      draft({ amenityHints: ["wifi", "KITCHEN", "Hot Tub"] }),
      amenities,
    );

    expect(result.values.amenityIds).toEqual(["a-wifi", "a-kitchen"]);
    expect(result.amenitiesMatched).toBe(2);
    expect(result.amenitiesTotal).toBe(3);
  });

  it("matches Airbnb-style amenity names via normalization and synonyms", () => {
    // Mirrors the Lagedra vocabulary that differs from Airbnb's wording only by
    // case, "&" vs "and", plurals, or a known synonym.
    const vocab: AmenityDefinitionDto[] = [
      { id: "a-wifi", name: "WiFi", category: "Internet", iconKey: "wifi" },
      { id: "a-dishes", name: "Dishes & Silverware", category: "Kitchen", iconKey: "utensils" },
      { id: "a-kettle", name: "Kettle", category: "Kitchen", iconKey: "coffee" },
      { id: "a-bodywash", name: "Body Wash", category: "Bathroom", iconKey: "sparkles" },
      { id: "a-fans", name: "Ceiling Fans", category: "ClimateControl", iconKey: "fan" },
      { id: "a-ac", name: "Central Air Conditioning", category: "ClimateControl", iconKey: "snowflake" },
      { id: "a-backyard", name: "Private Backyard", category: "Outdoor", iconKey: "fence" },
      { id: "a-sound", name: "Sound System", category: "LivingArea", iconKey: "speaker" },
      { id: "a-tv", name: "TV", category: "LivingArea", iconKey: "tv" },
    ];

    const result = mapImportedDraftToForm(
      draft({
        amenityHints: [
          "Wifi",
          "Dishes and silverware",
          "Hot water kettle",
          "Shower gel",
          "Ceiling fan",
          "Air conditioning",
          "Backyard",
          "Bluetooth sound system",
          "HDTV with Roku",
          "Wine glasses", // unknown -> dropped
        ],
      }),
      vocab,
    );

    expect(result.values.amenityIds).toEqual(
      expect.arrayContaining([
        "a-wifi",
        "a-dishes",
        "a-kettle",
        "a-bodywash",
        "a-fans",
        "a-ac",
        "a-backyard",
        "a-sound",
        "a-tv",
      ]),
    );
    expect(result.amenitiesMatched).toBe(9);
    expect(result.amenitiesTotal).toBe(10);
  });

  it("maps house rules and cancellation policy from an import", () => {
    const result = mapImportedDraftToForm(
      draft({
        petsAllowed: true,
        smokingAllowed: false,
        partiesAllowed: false,
        quietHoursStart: "22:00",
        quietHoursEnd: "09:00",
        houseRules: "  Please water the plants.\nNo loud music after 10pm.  ",
        cancellationPolicy: "Moderate",
      }),
      amenities,
    );

    expect(result.values.petsAllowed).toBe(true);
    expect(result.values.smokingAllowed).toBe(false);
    expect(result.values.partiesAllowed).toBe(false);
    expect(result.values.quietHoursStart).toBe("22:00");
    expect(result.values.quietHoursEnd).toBe("09:00");
    expect(result.values.additionalRules).toBe("Please water the plants.\nNo loud music after 10pm.");
    expect(result.values.cancellationType).toBe("Moderate");
    expect(result.importedFields).toEqual(
      expect.arrayContaining([
        "Pet policy",
        "Smoking policy",
        "Party policy",
        "Quiet hours",
        "House rules",
        "Cancellation policy",
      ]),
    );
  });

  it("maps Airbnb's strict-family cancellation labels to the Strict tier", () => {
    for (const label of ["Firm", "Strict", "Super Strict 60"]) {
      const result = mapImportedDraftToForm(draft({ cancellationPolicy: label }), amenities);
      expect(result.values.cancellationType).toBe("Strict");
    }

    const flexible = mapImportedDraftToForm(draft({ cancellationPolicy: "Flexible" }), amenities);
    expect(flexible.values.cancellationType).toBe("Flexible");

    const unknown = mapImportedDraftToForm(draft({ cancellationPolicy: "Mystery" }), amenities);
    expect(unknown.values.cancellationType).toBeUndefined();
  });

  it("maps a known property type and omits an unknown one", () => {
    const known = mapImportedDraftToForm(draft({ propertyType: "SingleFamilyResidence" }), amenities);
    expect(known.values.propertyType).toBe("House");

    const unknown = mapImportedDraftToForm(draft({ propertyType: "website" }), amenities);
    expect(unknown.values.propertyType).toBeUndefined();
  });

  it("omits fields that are not present so the form keeps its defaults", () => {
    const result = mapImportedDraftToForm(draft(), amenities);

    expect(result.values).toEqual({});
    expect(result.importedFields).toHaveLength(0);
    expect(result.amenitiesMatched).toBe(0);
  });
});
