import type { AmenityDefinitionDto } from "@/api/types";
import {
  LISTING_IMPORT_COLUMNS,
  MAX_IMPORT_PHOTOS,
  MAX_IMPORT_ROWS,
  isBlank,
  mapRowToListing,
  normalizeImportName,
  type ListingImportColumn,
  type ParsedListingAddress,
  type ParsedListingFile,
  type ParsedListingRow,
  type RawRecord,
} from "./listingImportShared";
import {
  matchPropertyType,
  normalizeAmenityName,
  AMENITY_SYNONYMS,
} from "./mapImportedDraftToForm";

/**
 * Bulk "Import from XML" support, the sibling of listingExcelImport.ts. One
 * module owns both the downloadable template and the parser: the template is
 * generated from LISTING_IMPORT_COLUMNS and uploaded elements are matched
 * back to the same columns by (normalized) element name, so <monthlyRent>,
 * <monthly-rent> and <MonthlyRent> all work. Row validation is shared with
 * the Excel importer, so both formats accept exactly the same values.
 *
 * Uploads in the Zillow/HotPads rental-feed syndication format (a root with
 * <property> entries, as produced by e.g. Aaxsys) are detected and mapped to
 * the same fields automatically, so hosts can upload a feed export as-is.
 */

/** Element names are the column keys, e.g. <monthlyRent>. */
const COLUMN_BY_NORMALIZED_NAME = new Map<string, ListingImportColumn>();
for (const column of LISTING_IMPORT_COLUMNS) {
  COLUMN_BY_NORMALIZED_NAME.set(normalizeImportName(column.key), column);
  // Also accept the human label, e.g. <min-stay> for "Min stay (days)".
  COLUMN_BY_NORMALIZED_NAME.set(normalizeImportName(column.label), column);
}

// ── Template generation ─────────────────────────────────────────

function escapeXml(text: string): string {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

/** XML comments must not contain "--"; collapse hyphen runs in user text. */
function commentSafe(text: string): string {
  return text.replace(/-{2,}/g, "-");
}

function listingBlock(withExampleValues: boolean): string {
  const lines: string[] = ["  <listing>"];
  for (const column of LISTING_IMPORT_COLUMNS) {
    if (column.key === "amenities") {
      lines.push("    <amenities>");
      if (withExampleValues) {
        for (const name of String(column.example).split(",")) {
          lines.push(`      <amenity>${escapeXml(name.trim())}</amenity>`);
        }
      } else {
        lines.push("      <amenity></amenity>");
      }
      lines.push("    </amenities>");
      continue;
    }
    const value = withExampleValues ? escapeXml(String(column.example)) : "";
    lines.push(`    <${column.key}>${value}</${column.key}>`);
  }
  if (withExampleValues) {
    lines.push("    <streetAddress>2400 5th Ave</streetAddress>");
    lines.push("    <unitNumber>131</unitNumber>");
    lines.push("    <city>San Diego</city>");
    lines.push("    <state>CA</state>");
    lines.push("    <zipCode>92101</zipCode>");
    lines.push("    <country>US</country>");
    lines.push("    <photos>");
    lines.push("      <photo>https://example.com/photo.jpg</photo>");
    lines.push("    </photos>");
  } else {
    lines.push("    <streetAddress></streetAddress>");
    lines.push("    <unitNumber></unitNumber>");
    lines.push("    <city></city>");
    lines.push("    <state></state>");
    lines.push("    <zipCode></zipCode>");
    lines.push("    <country></country>");
    lines.push("    <photos>");
    lines.push("      <photo></photo>");
    lines.push("    </photos>");
  }
  lines.push("  </listing>");
  return lines.join("\n");
}

export function buildListingXmlTemplate(
  amenities: readonly AmenityDefinitionDto[],
): string {
  const fieldLines = LISTING_IMPORT_COLUMNS.map(
    (column) => `    ${`<${column.key}>`.padEnd(24)} ${column.note}`,
  );
  const sorted = [...amenities].sort((a, b) => a.name.localeCompare(b.name));
  const amenityLines = sorted.map((a) => `    ${a.name} (${a.category})`);

  const instructions = [
    "  How to import listings into Lagedra",
    "",
    "  1. Each <listing> element inside <listings> becomes one draft listing. Duplicate the",
    "     empty <listing> block below for every property you want to import.",
    "  2. Fields marked \"Required\" in the list below must be filled in. Optional fields use",
    "     sensible defaults when left empty or omitted.",
    "  3. Amenities go inside <amenities> as one <amenity> element per amenity. Plain",
    "     comma-separated text (e.g. <amenities>WiFi, Dishwasher</amenities>) also works.",
    "     Valid names are listed at the bottom of this comment.",
    "  4. Times use the 24-hour HH:MM format (e.g. 15:00 for 3 PM).",
    "  5. Optional address fields (streetAddress, city, state, zipCode, country) are applied",
    "     when all of street, city, state and ZIP are filled in. country defaults to US.",
    `  6. Optional photos go inside <photos> as one <photo> URL per image (up to ${MAX_IMPORT_PHOTOS}).`,
    `  7. Up to ${MAX_IMPORT_ROWS} listings can be imported per file.`,
    "  8. When you're done, upload this file back in Lagedra. Each listing becomes a draft —",
    "     nothing is published automatically. Address and photos from the file are applied",
    "     on the draft; you can still review them before submitting for review.",
    "  9. Once a draft looks right, use \"Submit for review\" on the listing to send it to",
    "     the Lagedra team.",
    "",
    "  Fields:",
    ...fieldLines,
    "",
    "  Valid amenity names:",
    ...amenityLines,
  ].join("\n");

  return [
    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
    "<!--",
    commentSafe(instructions),
    "-->",
    "<listings>",
    "  <!-- Example listing, for reference. Listings inside comments are ignored.",
    commentSafe(listingBlock(true)),
    "  -->",
    listingBlock(false),
    "</listings>",
    "",
  ].join("\n");
}

// ── Upload parsing ──────────────────────────────────────────────

/** Accepts nested <amenity> elements or plain comma-separated text. */
function amenitiesText(element: Element): string {
  const parts = Array.from(element.children)
    .map((child) => (child.textContent ?? "").trim())
    .filter((text) => text.length > 0);
  if (parts.length > 0) return parts.join(", ");
  return (element.textContent ?? "").trim();
}

function elementsWithLocalName(doc: Document, localName: string): Element[] {
  return Array.from(doc.getElementsByTagName("*")).filter(
    (element) => element.localName.toLowerCase() === localName,
  );
}

export function parseListingImportXml(
  xml: string,
  amenities: readonly AmenityDefinitionDto[],
): ParsedListingFile {
  const doc = new DOMParser().parseFromString(xml, "text/xml");
  if (doc.querySelector("parsererror")) {
    return {
      rows: [],
      fileErrors: ["This file could not be read as XML. Download the .xml template from Lagedra and start from it."],
    };
  }

  const listingElements = elementsWithLocalName(doc, "listing");
  if (listingElements.length > 0) {
    return parseTemplateListings(listingElements, amenities);
  }

  const propertyElements = elementsWithLocalName(doc, "property");
  if (propertyElements.length > 0) {
    return parseFeedProperties(propertyElements, amenities);
  }

  return {
    rows: [],
    fileErrors: [
      "No <listing> or <property> elements were found. Upload the Lagedra .xml template, or a Zillow/HotPads-style property feed.",
    ],
  };
}

// ── Lagedra template format ─────────────────────────────────────

function parseTemplateListings(
  listingElements: readonly Element[],
  amenities: readonly AmenityDefinitionDto[],
): ParsedListingFile {
  const rows: ParsedListingRow[] = [];
  const fileErrors: string[] = [];

  for (const [index, element] of listingElements.entries()) {
    const raw: RawRecord = {};
    const unrecognized: string[] = [];
    const extraFields = new Map<string, string>();
    for (const child of Array.from(element.children)) {
      const column = COLUMN_BY_NORMALIZED_NAME.get(normalizeImportName(child.localName));
      if (column) {
        raw[column.key] =
          column.key === "amenities" ? amenitiesText(child) : (child.textContent ?? "").trim();
        continue;
      }
      if (isExtraListingElement(child.localName)) {
        extraFields.set(normalizeImportName(child.localName), (child.textContent ?? "").trim());
        continue;
      }
      unrecognized.push(`<${child.localName}>`);
    }

    // An all-empty listing (e.g. the untouched template skeleton) is skipped,
    // just like a blank spreadsheet row.
    const extras = extrasFromListing(element, extraFields);
    if (Object.values(raw).every((value) => isBlank(value))
      && !extras.address
      && extras.photoUrls.length === 0) {
      continue;
    }

    if (rows.length >= MAX_IMPORT_ROWS) {
      fileErrors.push(
        `Only the first ${MAX_IMPORT_ROWS} listings were read. Split larger imports into multiple files.`,
      );
      break;
    }

    const mapped = mapRowToListing(raw, amenities);
    if (unrecognized.length > 0) {
      mapped.warnings.push(`Elements not recognized and ignored: ${unrecognized.join(", ")}.`);
    }
    if (extras.addressWarning) {
      mapped.warnings.push(extras.addressWarning);
    }
    rows.push({
      rowNumber: index + 1,
      ...mapped,
      address: extras.address,
      photoUrls: extras.photoUrls,
    });
  }

  if (rows.length === 0 && fileErrors.length === 0) {
    fileErrors.push("No listings were found in the file. Fill in a <listing> element and upload it again.");
  }

  return { rows, fileErrors };
}

// ── Zillow/HotPads-style property feeds ─────────────────────────

/**
 * Boolean characteristic flags mapped to amenity hints. A hint is only added
 * when it resolves against the platform's amenity list, so flags we can't
 * represent don't produce "not recognized" warnings.
 */
const FEED_AMENITY_FLAGS: Record<string, string> = {
  "has-washer": "washer",
  "has-dishwasher": "dishwasher",
  "has-microwave": "microwave",
  "has-fireplace": "fireplace",
  "has-cable-satellite": "cable tv",
  "has-deck": "deck",
  "has-garden": "garden",
  "has-jetted-bath-tub": "hot tub",
  "has-patio": "patio",
  "has-pool": "pool",
  "has-sauna": "sauna",
  "building-has-fitness-center": "gym",
};

/**
 * One pass over a property's descendants, indexed by lower-cased local name
 * (first occurrence wins). Repeated getElementsByTagName scans are painfully
 * slow on large feeds, so lookups go through this index.
 */
function descendantTextIndex(root: Element): ReadonlyMap<string, string> {
  const index = new Map<string, string>();
  for (const element of Array.from(root.getElementsByTagName("*"))) {
    const name = element.localName.toLowerCase();
    if (!index.has(name)) {
      index.set(name, (element.textContent ?? "").trim());
    }
  }
  return index;
}

/**
 * Feed types are often composites like "apartment/condo/townhouse"; resolve
 * to the first segment we recognize. Unknown values pass through so the
 * shared validation reports them honestly.
 */
function feedPropertyType(raw: string): string {
  if (!raw || matchPropertyType(raw)) return raw;
  for (const part of raw.split("/")) {
    const matched = matchPropertyType(part);
    if (matched) return matched;
  }
  return raw;
}

/**
 * Minimum stay comes from <MinimumStay> (a number) or free-text <Terms> like
 * "30 days min". Values outside the platform's 30–180 day range are dropped
 * so the row falls back to the default instead of failing validation.
 */
function feedMinStayDays(minimumStay: string, terms: string): number | undefined {
  if (minimumStay) {
    const direct = Number(minimumStay);
    if (Number.isFinite(direct) && direct >= 30 && direct <= 180) {
      return Math.round(direct);
    }
  }
  const fromTerms = /(\d+)\s*day/i.exec(terms);
  if (fromTerms) {
    const parsed = Number(fromTerms[1]);
    if (parsed >= 30 && parsed <= 180) return parsed;
  }
  return undefined;
}

function feedAmenities(
  fields: ReadonlyMap<string, string>,
  amenities: readonly AmenityDefinitionDto[],
): string {
  const hints = (fields.get("amenities") ?? "")
    .split(",")
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0);

  const known = new Set(amenities.map((a) => normalizeAmenityName(a.name)));
  const resolves = (hint: string): boolean => {
    const normalized = normalizeAmenityName(hint);
    return known.has(normalized) || known.has(AMENITY_SYNONYMS[normalized] ?? "");
  };

  for (const [flag, hint] of Object.entries(FEED_AMENITY_FLAGS)) {
    if (fields.get(flag)?.toLowerCase() === "yes" && resolves(hint)) {
      hints.push(hint);
    }
  }

  return hints.join(", ");
}

function parseFeedProperties(
  propertyElements: readonly Element[],
  amenities: readonly AmenityDefinitionDto[],
): ParsedListingFile {
  const rows: ParsedListingRow[] = [];
  const fileErrors: string[] = [];

  for (const [index, property] of propertyElements.entries()) {
    const raw: RawRecord = {};
    const feedErrors: string[] = [];
    const fields = descendantTextIndex(property);
    const field = (name: string): string => fields.get(name) ?? "";

    raw.title = field("listing-title");
    raw.description = field("description");
    raw.propertyType = feedPropertyType(field("property-type"));
    raw.maxDeposit = field("security-deposit");
    raw.bedrooms = field("num-bedrooms");

    const bathrooms = field("num-bathrooms");
    if (bathrooms) {
      raw.bathrooms = bathrooms;
    } else {
      const full = field("num-full-bathrooms");
      const half = field("num-half-bathrooms");
      const fullCount = full ? Number(full) : 0;
      const halfCount = half ? Number(half) : 0;
      if ((full || half) && Number.isFinite(fullCount) && Number.isFinite(halfCount)) {
        raw.bathrooms = fullCount + halfCount * 0.5;
      }
    }

    // Feed prices carry a unit; anything other than monthly would import a
    // wrong rent, so those rows are rejected rather than converted.
    const priceTerm = field("price-term").toLowerCase();
    if (priceTerm && priceTerm !== "month" && priceTerm !== "monthly") {
      feedErrors.push(
        `Price term "${priceTerm}" is not supported — only monthly prices can be imported`,
      );
    } else {
      raw.monthlyRent = field("price");
    }

    const minStay = feedMinStayDays(field("minimumstay"), field("terms"));
    if (minStay !== undefined) raw.minStayDays = minStay;

    const amenitiesValue = feedAmenities(fields, amenities);
    if (amenitiesValue) raw.amenities = amenitiesValue;

    const extras = extrasFromFeed(property, fields);

    if (Object.values(raw).every((value) => isBlank(value))
      && !extras.address
      && extras.photoUrls.length === 0) {
      continue;
    }

    if (rows.length >= MAX_IMPORT_ROWS) {
      fileErrors.push(
        `Only the first ${MAX_IMPORT_ROWS} listings were read. Split larger imports into multiple files.`,
      );
      break;
    }

    const mapped = mapRowToListing(raw, amenities);
    if (extras.addressWarning) {
      mapped.warnings.push(extras.addressWarning);
    }
    if (feedErrors.length > 0) {
      rows.push({
        rowNumber: index + 1,
        ...mapped,
        values: null,
        errors: [...feedErrors, ...mapped.errors],
        address: extras.address,
        photoUrls: extras.photoUrls,
      });
    } else {
      rows.push({
        rowNumber: index + 1,
        ...mapped,
        address: extras.address,
        photoUrls: extras.photoUrls,
      });
    }
  }

  if (rows.length === 0 && fileErrors.length === 0) {
    fileErrors.push("No listings were found in the file.");
  }

  return { rows, fileErrors };
}

// ── Address & photos ────────────────────────────────────────────

const EXTRA_LISTING_ELEMENTS = new Set([
  "streetaddress",
  "street",
  "unitnumber",
  "unit",
  "city",
  "state",
  "zipcode",
  "postalcode",
  "country",
  "photos",
  "photo",
  "pictures",
  "picture",
]);

function isExtraListingElement(localName: string): boolean {
  return EXTRA_LISTING_ELEMENTS.has(normalizeImportName(localName));
}

type ListingExtras = {
  address: ParsedListingAddress | null;
  photoUrls: string[];
  addressWarning?: string;
};

function extrasFromListing(
  listing: Element,
  extraFields: ReadonlyMap<string, string>,
): ListingExtras {
  return buildExtras(
    {
      street: extraFields.get("streetaddress") || extraFields.get("street") || "",
      unit: extraFields.get("unitnumber") || extraFields.get("unit") || "",
      city: extraFields.get("city") || "",
      state: extraFields.get("state") || "",
      zip: extraFields.get("zipcode") || extraFields.get("postalcode") || "",
      country: extraFields.get("country") || "",
    },
    collectPhotoUrls(listing),
  );
}

function extrasFromFeed(
  property: Element,
  fields: ReadonlyMap<string, string>,
): ListingExtras {
  return buildExtras(
    {
      street: fields.get("street-address") || "",
      unit: fields.get("unit-number") || "",
      city: fields.get("city-name") || "",
      state: fields.get("state-code") || "",
      zip: fields.get("zipcode") || "",
      country: fields.get("country") || "",
    },
    collectPhotoUrls(property),
  );
}

function buildExtras(
  parts: {
    street: string;
    unit: string;
    city: string;
    state: string;
    zip: string;
    country: string;
  },
  photoUrls: string[],
): ListingExtras {
  const street = composeStreet(parts.street, parts.unit);
  const city = parts.city.trim();
  const state = normalizeState(parts.state);
  const zip = parts.zip.trim();
  const country = parts.country.trim() || "US";
  const anyLocation = Boolean(street || parts.unit.trim() || city || state || zip || parts.country.trim());

  if (street && city && state && zip) {
    return {
      address: { street, city, state, zipCode: zip, country },
      photoUrls,
    };
  }

  return {
    address: null,
    photoUrls,
    addressWarning: anyLocation
      ? "Address skipped — street, city, state and ZIP are all required to set a location."
      : undefined,
  };
}

/** Strips feed prefixes like "FURNISHED RENTAL" and appends a unit when present. */
function composeStreet(rawStreet: string, rawUnit: string): string {
  let street = rawStreet.replace(/^(un)?furnished\s+rental\s+/i, "").replace(/\s+/g, " ").trim();
  const unit = rawUnit.trim();
  if (street && unit && !street.toLowerCase().includes(unit.toLowerCase())) {
    street = `${street}, Unit ${unit}`;
  }
  return street;
}

function normalizeState(raw: string): string {
  const trimmed = raw.trim();
  return trimmed.length === 2 ? trimmed.toUpperCase() : trimmed;
}

function collectPhotoUrls(root: Element): string[] {
  const seen = new Set<string>();
  const urls: string[] = [];
  for (const element of Array.from(root.getElementsByTagName("*"))) {
    const name = element.localName.toLowerCase();
    if (name !== "picture-url" && name !== "photo") continue;
    if (element.children.length > 0) continue;
    const text = (element.textContent ?? "").trim();
    if (!/^https?:\/\//i.test(text)) continue;
    const key = text.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    urls.push(text);
    if (urls.length >= MAX_IMPORT_PHOTOS) break;
  }
  return urls;
}
