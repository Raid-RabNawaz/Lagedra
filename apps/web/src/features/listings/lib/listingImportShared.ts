import type * as ExcelJSTypes from "exceljs";
import type { AmenityDefinitionDto } from "@/api/types";
import {
  listingFormSchema,
  defaultListingFormValues,
  propertyTypes,
  cancellationTypes,
  type ListingFormValues,
} from "./listingFormSchema";
import {
  matchPropertyType,
  matchCancellationType,
  normalizeAmenityName,
  AMENITY_SYNONYMS,
} from "./mapImportedDraftToForm";

/**
 * Format-neutral core shared by the bulk listing importers (Excel and XML):
 * the field catalog, value coercion and the raw-record -> ListingFormValues
 * mapping. Template generation and file reading live in listingExcelImport.ts
 * and listingXmlImport.ts; both feed their rows through mapRowToListing so
 * validation behaves identically regardless of the uploaded format.
 */

/** Hard cap so a stray export with thousands of rows can't hammer the API. */
export const MAX_IMPORT_ROWS = 100;

/** Soft cap kept in sync with the URL importer and server-side photo fetch. */
export const MAX_IMPORT_PHOTOS = 20;

export const YES_NO = ["Yes", "No"] as const;

export type ColumnKey =
  | "title"
  | "description"
  | "propertyType"
  | "monthlyRent"
  | "maxDeposit"
  | "bedrooms"
  | "bathrooms"
  | "squareFootage"
  | "minStayDays"
  | "maxStayDays"
  | "maxGuests"
  | "checkInTime"
  | "checkOutTime"
  | "petsAllowed"
  | "smokingAllowed"
  | "partiesAllowed"
  | "quietHoursStart"
  | "quietHoursEnd"
  | "additionalRules"
  | "cancellationPolicy"
  | "freeCancellationDays"
  | "instantBooking"
  | "amenities"
  | "virtualTourUrl";

export type ListingImportColumn = {
  key: ColumnKey;
  /** Human-readable label used in error messages, e.g. "Monthly rent (USD)". */
  label: string;
  required: boolean;
  /** Column width in the Excel template; unused by the XML template. */
  width: number;
  example: string | number;
  note: string;
  /** Valid values; the Excel template renders these as in-cell dropdowns. */
  options?: readonly string[];
};

export const LISTING_IMPORT_COLUMNS: readonly ListingImportColumn[] = [
  {
    key: "title",
    label: "Title",
    required: true,
    width: 36,
    example: "Sunny 2-bedroom near the riverfront park",
    note: "Required. At least 5 characters.",
  },
  {
    key: "description",
    label: "Description",
    required: true,
    width: 60,
    example:
      "Bright, fully furnished two-bedroom apartment with a dedicated workspace, "
      + "fast Wi-Fi and a balcony overlooking the park. Ideal for stays of one to six months.",
    note: "Required. At least 50 characters.",
  },
  {
    key: "propertyType",
    label: "Property type",
    required: true,
    width: 16,
    example: "Apartment",
    note: `Required. One of: ${propertyTypes.join(", ")}.`,
    options: propertyTypes,
  },
  {
    key: "monthlyRent",
    label: "Monthly rent (USD)",
    required: true,
    width: 18,
    example: 2400,
    note: "Required. Number greater than 0, in dollars.",
  },
  {
    key: "maxDeposit",
    label: "Max deposit (USD)",
    required: true,
    width: 18,
    example: 2400,
    note: "Required. Maximum security deposit in dollars. 0 is allowed.",
  },
  {
    key: "bedrooms",
    label: "Bedrooms",
    required: true,
    width: 11,
    example: 2,
    note: "Required. Whole number, 0 for a studio.",
  },
  {
    key: "bathrooms",
    label: "Bathrooms",
    required: true,
    width: 11,
    example: 1.5,
    note: "Required. Minimum 0.5. Half baths allowed (e.g. 1.5).",
  },
  {
    key: "squareFootage",
    label: "Square footage",
    required: false,
    width: 14,
    example: 950,
    note: "Optional. Whole number.",
  },
  {
    key: "minStayDays",
    label: "Min stay (days)",
    required: false,
    width: 14,
    example: 30,
    note: "Optional. 30–180. Defaults to 30.",
  },
  {
    key: "maxStayDays",
    label: "Max stay (days)",
    required: false,
    width: 14,
    example: 180,
    note: "Optional. 30–180 and at least the min stay. Defaults to 180.",
  },
  {
    key: "maxGuests",
    label: "Max guests",
    required: false,
    width: 11,
    example: 4,
    note: "Optional. Defaults to 2.",
  },
  {
    key: "checkInTime",
    label: "Check-in time",
    required: false,
    width: 13,
    example: "15:00",
    note: "Optional. 24-hour HH:MM. Defaults to 15:00.",
  },
  {
    key: "checkOutTime",
    label: "Check-out time",
    required: false,
    width: 14,
    example: "11:00",
    note: "Optional. 24-hour HH:MM. Defaults to 11:00.",
  },
  {
    key: "petsAllowed",
    label: "Pets allowed",
    required: false,
    width: 12,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "smokingAllowed",
    label: "Smoking allowed",
    required: false,
    width: 15,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "partiesAllowed",
    label: "Parties allowed",
    required: false,
    width: 14,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "quietHoursStart",
    label: "Quiet hours start",
    required: false,
    width: 15,
    example: "22:00",
    note: "Optional. 24-hour HH:MM.",
  },
  {
    key: "quietHoursEnd",
    label: "Quiet hours end",
    required: false,
    width: 15,
    example: "07:00",
    note: "Optional. 24-hour HH:MM.",
  },
  {
    key: "additionalRules",
    label: "Additional rules",
    required: false,
    width: 32,
    example: "No shoes indoors. Recycling is mandatory.",
    note: "Optional. Free text shown with the house rules.",
  },
  {
    key: "cancellationPolicy",
    label: "Cancellation policy",
    required: false,
    width: 18,
    example: "Moderate",
    note: `Optional. One of: ${cancellationTypes.join(", ")}. Defaults to Moderate.`,
    options: cancellationTypes,
  },
  {
    key: "freeCancellationDays",
    label: "Free cancellation days",
    required: false,
    width: 20,
    example: 14,
    note: "Optional. Whole number of days. Defaults to 14.",
  },
  {
    key: "instantBooking",
    label: "Instant booking",
    required: false,
    width: 14,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "amenities",
    label: "Amenities",
    required: false,
    width: 40,
    example: "WiFi, Dishwasher, In-Unit Washer",
    note: "Optional. Comma-separated names from the template's amenity list.",
  },
  {
    key: "virtualTourUrl",
    label: "Virtual tour URL",
    required: false,
    width: 28,
    example: "https://example.com/tour",
    note: "Optional. Link to a 3D/virtual tour.",
  },
];

/**
 * Forgiving name matching for column headers (Excel) and element names (XML):
 * case, punctuation, the required-marker "*" and parenthetical hints like
 * "(USD)" are all ignored, so lightly edited files still parse.
 */
export function normalizeImportName(value: string): string {
  return value
    .toLowerCase()
    .replace(/\([^)]*\)/g, " ")
    .replace(/[^a-z0-9]+/g, "");
}

// ── Value coercion ──────────────────────────────────────────────

/**
 * Raw values are Excel cell values; XML supplies plain strings, which are a
 * subset. The type-only import keeps exceljs out of the bundle.
 */
export type ImportCellValue = ExcelJSTypes.CellValue;

export function cellText(value: ImportCellValue): string {
  if (value === null || value === undefined) return "";
  if (typeof value === "string") return value.trim();
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  if (value instanceof Date) return value.toISOString();
  if (typeof value === "object") {
    if ("richText" in value) {
      return value.richText.map((part) => part.text).join("").trim();
    }
    if ("text" in value && typeof value.text === "string") return value.text.trim();
    if ("result" in value) return cellText(value.result as ImportCellValue);
  }
  return "";
}

function cellNumber(value: ImportCellValue): number | undefined {
  if (typeof value === "number") return value;
  const text = cellText(value).replace(/[$,\s]/g, "");
  if (!text) return undefined;
  const parsed = Number(text);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function cellBoolean(value: ImportCellValue): boolean | undefined {
  if (typeof value === "boolean") return value;
  const text = cellText(value).toLowerCase();
  if (!text) return undefined;
  if (["yes", "y", "true", "1"].includes(text)) return true;
  if (["no", "n", "false", "0"].includes(text)) return false;
  return undefined;
}

/** Accepts "15:00", "3:00 PM", Excel time serials and date-typed cells. */
function cellTime(value: ImportCellValue): string | undefined {
  if (value instanceof Date) {
    const h = value.getUTCHours();
    const m = value.getUTCMinutes();
    return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
  }
  if (typeof value === "number" && value >= 0 && value < 1) {
    const totalMinutes = Math.round(value * 24 * 60);
    const h = Math.floor(totalMinutes / 60) % 24;
    const m = totalMinutes % 60;
    return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
  }
  const text = cellText(value);
  if (!text) return undefined;
  const match = /^(\d{1,2}):(\d{2})(?::\d{2})?\s*(am|pm)?$/i.exec(text.trim());
  if (!match) return undefined;
  let h = Number(match[1]);
  const m = Number(match[2]);
  const meridiem = match[3]?.toLowerCase();
  if (h > 23 || m > 59) return undefined;
  if (meridiem === "pm" && h < 12) h += 12;
  if (meridiem === "am" && h === 12) h = 0;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

export function isBlank(value: ImportCellValue): boolean {
  return cellText(value).length === 0;
}

// ── Row mapping ─────────────────────────────────────────────────

export type ParsedListingAddress = {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
};

export type ParsedListingRow = {
  /**
   * 1-based position in the uploaded file: the spreadsheet row number for
   * Excel, the <listing> ordinal for XML.
   */
  rowNumber: number;
  /** Best-effort title for display, even when the row is invalid. */
  title: string;
  /** Complete form values when the row is valid, otherwise null. */
  values: ListingFormValues | null;
  errors: string[];
  /** Non-blocking issues, e.g. amenity names that couldn't be matched. */
  warnings: string[];
  /** Structured address when the file included one (XML feeds / template). */
  address?: ParsedListingAddress | null;
  /** Public photo URLs to fetch server-side after the draft is created. */
  photoUrls?: string[];
};

export type ParsedListingFile = {
  rows: ParsedListingRow[];
  /** File-level problems (wrong structure, missing columns, too many rows). */
  fileErrors: string[];
};

/** Friendly field labels for Zod validation messages, keyed by form field. */
const FIELD_LABELS: Partial<Record<keyof ListingFormValues, string>> = {
  title: "Title",
  description: "Description",
  propertyType: "Property type",
  monthlyRentDollars: "Monthly rent",
  maxDepositDollars: "Max deposit",
  bedrooms: "Bedrooms",
  bathrooms: "Bathrooms",
  squareFootage: "Square footage",
  minStayDays: "Min stay",
  maxStayDays: "Max stay",
  maxGuests: "Max guests",
  checkInTime: "Check-in time",
  checkOutTime: "Check-out time",
  freeCancellationDays: "Free cancellation days",
};

export type RawRecord = Partial<Record<ColumnKey, ImportCellValue>>;

export function mapRowToListing(
  raw: RawRecord,
  amenities: readonly AmenityDefinitionDto[],
): Omit<ParsedListingRow, "rowNumber"> {
  const errors: string[] = [];
  const warnings: string[] = [];
  const values: Partial<ListingFormValues> = {};
  const title = cellText(raw.title) || "(untitled)";

  for (const column of LISTING_IMPORT_COLUMNS) {
    if (column.required && isBlank(raw[column.key])) {
      errors.push(`${column.label} is required`);
    }
  }

  const setNumber = (
    key: ColumnKey,
    apply: (n: number) => void,
    { integer = false }: { integer?: boolean } = {},
  ) => {
    const cell = raw[key];
    if (cell === undefined || isBlank(cell)) return;
    const parsed = cellNumber(cell);
    const column = LISTING_IMPORT_COLUMNS.find((c) => c.key === key)!;
    if (parsed === undefined) {
      errors.push(`${column.label} must be a number`);
      return;
    }
    apply(integer ? Math.round(parsed) : parsed);
  };

  const setBoolean = (key: ColumnKey, apply: (b: boolean) => void) => {
    const cell = raw[key];
    if (cell === undefined || isBlank(cell)) return;
    const parsed = cellBoolean(cell);
    const column = LISTING_IMPORT_COLUMNS.find((c) => c.key === key)!;
    if (parsed === undefined) {
      errors.push(`${column.label} must be Yes or No`);
      return;
    }
    apply(parsed);
  };

  const setTime = (key: ColumnKey, apply: (t: string) => void) => {
    const cell = raw[key];
    if (cell === undefined || isBlank(cell)) return;
    const parsed = cellTime(cell);
    const column = LISTING_IMPORT_COLUMNS.find((c) => c.key === key)!;
    if (parsed === undefined) {
      errors.push(`${column.label} must be a time like 15:00`);
      return;
    }
    apply(parsed);
  };

  if (!isBlank(raw.title)) values.title = cellText(raw.title);
  if (!isBlank(raw.description)) values.description = cellText(raw.description);

  if (!isBlank(raw.propertyType)) {
    const matched = matchPropertyType(cellText(raw.propertyType));
    if (matched) {
      values.propertyType = matched;
    } else {
      errors.push(`Property type must be one of: ${propertyTypes.join(", ")}`);
    }
  }

  setNumber("monthlyRent", (n) => (values.monthlyRentDollars = n));
  setNumber("maxDeposit", (n) => (values.maxDepositDollars = n));
  setNumber("bedrooms", (n) => (values.bedrooms = n), { integer: true });
  setNumber("bathrooms", (n) => (values.bathrooms = n));
  setNumber("squareFootage", (n) => (values.squareFootage = n), { integer: true });
  setNumber("minStayDays", (n) => (values.minStayDays = n), { integer: true });
  setNumber("maxStayDays", (n) => (values.maxStayDays = n), { integer: true });
  setNumber("maxGuests", (n) => (values.maxGuests = n), { integer: true });
  setNumber("freeCancellationDays", (n) => (values.freeCancellationDays = n), {
    integer: true,
  });

  setTime("checkInTime", (t) => (values.checkInTime = t));
  setTime("checkOutTime", (t) => (values.checkOutTime = t));
  setTime("quietHoursStart", (t) => (values.quietHoursStart = t));
  setTime("quietHoursEnd", (t) => (values.quietHoursEnd = t));

  setBoolean("petsAllowed", (b) => (values.petsAllowed = b));
  setBoolean("smokingAllowed", (b) => (values.smokingAllowed = b));
  setBoolean("partiesAllowed", (b) => (values.partiesAllowed = b));
  setBoolean("instantBooking", (b) => (values.instantBookingEnabled = b));

  if (!isBlank(raw.additionalRules)) values.additionalRules = cellText(raw.additionalRules);
  if (!isBlank(raw.virtualTourUrl)) values.virtualTourUrl = cellText(raw.virtualTourUrl);

  if (!isBlank(raw.cancellationPolicy)) {
    const matched = matchCancellationType(cellText(raw.cancellationPolicy));
    if (matched) {
      values.cancellationType = matched;
    } else {
      errors.push(`Cancellation policy must be one of: ${cancellationTypes.join(", ")}`);
    }
  }

  if (!isBlank(raw.amenities)) {
    const byName = new Map(
      amenities.map((a) => [normalizeAmenityName(a.name), a.id]),
    );
    const matchedIds = new Set<string>();
    const unmatched: string[] = [];
    for (const name of cellText(raw.amenities).split(",")) {
      const trimmed = name.trim();
      if (!trimmed) continue;
      const normalized = normalizeAmenityName(trimmed);
      const id = byName.get(normalized) ?? byName.get(AMENITY_SYNONYMS[normalized] ?? "");
      if (id) {
        matchedIds.add(id);
      } else {
        unmatched.push(trimmed);
      }
    }
    values.amenityIds = [...matchedIds];
    if (unmatched.length > 0) {
      warnings.push(
        `Amenities not recognized and skipped: ${unmatched.join(", ")}. See the amenity list in the template.`,
      );
    }
  }

  if (errors.length > 0) {
    return { title, values: null, errors, warnings };
  }

  const merged: ListingFormValues = { ...defaultListingFormValues, ...values };
  const parsed = listingFormSchema.safeParse(merged);
  if (!parsed.success) {
    const seen = new Set<string>();
    for (const issue of parsed.error.issues) {
      const key = issue.path[0];
      const label =
        typeof key === "string"
          ? (FIELD_LABELS[key as keyof ListingFormValues] ?? key)
          : "Value";
      const message = `${label}: ${issue.message}`;
      if (!seen.has(message)) {
        seen.add(message);
        errors.push(message);
      }
    }
    return { title, values: null, errors, warnings };
  }

  return { title, values: parsed.data, errors, warnings };
}
