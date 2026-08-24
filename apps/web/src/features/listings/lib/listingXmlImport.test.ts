// @vitest-environment jsdom
import { describe, it, expect } from "vitest";
import type { AmenityDefinitionDto } from "@/api/types";
import { buildListingXmlTemplate, parseListingImportXml } from "./listingXmlImport";

const amenities: AmenityDefinitionDto[] = [
  { id: "a-wifi", name: "WiFi", category: "Internet", iconKey: "wifi" },
  { id: "a-dishwasher", name: "Dishwasher", category: "Kitchen", iconKey: "dw" },
];

const VALID_DESCRIPTION =
  "Bright, fully furnished two-bedroom apartment with a dedicated workspace and fast Wi-Fi.";

type FieldMap = Record<string, string | null>;

function validFields(overrides: FieldMap = {}): FieldMap {
  return {
    title: "Sunny riverside apartment",
    description: VALID_DESCRIPTION,
    propertyType: "Apartment",
    monthlyRent: "2400",
    maxDeposit: "2400",
    bedrooms: "2",
    bathrooms: "1.5",
    ...overrides,
  };
}

/** Values are inserted verbatim, so nested markup (e.g. <amenity>) is allowed. */
function listingXml(fields: FieldMap): string {
  const body = Object.entries(fields)
    .filter(([, value]) => value !== null)
    .map(([name, value]) => `<${name}>${value}</${name}>`)
    .join("");
  return `<listing>${body}</listing>`;
}

function fileWith(...listings: string[]): string {
  return `<?xml version="1.0" encoding="UTF-8"?><listings>${listings.join("")}</listings>`;
}

describe("parseListingImportXml", () => {
  it("parses a valid listing into complete form values with defaults applied", () => {
    const data = fileWith(
      listingXml(
        validFields({
          maxGuests: "4",
          checkInTime: "16:00",
          petsAllowed: "Yes",
          amenities: "<amenity>wifi</amenity><amenity>Dishwasher</amenity>",
          instantBooking: "Yes",
        }),
      ),
    );

    const result = parseListingImportXml(data, amenities);

    expect(result.fileErrors).toEqual([]);
    expect(result.rows).toHaveLength(1);
    const row = result.rows[0];
    expect(row.errors).toEqual([]);
    expect(row.values).not.toBeNull();
    expect(row.values!.title).toBe("Sunny riverside apartment");
    expect(row.values!.monthlyRentDollars).toBe(2400);
    expect(row.values!.bathrooms).toBe(1.5);
    expect(row.values!.maxGuests).toBe(4);
    expect(row.values!.checkInTime).toBe("16:00");
    expect(row.values!.petsAllowed).toBe(true);
    expect(row.values!.instantBookingEnabled).toBe(true);
    expect(row.values!.amenityIds).toEqual(["a-wifi", "a-dishwasher"]);
    // Defaults for fields left out.
    expect(row.values!.minStayDays).toBe(30);
    expect(row.values!.maxStayDays).toBe(180);
    expect(row.values!.checkOutTime).toBe("11:00");
  });

  it("accepts comma-separated amenities text as an alternative to <amenity> elements", () => {
    const data = fileWith(listingXml(validFields({ amenities: "wifi, Dishwasher" })));

    const result = parseListingImportXml(data, amenities);

    expect(result.rows[0].values!.amenityIds).toEqual(["a-wifi", "a-dishwasher"]);
  });

  it("reads address and photo URLs from the Lagedra template format", () => {
    const data = fileWith(
      listingXml(
        validFields({
          streetAddress: "2400 5th Ave",
          unitNumber: "131",
          city: "San Diego",
          state: "ca",
          zipCode: "92101",
          photos: "<photo>https://cdn.example.com/a.jpg</photo><photo>https://cdn.example.com/b.jpg</photo>",
        }),
      ),
    );

    const result = parseListingImportXml(data, amenities);
    const row = result.rows[0];
    expect(row.values).not.toBeNull();
    expect(row.address).toEqual({
      street: "2400 5th Ave, Unit 131",
      city: "San Diego",
      state: "CA",
      zipCode: "92101",
      country: "US",
    });
    expect(row.photoUrls).toEqual([
      "https://cdn.example.com/a.jpg",
      "https://cdn.example.com/b.jpg",
    ]);
  });

  it("matches element names forgivingly (case, dashes, label aliases)", () => {
    const data = fileWith(
      listingXml(
        validFields({
          monthlyRent: null,
          "monthly-rent": "2400",
          "min-stay": "60",
        }),
      ),
    );

    const result = parseListingImportXml(data, amenities);

    const row = result.rows[0];
    expect(row.values).not.toBeNull();
    expect(row.values!.monthlyRentDollars).toBe(2400);
    expect(row.values!.minStayDays).toBe(60);
  });

  it("rejects listings with missing required fields and reports which", () => {
    const data = fileWith(
      listingXml(validFields()),
      listingXml(validFields({ monthlyRent: null, propertyType: null })),
    );

    const result = parseListingImportXml(data, amenities);

    expect(result.rows).toHaveLength(2);
    const row = result.rows[1];
    expect(row.rowNumber).toBe(2);
    expect(row.values).toBeNull();
    expect(row.errors).toContain("Monthly rent (USD) is required");
    expect(row.errors).toContain("Property type is required");
  });

  it("rejects invalid values with labeled validation messages", () => {
    const data = fileWith(
      listingXml(validFields({ title: "Hey", description: "Too short", bathrooms: "0" })),
    );

    const result = parseListingImportXml(data, amenities);

    const row = result.rows[0];
    expect(row.values).toBeNull();
    expect(row.errors.some((e) => e.startsWith("Title:"))).toBe(true);
    expect(row.errors.some((e) => e.startsWith("Description:"))).toBe(true);
    expect(row.errors.some((e) => e.startsWith("Bathrooms:"))).toBe(true);
  });

  it("warns about unmatched amenities but keeps the listing valid", () => {
    const data = fileWith(listingXml(validFields({ amenities: "WiFi, Golden Slide" })));

    const result = parseListingImportXml(data, amenities);

    const row = result.rows[0];
    expect(row.values).not.toBeNull();
    expect(row.values!.amenityIds).toEqual(["a-wifi"]);
    expect(row.warnings[0]).toContain("Golden Slide");
  });

  it("warns about unrecognized elements but keeps the listing valid", () => {
    const data = fileWith(listingXml(validFields({ price: "123" })));

    const result = parseListingImportXml(data, amenities);

    const row = result.rows[0];
    expect(row.values).not.toBeNull();
    expect(row.warnings.some((w) => w.includes("<price>"))).toBe(true);
  });

  it("skips empty listing elements and reports a file error when nothing is left", () => {
    const data = fileWith("<listing></listing>", "<listing><title></title></listing>");

    const result = parseListingImportXml(data, amenities);

    expect(result.rows).toEqual([]);
    expect(result.fileErrors).toHaveLength(1);
  });

  it("rejects files that are not well-formed XML", () => {
    const result = parseListingImportXml("<listings><listing></listings>", amenities);

    expect(result.rows).toEqual([]);
    expect(result.fileErrors[0]).toContain("could not be read as XML");
  });

  it("rejects files without <listing> or <property> elements", () => {
    const result = parseListingImportXml("<products><item/></products>", amenities);

    expect(result.rows).toEqual([]);
    expect(result.fileErrors[0]).toContain("No <listing> or <property> elements");
  });
});

describe("parseListingImportXml with Zillow/HotPads-style feeds", () => {
  const feedAmenityDefs: AmenityDefinitionDto[] = [
    ...amenities,
    { id: "a-ac", name: "Central Air Conditioning", category: "ClimateControl", iconKey: "ac" },
    { id: "a-pool", name: "Pool", category: "Outdoor", iconKey: "pool" },
  ];

  const FEED_DESCRIPTION =
    "Fully furnished mid-term rental available for flexible stays of 30 days or longer in Bankers Hill.";

  function propertyXml(overrides: Partial<Record<string, string | null>> = {}): string {
    const fields: Record<string, string | null> = {
      "listing-title": "At Laurel bay,BH-131(FURNISHED RENTAL)",
      description: FEED_DESCRIPTION,
      "property-type": "apartment/condo/townhouse",
      price: " 4525.00",
      "price-term": "month",
      "security-deposit": "500",
      "num-bedrooms": "1",
      "num-bathrooms": "1",
      minimumstay: "",
      terms: "",
      amenities: "",
      extras: "",
      ...overrides,
    };
    return [
      "<property>",
      "<location>",
      "<street-address>2400 5th Ave</street-address>",
      "<unit-number>131</unit-number>",
      "<city-name>San Diego</city-name>",
      "<zipcode>92101</zipcode>",
      "<state-code>Ca</state-code>",
      "</location>",
      "<details>",
      fields.terms === null ? "" : `<Terms>${fields.terms}</Terms>`,
      fields.minimumstay === null ? "" : `<MinimumStay>${fields.minimumstay}</MinimumStay>`,
      fields.price === null ? "" : `<price>${fields.price}</price>`,
      `<listing-title>${fields["listing-title"] ?? ""}</listing-title>`,
      `<num-bedrooms>${fields["num-bedrooms"] ?? ""}</num-bedrooms>`,
      `<num-bathrooms>${fields["num-bathrooms"] ?? ""}</num-bathrooms>`,
      `<property-type>${fields["property-type"] ?? ""}</property-type>`,
      `<description>${fields.description ?? ""}</description>`,
      "</details>",
      "<rental-terms>",
      `<price-term>${fields["price-term"] ?? ""}</price-term>`,
      `<security-deposit>${fields["security-deposit"] ?? ""}</security-deposit>`,
      "</rental-terms>",
      "<detailed-characteristics>",
      `<amenities>${fields.amenities ?? ""}</amenities>`,
      fields.extras ?? "",
      "</detailed-characteristics>",
      "</property>",
    ].join("");
  }

  function feedWith(...properties: string[]): string {
    return `<?xml version="1.0" encoding="UTF-8"?><properties UnitCount="${properties.length}">${properties.join("")}</properties>`;
  }

  it("maps feed properties to complete form values", () => {
    const result = parseListingImportXml(feedWith(propertyXml()), feedAmenityDefs);

    expect(result.fileErrors).toEqual([]);
    expect(result.rows).toHaveLength(1);
    const row = result.rows[0];
    expect(row.errors).toEqual([]);
    expect(row.values).not.toBeNull();
    expect(row.values!.title).toBe("At Laurel bay,BH-131(FURNISHED RENTAL)");
    expect(row.values!.propertyType).toBe("Apartment");
    expect(row.values!.monthlyRentDollars).toBe(4525);
    expect(row.values!.maxDepositDollars).toBe(500);
    expect(row.values!.bedrooms).toBe(1);
    expect(row.values!.bathrooms).toBe(1);
    // Feed has no min-stay info here — defaults apply.
    expect(row.values!.minStayDays).toBe(30);
    expect(row.address).toEqual({
      street: "2400 5th Ave, Unit 131",
      city: "San Diego",
      state: "CA",
      zipCode: "92101",
      country: "US",
    });
    expect(row.photoUrls).toEqual([]);
  });

  it("reads minimum stay from <MinimumStay> or free-text <Terms>", () => {
    const result = parseListingImportXml(
      feedWith(
        propertyXml({ minimumstay: "60" }),
        propertyXml({ terms: "45 days min" }),
        propertyXml({ minimumstay: "3" }), // below platform floor — default applies
      ),
      feedAmenityDefs,
    );

    expect(result.rows[0].values!.minStayDays).toBe(60);
    expect(result.rows[1].values!.minStayDays).toBe(45);
    expect(result.rows[2].values!.minStayDays).toBe(30);
  });

  it("matches amenities from the blob (with synonyms) and resolvable has-* flags", () => {
    const result = parseListingImportXml(
      feedWith(
        propertyXml({
          amenities: "Kitchen,Air conditioning,Dishwasher,800 sq.ft.",
          extras: "<has-pool>yes</has-pool><has-sauna>yes</has-sauna><has-microwave>no</has-microwave>",
        }),
      ),
      feedAmenityDefs,
    );

    const row = result.rows[0];
    expect(row.values).not.toBeNull();
    // Dishwasher exact, Air conditioning via synonym, Pool via has-pool flag.
    expect([...row.values!.amenityIds].sort()).toEqual(["a-ac", "a-dishwasher", "a-pool"]);
    // Unmatched blob entries warn; unresolvable flags (sauna) don't.
    expect(row.warnings[0]).toContain("Kitchen");
    expect(row.warnings[0]).not.toContain("sauna");
  });

  it("rejects non-monthly price terms instead of importing a wrong rent", () => {
    const result = parseListingImportXml(
      feedWith(propertyXml({ "price-term": "week" })),
      feedAmenityDefs,
    );

    const row = result.rows[0];
    expect(row.values).toBeNull();
    expect(row.errors[0]).toContain('Price term "week" is not supported');
  });

  it("derives bathrooms from full + half counts when num-bathrooms is empty", () => {
    const result = parseListingImportXml(
      feedWith(
        propertyXml({
          "num-bathrooms": "",
          extras: "",
          description: FEED_DESCRIPTION,
        }).replace(
          "<num-bathrooms></num-bathrooms>",
          "<num-full-bathrooms>1</num-full-bathrooms><num-half-bathrooms>1</num-half-bathrooms><num-bathrooms></num-bathrooms>",
        ),
      ),
      feedAmenityDefs,
    );

    expect(result.rows[0].values!.bathrooms).toBe(1.5);
  });

  it("reports rows with missing required feed fields honestly", () => {
    const result = parseListingImportXml(
      feedWith(propertyXml(), propertyXml({ description: "", price: "" })),
      feedAmenityDefs,
    );

    expect(result.rows).toHaveLength(2);
    expect(result.rows[1].rowNumber).toBe(2);
    expect(result.rows[1].values).toBeNull();
    expect(result.rows[1].errors).toContain("Description is required");
    expect(result.rows[1].errors).toContain("Monthly rent (USD) is required");
  });

  it("collects picture URLs and strips the FURNISHED RENTAL street prefix", () => {
    const result = parseListingImportXml(
      feedWith(
        propertyXml({
          extras: "",
        }).replace(
          "<street-address>2400 5th Ave</street-address>",
          "<street-address>FURNISHED RENTAL 2400 5th Ave</street-address>",
        ).replace(
          "</detailed-characteristics>",
          `</detailed-characteristics><pictures>
            <picture><picture-url>http://www.aaxsys.com/units/BH-131/R01.JPG</picture-url></picture>
            <picture><picture-url>http://www.aaxsys.com/units/BH-131/R02.JPG</picture-url></picture>
            <picture><picture-url>http://www.aaxsys.com/units/BH-131/R01.JPG</picture-url></picture>
          </pictures>`,
        ),
      ),
      feedAmenityDefs,
    );

    const row = result.rows[0];
    expect(row.address!.street).toBe("2400 5th Ave, Unit 131");
    expect(row.photoUrls).toEqual([
      "http://www.aaxsys.com/units/BH-131/R01.JPG",
      "http://www.aaxsys.com/units/BH-131/R02.JPG",
    ]);
  });

  it("warns when location is only partially filled in", () => {
    const result = parseListingImportXml(
      feedWith(
        propertyXml().replace(
          /<location>[\s\S]*<\/location>/,
          "<location><city-name>San Diego</city-name><zipcode>92101</zipcode></location>",
        ),
      ),
      feedAmenityDefs,
    );

    expect(result.rows[0].values).not.toBeNull();
    expect(result.rows[0].address).toBeNull();
    expect(result.rows[0].warnings.some((w) => w.includes("Address skipped"))).toBe(true);
  });
});

describe("buildListingXmlTemplate", () => {
  it("produces well-formed XML whose blank skeleton imports nothing", () => {
    const template = buildListingXmlTemplate(amenities);
    const doc = new DOMParser().parseFromString(template, "text/xml");
    expect(doc.querySelector("parsererror")).toBeNull();

    // Uploading the untouched template must not create listings.
    const result = parseListingImportXml(template, amenities);
    expect(result.rows).toEqual([]);
    expect(result.fileErrors[0]).toContain("No listings were found");
  });

  it("round-trips the commented example listing as a valid import", () => {
    const template = buildListingXmlTemplate(amenities);
    const doc = new DOMParser().parseFromString(template, "text/xml");

    // The example lives inside a comment so it is ignored on upload; the
    // closing tag distinguishes it from the instructions comment.
    const comments: string[] = [];
    const collect = (node: Node) => {
      node.childNodes.forEach((child) => {
        if (child.nodeType === Node.COMMENT_NODE) comments.push(child.nodeValue ?? "");
        collect(child);
      });
    };
    collect(doc);
    const example = comments.find((c) => c.includes("</listing>"));
    expect(example).toBeTruthy();

    const result = parseListingImportXml(`<listings>${example!}</listings>`, amenities);

    expect(result.fileErrors).toEqual([]);
    expect(result.rows).toHaveLength(1);
    expect(result.rows[0].errors).toEqual([]);
    expect(result.rows[0].values).not.toBeNull();
    expect(result.rows[0].values!.monthlyRentDollars).toBe(2400);
  });
});
