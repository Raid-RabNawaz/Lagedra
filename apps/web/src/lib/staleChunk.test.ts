import { describe, expect, it } from "vitest";
import { isStaleChunkError } from "./staleChunk";

describe("isStaleChunkError", () => {
  it("recognizes Chrome's dynamic import TypeError", () => {
    expect(
      isStaleChunkError(
        new TypeError(
          "Failed to fetch dynamically imported module: https://staging.lagedra.com/assets/MarketplaceHomePage-eedG43eA.js",
        ),
      ),
    ).toBe(true);
  });

  it("recognizes a failed named lazy export after a poisoned chunk", () => {
    expect(
      isStaleChunkError(
        new TypeError("Cannot read properties of undefined (reading 'MarketplaceHomePage')"),
      ),
    ).toBe(true);
  });

  it("recognizes Firefox and webpack wordings", () => {
    expect(
      isStaleChunkError(new Error("error loading dynamically imported module")),
    ).toBe(true);
    expect(isStaleChunkError("Importing a module script failed.")).toBe(true);
    expect(isStaleChunkError(new Error("Loading chunk 12 failed"))).toBe(true);
  });

  it("ignores unrelated errors", () => {
    expect(isStaleChunkError(new Error("Network Error"))).toBe(false);
    expect(isStaleChunkError(null)).toBe(false);
  });
});
