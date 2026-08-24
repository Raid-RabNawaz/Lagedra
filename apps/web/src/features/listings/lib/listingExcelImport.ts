import type * as ExcelJSTypes from "exceljs";
import type { AmenityDefinitionDto } from "@/api/types";
import {
  LISTING_IMPORT_COLUMNS,
  MAX_IMPORT_ROWS,
  cellText,
  isBlank,
  mapRowToListing,
  normalizeImportName,
  type ListingImportColumn,
  type ParsedListingFile,
  type ParsedListingRow,
  type RawRecord,
} from "./listingImportShared";

/**
 * Bulk "Import from Excel" support. This module owns both the downloadable
 * template and the workbook parser so the two can never drift apart: the
 * template is generated from LISTING_EXCEL_COLUMNS and uploads are matched
 * back to the same columns by (normalized) header text. Row validation is
 * shared with the XML importer via listingImportShared.ts.
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

export type ListingExcelColumn = ListingImportColumn & {
  /** Header cell text; required columns are marked with "*". */
  header: string;
};

export const LISTING_EXCEL_COLUMNS: readonly ListingExcelColumn[] =
  LISTING_IMPORT_COLUMNS.map((column) => ({
    ...column,
    header: column.required ? `${column.label} *` : column.label,
  }));

const COLUMN_BY_NORMALIZED_HEADER = new Map<string, ListingExcelColumn>(
  LISTING_EXCEL_COLUMNS.map((c) => [normalizeImportName(c.header), c]),
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
  });

  // Add the sample BEFORE attaching dropdowns. Calling getCell() for
  // validation on rows 2–201 materializes empty rows; if we did that first,
  // addRow() would append the example at row 202 and the sheet would look blank.
  if (withExampleRow) {
    const example = sheet.addRow(LISTING_EXCEL_COLUMNS.map((c) => c.example));
    example.alignment = { vertical: "top", wrapText: true };
  }

  LISTING_EXCEL_COLUMNS.forEach((column, index) => {
    if (!column.options) return;
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
  });
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

export async function parseListingImportWorkbook(
  data: ArrayBuffer,
  amenities: readonly AmenityDefinitionDto[],
): Promise<ParsedListingFile> {
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
    const column = COLUMN_BY_NORMALIZED_HEADER.get(normalizeImportName(cellText(cell.value)));
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
          .map((c) => c.label)
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
