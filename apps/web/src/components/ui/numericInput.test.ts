import { describe, expect, it } from "vitest";
import {
  formatNumericValue,
  parseNumericInput,
  sanitizeNumericInput,
} from "./numericInput";

describe("sanitizeNumericInput", () => {
  it("strips letters and extra dots while keeping a trailing decimal", () => {
    expect(sanitizeNumericInput("79.")).toBe("79.");
    expect(sanitizeNumericInput("12.3.4", { decimalPlaces: 2 })).toBe("12.34");
    expect(sanitizeNumericInput("e12")).toBe("12");
  });

  it("blocks decimals for integers", () => {
    expect(sanitizeNumericInput("12.5", { decimalPlaces: 0 })).toBe("125");
  });

  it("allows a leading minus when negative values are permitted", () => {
    expect(sanitizeNumericInput("-4.2", { min: -10, decimalPlaces: 1 })).toBe("-4.2");
    expect(sanitizeNumericInput("-4", { min: 0 })).toBe("4");
  });
});

describe("parseNumericInput", () => {
  it("treats in-progress drafts as empty", () => {
    expect(parseNumericInput("")).toBeNull();
    expect(parseNumericInput(".")).toBeNull();
    expect(parseNumericInput("-")).toBeNull();
  });

  it("parses complete numbers", () => {
    expect(parseNumericInput("79.")).toBe(79);
    expect(parseNumericInput("10.5")).toBe(10.5);
    expect(parseNumericInput("0")).toBe(0);
  });
});

describe("formatNumericValue", () => {
  it("formats committed values without inventing text for empty", () => {
    expect(formatNumericValue(null)).toBe("");
    expect(formatNumericValue(79, 2)).toBe("79.00");
    expect(formatNumericValue(10.5, 2)).toBe("10.50");
    expect(formatNumericValue(4, 0)).toBe("4");
  });
});
