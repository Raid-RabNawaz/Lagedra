import type {
  AmenityDefinitionDto,
  CancellationPolicyType,
  ImportedListingDraftDto,
  PropertyType,
} from "@/api/types";
import type { ListingFormValues } from "./listingFormSchema";

const PROPERTY_TYPES: readonly PropertyType[] = [
  "Apartment",
  "House",
  "Condo",
  "Townhouse",
  "Studio",
  "Loft",
  "Villa",
  "Cottage",
  "Cabin",
  "Other",
];

// Common schema.org / Open Graph type strings mapped to our enum. Anything not
// listed here (and not a direct enum match) is dropped so the form keeps its
// default property type.
const PROPERTY_TYPE_SYNONYMS: Record<string, PropertyType> = {
  apartment: "Apartment",
  flat: "Apartment",
  house: "House",
  singlefamilyresidence: "House",
  home: "House",
  condo: "Condo",
  condominium: "Condo",
  townhouse: "Townhouse",
  townhome: "Townhouse",
  studio: "Studio",
  loft: "Loft",
  villa: "Villa",
  cottage: "Cottage",
  cabin: "Cabin",
};

export type ImportMappingResult = {
  /** Partial form values to merge over the wizard defaults. */
  values: Partial<ListingFormValues>;
  /** True when monthly rent was derived from a nightly rate (×30). */
  monthlyDerivedFromNightly: boolean;
  /** Number of amenity hints that matched a known amenity definition. */
  amenitiesMatched: number;
  /** Total number of amenity hints offered by the import. */
  amenitiesTotal: number;
  /** Human-readable labels of the fields that were pre-filled. */
  importedFields: string[];
};

export function matchPropertyType(raw: string | null | undefined): PropertyType | undefined {
  if (!raw) return undefined;
  const normalized = raw.trim().toLowerCase();
  const direct = PROPERTY_TYPES.find((t) => t.toLowerCase() === normalized);
  if (direct) return direct;
  return PROPERTY_TYPE_SYNONYMS[normalized];
}

// Maps a third-party cancellation-policy label onto our closest enum. Airbnb's
// "Firm"/"Strict"/"Super Strict" all collapse to our strictest non-custom tier;
// unknown labels are dropped so the form keeps its default policy.
export function matchCancellationType(
  raw: string | null | undefined,
): CancellationPolicyType | undefined {
  if (!raw) return undefined;
  const n = raw.trim().toLowerCase();
  if (n.includes("flex")) return "Flexible";
  if (n.includes("moderate")) return "Moderate";
  if (n.includes("non-refundable") || n.includes("nonrefundable") || n.includes("no refund")) {
    return "NonRefundable";
  }
  if (n.includes("firm") || n.includes("strict") || n.includes("super")) return "Strict";
  return undefined;
}

// Normalizes amenity names so values that differ only cosmetically still match:
// case, punctuation, and "&" vs "and" are all folded away
// (e.g. "Wi-Fi", "WiFi", "wifi" → "wifi"; "Dishes & Silverware" → "dishes and silverware").
export function normalizeAmenityName(value: string): string {
  return value
    .toLowerCase()
    .replace(/&/g, " and ")
    .replace(/[^a-z0-9]+/g, " ")
    .trim()
    .replace(/\s+/g, " ");
}

// Maps common third-party amenity wording (normalized) onto our canonical
// vocabulary (normalized). Imported platforms — Airbnb in particular — name the
// same amenity differently ("Shower gel" vs "Body Wash", "Ceiling fan" vs
// "Ceiling Fans"), so an exact match alone misses most of them. Also used by
// the bulk file importers (listingImportShared.ts).
export const AMENITY_SYNONYMS: Record<string, string> = {
  "shower gel": "body wash",
  "bluetooth sound system": "sound system",
  "portable speaker": "sound system",
  "hot water kettle": "kettle",
  "electric kettle": "kettle",
  hdtv: "tv",
  "hdtv with roku": "tv",
  "hdtv with standard cable": "cable tv",
  "tv with standard cable": "cable tv",
  "indoor fireplace": "fireplace",
  "air conditioning": "central air conditioning",
  heating: "central heating",
  "ceiling fan": "ceiling fans",
  "portable fans": "portable fan",
  "private patio or balcony": "patio",
  "patio or balcony": "patio",
  "shared patio or balcony": "patio",
  backyard: "private backyard",
  washer: "in unit washer",
  "free washer": "in unit washer",
  "free washer in unit": "in unit washer",
  dryer: "in unit dryer",
  "free dryer": "in unit dryer",
  "free dryer in unit": "in unit dryer",
  "free street parking": "street parking",
  "private pool": "pool",
  "shared pool": "pool",
  "private hot tub": "hot tub",
  "shared hot tub": "hot tub",
  "barbecue grill": "bbq grill",
  "bbq grill": "bbq grill",
  gym: "gym fitness equipment",
  "exercise equipment": "gym fitness equipment",
};

function matchAmenityIds(
  hints: readonly string[] | null | undefined,
  amenities: readonly AmenityDefinitionDto[],
): { ids: string[]; matched: number; total: number } {
  if (!hints || hints.length === 0) {
    return { ids: [], matched: 0, total: 0 };
  }

  const byName = new Map<string, string>();
  for (const amenity of amenities) {
    byName.set(normalizeAmenityName(amenity.name), amenity.id);
  }

  const ids = new Set<string>();
  for (const hint of hints) {
    const normalized = normalizeAmenityName(hint);
    const id = byName.get(normalized) ?? byName.get(AMENITY_SYNONYMS[normalized] ?? "");
    if (id) {
      ids.add(id);
    }
  }

  return { ids: [...ids], matched: ids.size, total: hints.length };
}

/**
 * Translates an imported draft into a partial set of listing form values.
 * Every rule is conservative: unknown enums, unmatched amenities, and missing
 * fields are simply omitted so the wizard falls back to its normal defaults.
 */
export function mapImportedDraftToForm(
  draft: ImportedListingDraftDto,
  amenities: readonly AmenityDefinitionDto[] = [],
): ImportMappingResult {
  const values: Partial<ListingFormValues> = {};
  const importedFields: string[] = [];

  if (draft.title && draft.title.trim().length > 0) {
    values.title = draft.title.trim();
    importedFields.push("Title");
  }

  if (draft.description && draft.description.trim().length > 0) {
    values.description = draft.description.trim();
    importedFields.push("Description");
  }

  const propertyType = matchPropertyType(draft.propertyType);
  if (propertyType) {
    values.propertyType = propertyType;
    importedFields.push("Property type");
  }

  if (typeof draft.bedrooms === "number" && Number.isFinite(draft.bedrooms)) {
    values.bedrooms = Math.max(0, Math.round(draft.bedrooms));
    importedFields.push("Bedrooms");
  }

  if (typeof draft.bathrooms === "number" && Number.isFinite(draft.bathrooms)) {
    values.bathrooms = draft.bathrooms;
    importedFields.push("Bathrooms");
  }

  if (typeof draft.squareFootage === "number" && Number.isFinite(draft.squareFootage)) {
    values.squareFootage = Math.max(0, Math.round(draft.squareFootage));
    importedFields.push("Square footage");
  }

  if (typeof draft.maxGuests === "number" && Number.isFinite(draft.maxGuests) && draft.maxGuests > 0) {
    values.maxGuests = Math.round(draft.maxGuests);
    importedFields.push("Max guests");
  }

  if (draft.checkInTime) {
    values.checkInTime = draft.checkInTime;
    importedFields.push("Check-in time");
  }

  if (draft.checkOutTime) {
    values.checkOutTime = draft.checkOutTime;
    importedFields.push("Check-out time");
  }

  // Pricing: prefer an explicit monthly rate; otherwise derive from nightly ×30.
  let monthlyDerivedFromNightly = false;
  if (typeof draft.monthlyRentCents === "number" && draft.monthlyRentCents > 0) {
    values.monthlyRentDollars = draft.monthlyRentCents / 100;
    importedFields.push("Monthly rent");
  } else if (typeof draft.nightlyRateCents === "number" && draft.nightlyRateCents > 0) {
    values.monthlyRentDollars = (draft.nightlyRateCents * 30) / 100;
    monthlyDerivedFromNightly = true;
    importedFields.push("Monthly rent (estimated from nightly rate)");
  }

  const amenityMatch = matchAmenityIds(draft.amenityHints, amenities);
  if (amenityMatch.ids.length > 0) {
    values.amenityIds = amenityMatch.ids;
    importedFields.push("Amenities");
  }

  // House rules. Booleans are applied even when false ("No pets" → false) so the
  // imported policy is reflected, not just the permissive cases.
  if (typeof draft.petsAllowed === "boolean") {
    values.petsAllowed = draft.petsAllowed;
    importedFields.push("Pet policy");
  }

  if (typeof draft.smokingAllowed === "boolean") {
    values.smokingAllowed = draft.smokingAllowed;
    importedFields.push("Smoking policy");
  }

  if (typeof draft.partiesAllowed === "boolean") {
    values.partiesAllowed = draft.partiesAllowed;
    importedFields.push("Party policy");
  }

  if (draft.quietHoursStart && draft.quietHoursEnd) {
    values.quietHoursStart = draft.quietHoursStart;
    values.quietHoursEnd = draft.quietHoursEnd;
    importedFields.push("Quiet hours");
  }

  if (draft.houseRules && draft.houseRules.trim().length > 0) {
    values.additionalRules = draft.houseRules.trim();
    importedFields.push("House rules");
  }

  const cancellationType = matchCancellationType(draft.cancellationPolicy);
  if (cancellationType) {
    values.cancellationType = cancellationType;
    importedFields.push("Cancellation policy");
  }

  return {
    values,
    monthlyDerivedFromNightly,
    amenitiesMatched: amenityMatch.matched,
    amenitiesTotal: amenityMatch.total,
    importedFields,
  };
}
