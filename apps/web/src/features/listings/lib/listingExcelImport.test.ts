import { describe, it, expect } from "vitest";
import ExcelJS from "exceljs";
import type { AmenityDefinitionDto } from "@/api/types";
import {
  LISTING_EXCEL_COLUMNS,
  buildListingImportTemplate,
  parseListingImportWorkbook,
} from "./listingExcelImport";

const amenities: AmenityDefinitionDto[] = [
  { id: "a-wifi", name: "WiFi", category: "Internet", iconKey: "wifi" },
  { id: "a-dishwasher", name: "Dishwasher", category: "Kitchen", iconKey: "dw" },
];

const VALID_DESCRIPTION =
  "Bright, fully furnished two-bedroom apartment with a dedicated workspace and fast Wi-Fi.";

type RowInput = Partial<Record<(typeof LISTING_EXCEL_COLUMNS)[number]["key"], unknown>>;

async function workbookWithRows(rows: RowInput[]): Promise<ArrayBuffer> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet("Listings");
  sheet.addRow(LISTING_EXCEL_COLUMNS.map((c) => c.header));
  for (const row of rows) {
    sheet.addRow(LISTING_EXCEL_COLUMNS.map((c) => row[c.key] ?? null));
  }
  return workbook.xlsx.writeBuffer() as Promise<ArrayBuffer>;
}

function validRow(overrides: RowInput = {}): RowInput {
  return {
    title: "Sunny riverside apartment",
    description: VALID_DESCRIPTION,
    propertyType: "Apartment",
    monthlyRent: 2400,
    maxDeposit: 2400,
    bedrooms: 2,
    bathrooms: 1.5,
    ...overrides,
  };
}

describe("parseListingImportWorkbook", () => {
  it("parses a valid row into complete form values with defaults applied", async () => {
    const data = await workbookWithRows([
      validRow({
        maxGuests: 4,
        checkInTime: "16:00",
        petsAllowed: "Yes",
        amenities: "wifi, Dishwasher",
        instantBooking: "Yes",
      }),
    ]);

    const result = await parseListingImportWorkbook(data, amenities);

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
    // Defaults for columns left blank.
    expect(row.values!.minStayDays).toBe(30);
    expect(row.values!.maxStayDays).toBe(180);
    expect(row.values!.checkOutTime).toBe("11:00");
  });

  it("rejects rows with missing required fields and reports which", async () => {
    const data = await workbookWithRows([
      validRow({ monthlyRent: null, propertyType: null }),
    ]);

    const result = await parseListingImportWorkbook(data, amenities);

    const row = result.rows[0];
    expect(row.values).toBeNull();
    expect(row.errors).toContain("Monthly rent (USD) is required");
    expect(row.errors).toContain("Property type is required");
  });

  it("rejects invalid values with labeled validation messages", async () => {
    const data = await workbookWithRows([
      validRow({ title: "Hey", description: "Too short", bathrooms: 0 }),
    ]);

    const result = await parseListingImportWorkbook(data, amenities);

    const row = result.rows[0];
    expect(row.values).toBeNull();
    expect(row.errors.some((e) => e.startsWith("Title:"))).toBe(true);
    expect(row.errors.some((e) => e.startsWith("Description:"))).toBe(true);
    expect(row.errors.some((e) => e.startsWith("Bathrooms:"))).toBe(true);
  });

  it("warns about unmatched amenities but keeps the row valid", async () => {
    const data = await workbookWithRows([
      validRow({ amenities: "WiFi, Golden Slide" }),
    ]);

    const result = await parseListingImportWorkbook(data, amenities);

    const row = result.rows[0];
    expect(row.values).not.toBeNull();
    expect(row.values!.amenityIds).toEqual(["a-wifi"]);
    expect(row.warnings[0]).toContain("Golden Slide");
  });

  it("skips fully empty rows and reports a file error when nothing is left", async () => {
    const data = await workbookWithRows([{}, {}]);

    const result = await parseListingImportWorkbook(data, amenities);

    expect(result.rows).toEqual([]);
    expect(result.fileErrors).toHaveLength(1);
  });

  it("rejects files that don't match the template", async () => {
    const workbook = new ExcelJS.Workbook();
    const sheet = workbook.addWorksheet("Listings");
    sheet.addRow(["Name", "Price"]);
    sheet.addRow(["Some house", 1200]);
    const data = (await workbook.xlsx.writeBuffer()) as ArrayBuffer;

    const result = await parseListingImportWorkbook(data, amenities);

    expect(result.rows).toEqual([]);
    expect(result.fileErrors[0]).toContain("doesn't match the Lagedra template");
  });

  it("puts a filled-in sample on row 2 of the Example sheet", async () => {
    const blob = await buildListingImportTemplate(amenities);
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.load(await blob.arrayBuffer());
    const example = workbook.getWorksheet("Example");
    expect(example).toBeTruthy();

    // Row 1 = headers, row 2 = sample listing (not buried after empty
    // validation rows).
    expect(String(example!.getRow(1).getCell(1).value)).toContain("Title");
    expect(String(example!.getRow(2).getCell(1).value)).toContain("Sunny");
    expect(example!.getRow(2).getCell(4).value).toBe(2400);
  });

  it("round-trips the generated template's Example sheet headers", async () => {
    // The downloadable template itself must parse cleanly: fill its Listings
    // sheet with one row and re-upload.
    const blob = await buildListingImportTemplate(amenities);
    const workbook = new ExcelJS.Workbook();
    await workbook.xlsx.load(await blob.arrayBuffer());
    const sheet = workbook.getWorksheet("Listings")!;
    sheet.addRow(LISTING_EXCEL_COLUMNS.map((c) => validRow()[c.key] ?? null));
    const data = (await workbook.xlsx.writeBuffer()) as ArrayBuffer;

    const result = await parseListingImportWorkbook(data, amenities);

    expect(result.fileErrors).toEqual([]);
    expect(result.rows).toHaveLength(1);
    expect(result.rows[0].values).not.toBeNull();
  });
});
