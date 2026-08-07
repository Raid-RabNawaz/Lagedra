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
} from "./mapImportedDraftToForm";

/**
 * Bulk "Import from Excel" support. One module owns both the downloadable
 * template and the parser so the two can never drift apart: the template is
 * generated from LISTING_EXCEL_COLUMNS and uploads are matched back to the
 * same columns by (normalized) header text.
 *
 * exceljs is loaded lazily — it is a large dependency and only needed once
 * the host actually opens the import dialog.
 */

type ExcelJSModule = typeof ExcelJSTypes;

async function loadExcelJS(): Promise<ExcelJSModule> {
  const mod: unknown = await import("exceljs");
  const withDefault = mod as { default?: ExcelJSModule };
  return withDefault.default ?? (mod as ExcelJSModule);
}

const LISTINGS_SHEET = "Listings";
const YES_NO = ["Yes", "No"] as const;

/** Hard cap so a stray export with thousands of rows can't hammer the API. */
export const MAX_IMPORT_ROWS = 100;

type ColumnKey =
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

export type ListingExcelColumn = {
  key: ColumnKey;
  header: string;
  required: boolean;
  width: number;
  example: string | number;
  note: string;
  /** When set, the template gets an in-cell dropdown with these options. */
  options?: readonly string[];
};

export const LISTING_EXCEL_COLUMNS: readonly ListingExcelColumn[] = [
  {
    key: "title",
    header: "Title *",
    required: true,
    width: 36,
    example: "Sunny 2-bedroom near the riverfront park",
    note: "Required. At least 5 characters.",
  },
  {
    key: "description",
    header: "Description *",
    required: true,
    width: 60,
    example:
      "Bright, fully furnished two-bedroom apartment with a dedicated workspace, "
      + "fast Wi-Fi and a balcony overlooking the park. Ideal for stays of one to six months.",
    note: "Required. At least 50 characters.",
  },
  {
    key: "propertyType",
    header: "Property type *",
    required: true,
    width: 16,
    example: "Apartment",
    note: `Required. One of: ${propertyTypes.join(", ")}.`,
    options: propertyTypes,
  },
  {
    key: "monthlyRent",
    header: "Monthly rent (USD) *",
    required: true,
    width: 18,
    example: 2400,
    note: "Required. Number greater than 0, in dollars.",
  },
  {
    key: "maxDeposit",
    header: "Max deposit (USD) *",
    required: true,
    width: 18,
    example: 2400,
    note: "Required. Maximum security deposit in dollars. 0 is allowed.",
  },
  {
    key: "bedrooms",
    header: "Bedrooms *",
    required: true,
    width: 11,
    example: 2,
    note: "Required. Whole number, 0 for a studio.",
  },
  {
    key: "bathrooms",
    header: "Bathrooms *",
    required: true,
    width: 11,
    example: 1.5,
    note: "Required. Minimum 0.5. Half baths allowed (e.g. 1.5).",
  },
  {
    key: "squareFootage",
    header: "Square footage",
    required: false,
    width: 14,
    example: 950,
    note: "Optional. Whole number.",
  },
  {
    key: "minStayDays",
    header: "Min stay (days)",
    required: false,
    width: 14,
    example: 30,
    note: "Optional. 30–180. Defaults to 30.",
  },
  {
    key: "maxStayDays",
    header: "Max stay (days)",
    required: false,
    width: 14,
    example: 180,
    note: "Optional. 30–180 and at least the min stay. Defaults to 180.",
  },
  {
    key: "maxGuests",
    header: "Max guests",
    required: false,
    width: 11,
    example: 4,
    note: "Optional. Defaults to 2.",
  },
  {
    key: "checkInTime",
    header: "Check-in time",
    required: false,
    width: 13,
    example: "15:00",
    note: "Optional. 24-hour HH:MM. Defaults to 15:00.",
  },
  {
    key: "checkOutTime",
    header: "Check-out time",
    required: false,
    width: 14,
    example: "11:00",
    note: "Optional. 24-hour HH:MM. Defaults to 11:00.",
  },
  {
    key: "petsAllowed",
    header: "Pets allowed",
    required: false,
    width: 12,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "smokingAllowed",
    header: "Smoking allowed",
    required: false,
    width: 15,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "partiesAllowed",
    header: "Parties allowed",
    required: false,
    width: 14,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "quietHoursStart",
    header: "Quiet hours start",
    required: false,
    width: 15,
    example: "22:00",
    note: "Optional. 24-hour HH:MM.",
  },
  {
    key: "quietHoursEnd",
    header: "Quiet hours end",
    required: false,
    width: 15,
    example: "07:00",
    note: "Optional. 24-hour HH:MM.",
  },
  {
    key: "additionalRules",
    header: "Additional rules",
    required: false,
    width: 32,
    example: "No shoes indoors. Recycling is mandatory.",
    note: "Optional. Free text shown with the house rules.",
  },
  {
    key: "cancellationPolicy",
    header: "Cancellation policy",
    required: false,
    width: 18,
    example: "Moderate",
    note: `Optional. One of: ${cancellationTypes.join(", ")}. Defaults to Moderate.`,
    options: cancellationTypes,
  },
  {
    key: "freeCancellationDays",
    header: "Free cancellation days",
    required: false,
    width: 20,
    example: 14,
    note: "Optional. Whole number of days. Defaults to 14.",
  },
  {
    key: "instantBooking",
    header: "Instant booking",
    required: false,
    width: 14,
    example: "No",
    note: "Optional. Yes or No. Defaults to No.",
    options: YES_NO,
  },
  {
    key: "amenities",
    header: "Amenities",
    required: false,
    width: 40,
    example: "WiFi, Dishwasher, In-Unit Washer",
    note: "Optional. Comma-separated names from the Amenities sheet.",
  },
  {
    key: "virtualTourUrl",
    header: "Virtual tour URL",
    required: false,
    width: 28,
    example: "https://example.com/tour",
    note: "Optional. Link to a 3D/virtual tour.",
  },
];

/**
 * Header matching is forgiving: case, punctuation, the required-marker "*"
 * and parenthetical hints like "(USD)" are all ignored, so lightly edited
 * templates still parse.
 */
function normalizeHeader(value: string): string {
  return value
    .toLowerCase()
    .replace(/\([^)]*\)/g, " ")
    .replace(/[^a-z0-9]+/g, "");
}

const COLUMN_BY_NORMALIZED_HEADER = new Map<string, ListingExcelColumn>(
  LISTING_EXCEL_COLUMNS.map((c) => [normalizeHeader(c.header), c]),
);

// ── Template generation ─────────────────────────────────────────

const INSTRUCTIONS: readonly string[] = [
  "How to import listings into Lagedra",
  "",
  "1. Fill in one listing per row on the \"Listings\" sheet. The \"Example\" sheet shows a completed row.",
  "2. Columns marked with * are required. Optional columns use sensible defaults when left blank — hover a column header for details.",
  "3. Amenities are comma-separated and must match the names on the \"Amenities\" sheet (e.g. \"WiFi, Dishwasher\").",
  "4. Times use the 24-hour HH:MM format (e.g. 15:00 for 3 PM).",
  `5. Up to ${MAX_IMPORT_ROWS} listings can be imported per file.`,
  "6. When you're done, upload this file back in Lagedra. Each row becomes a draft listing — nothing is published automatically.",
  "7. After importing, open each draft to add its address, map location and photos, double-check the imported details, and fill in anything missing.",
  "8. Once a draft looks right, use \"Submit for review\" on the listing to send it to the Lagedra team.",
];

function styleHeaderRow(row: ExcelJSTypes.Row, count: number): void {
  for (let i = 1; i <= count; i++) {
    const cell = row.getCell(i);
    cell.font = { bold: true };
    cell.fill = {
      type: "pattern",
      pattern: "solid",
      fgColor: { argb: "FFEFF2F6" },
    };
    cell.border = { bottom: { style: "thin", color: { argb: "FFB8C0CC" } } };
    cell.alignment = { vertical: "middle" };
  }
}

function addListingSheet(
  workbook: ExcelJSTypes.Workbook,
  name: string,
  withExampleRow: boolean,
): void {
  const sheet = workbook.addWorksheet(name, {
    views: [{ state: "frozen", ySplit: 1 }],
  });

  const headerRow = sheet.addRow(LISTING_EXCEL_COLUMNS.map((c) => c.header));
  styleHeaderRow(headerRow, LISTING_EXCEL_COLUMNS.length);

  LISTING_EXCEL_COLUMNS.forEach((column, index) => {
    const col = sheet.getColumn(index + 1);
    col.width = column.width;
    headerRow.getCell(index + 1).note = column.note;

    if (column.options) {
      // In-cell dropdowns for the first 200 data rows.
      for (let rowNumber = 2; rowNumber <= 201; rowNumber++) {
        sheet.getCell(rowNumber, index + 1).dataValidation = {
          type: "list",
          allowBlank: true,
          formulae: [`"${column.options.join(",")}"`],
          showErrorMessage: true,
          errorTitle: "Invalid value",
          error: `Choose one of: ${column.options.join(", ")}`,
        };
      }
    }
  });

  if (withExampleRow) {
    const example = sheet.addRow(LISTING_EXCEL_COLUMNS.map((c) => c.example));
    example.alignment = { vertical: "top", wrapText: true };
  }
}

export async function buildListingImportTemplate(
  amenities: readonly AmenityDefinitionDto[],
): Promise<Blob> {
  const ExcelJS = await loadExcelJS();
  const workbook = new ExcelJS.Workbook();
  workbook.creator = "Lagedra";

  addListingSheet(workbook, LISTINGS_SHEET, false);

  const instructions = workbook.addWorksheet("Instructions");
  instructions.getColumn(1).width = 120;
  for (const line of INSTRUCTIONS) {
    const row = instructions.addRow([line]);
    row.getCell(1).alignment = { wrapText: true, vertical: "top" };
  }
  instructions.getRow(1).font = { bold: true, size: 14 };

  addListingSheet(workbook, "Example", true);

  const amenitySheet = workbook.addWorksheet("Amenities");
  const amenityHeader = amenitySheet.addRow(["Amenity name", "Category"]);
  styleHeaderRow(amenityHeader, 2);
  amenitySheet.getColumn(1).width = 34;
  amenitySheet.getColumn(2).width = 22;
  const sorted = [...amenities].sort((a, b) => a.name.localeCompare(b.name));
  for (const amenity of sorted) {
    amenitySheet.addRow([amenity.name, amenity.category]);
  }

  const buffer = await workbook.xlsx.writeBuffer();
  return new Blob([buffer], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
}

// ── Upload parsing ──────────────────────────────────────────────

export type ParsedListingRow = {
  /** 1-based Excel row number, matching what the host sees in the file. */
  rowNumber: number;
  /** Best-effort title for display, even when the row is invalid. */
  title: string;
  /** Complete form values when the row is valid, otherwise null. */
  values: ListingFormValues | null;
  errors: string[];
  /** Non-blocking issues, e.g. amenity names that couldn't be matched. */
  warnings: string[];
};

export type ParsedListingWorkbook = {
  rows: ParsedListingRow[];
  /** File-level problems (wrong sheet, missing headers, too many rows). */
  fileErrors: string[];
};

type CellValue = ExcelJSTypes.CellValue;

function cellText(value: CellValue): string {
  if (value === null || value === undefined) return "";
  if (typeof value === "string") return value.trim();
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  if (value instanceof Date) return value.toISOString();
  if (typeof value === "object") {
    if ("richText" in value) {
      return value.richText.map((part) => part.text).join("").trim();
    }
    if ("text" in value && typeof value.text === "string") return value.text.trim();
    if ("result" in value) return cellText(value.result as CellValue);
  }
  return "";
}

function cellNumber(value: CellValue): number | undefined {
  if (typeof value === "number") return value;
  const text = cellText(value).replace(/[$,\s]/g, "");
  if (!text) return undefined;
  const parsed = Number(text);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function cellBoolean(value: CellValue): boolean | undefined {
  if (typeof value === "boolean") return value;
  const text = cellText(value).toLowerCase();
  if (!text) return undefined;
  if (["yes", "y", "true", "1"].includes(text)) return true;
  if (["no", "n", "false", "0"].includes(text)) return false;
  return undefined;
}

/** Accepts "15:00", "3:00 PM", Excel time serials and date-typed cells. */
function cellTime(value: CellValue): string | undefined {
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

function isBlank(value: CellValue): boolean {
  return cellText(value).length === 0;
}

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

type RawRecord = Partial<Record<ColumnKey, CellValue>>;

function mapRowToListing(
  raw: RawRecord,
  amenities: readonly AmenityDefinitionDto[],
): Omit<ParsedListingRow, "rowNumber"> {
  const errors: string[] = [];
  const warnings: string[] = [];
  const values: Partial<ListingFormValues> = {};
  const title = cellText(raw.title) || "(untitled)";

  for (const column of LISTING_EXCEL_COLUMNS) {
    if (column.required && isBlank(raw[column.key])) {
      errors.push(`${column.header.replace(" *", "")} is required`);
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
    const column = LISTING_EXCEL_COLUMNS.find((c) => c.key === key)!;
    if (parsed === undefined) {
      errors.push(`${column.header.replace(" *", "")} must be a number`);
      return;
    }
    apply(integer ? Math.round(parsed) : parsed);
  };

  const setBoolean = (key: ColumnKey, apply: (b: boolean) => void) => {
    const cell = raw[key];
    if (cell === undefined || isBlank(cell)) return;
    const parsed = cellBoolean(cell);
    const column = LISTING_EXCEL_COLUMNS.find((c) => c.key === key)!;
    if (parsed === undefined) {
      errors.push(`${column.header} must be Yes or No`);
      return;
    }
    apply(parsed);
  };

  const setTime = (key: ColumnKey, apply: (t: string) => void) => {
    const cell = raw[key];
    if (cell === undefined || isBlank(cell)) return;
    const parsed = cellTime(cell);
    const column = LISTING_EXCEL_COLUMNS.find((c) => c.key === key)!;
    if (parsed === undefined) {
      errors.push(`${column.header} must be a time like 15:00`);
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
      const id = byName.get(normalizeAmenityName(trimmed));
      if (id) {
        matchedIds.add(id);
      } else {
        unmatched.push(trimmed);
      }
    }
    values.amenityIds = [...matchedIds];
    if (unmatched.length > 0) {
      warnings.push(
        `Amenities not recognized and skipped: ${unmatched.join(", ")}. See the Amenities sheet in the template.`,
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

export async function parseListingImportWorkbook(
  data: ArrayBuffer,
  amenities: readonly AmenityDefinitionDto[],
): Promise<ParsedListingWorkbook> {
  const ExcelJS = await loadExcelJS();
  const workbook = new ExcelJS.Workbook();
  try {
    await workbook.xlsx.load(data);
  } catch {
    return {
      rows: [],
      fileErrors: ["This file could not be read. Upload the .xlsx template downloaded from Lagedra."],
    };
  }

  const sheet = workbook.getWorksheet(LISTINGS_SHEET) ?? workbook.worksheets[0];
  if (!sheet) {
    return { rows: [], fileErrors: ["The workbook has no sheets."] };
  }

  // Map spreadsheet columns back to template columns by header text.
  const columnByIndex = new Map<number, ListingExcelColumn>();
  sheet.getRow(1).eachCell((cell, colNumber) => {
    const column = COLUMN_BY_NORMALIZED_HEADER.get(normalizeHeader(cellText(cell.value)));
    if (column) columnByIndex.set(colNumber, column);
  });

  const mappedKeys = new Set([...columnByIndex.values()].map((c) => c.key));
  const missingRequired = LISTING_EXCEL_COLUMNS.filter(
    (c) => c.required && !mappedKeys.has(c.key),
  );
  if (missingRequired.length > 0) {
    return {
      rows: [],
      fileErrors: [
        `This file doesn't match the Lagedra template — missing columns: ${missingRequired
          .map((c) => c.header.replace(" *", ""))
          .join(", ")}. Download a fresh template and try again.`,
      ],
    };
  }

  const rows: ParsedListingRow[] = [];
  const fileErrors: string[] = [];

  for (let rowNumber = 2; rowNumber <= sheet.rowCount; rowNumber++) {
    const row = sheet.getRow(rowNumber);
    const raw: RawRecord = {};
    let hasContent = false;
    for (const [colNumber, column] of columnByIndex) {
      const value = row.getCell(colNumber).value;
      raw[column.key] = value;
      if (!isBlank(value)) hasContent = true;
    }
    if (!hasContent) continue;

    if (rows.length >= MAX_IMPORT_ROWS) {
      fileErrors.push(
        `Only the first ${MAX_IMPORT_ROWS} listings were read. Split larger imports into multiple files.`,
      );
      break;
    }

    rows.push({ rowNumber, ...mapRowToListing(raw, amenities) });
  }

  if (rows.length === 0 && fileErrors.length === 0) {
    fileErrors.push("No listings were found in the file. Fill in the Listings sheet and upload it again.");
  }

  return { rows, fileErrors };
}
