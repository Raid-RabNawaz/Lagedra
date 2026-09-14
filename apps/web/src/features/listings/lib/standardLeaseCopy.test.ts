import { describe, expect, it } from "vitest";
import {
  DEFAULT_LEASE_JURISDICTION_NAME,
  resolveLeaseJurisdictionName,
  tenantStandardLeaseTitle,
} from "./standardLeaseCopy";

describe("standardLeaseCopy", () => {
  it("defaults to California when the listing has no jurisdiction", () => {
    expect(resolveLeaseJurisdictionName(undefined)).toBe(
      DEFAULT_LEASE_JURISDICTION_NAME,
    );
    expect(tenantStandardLeaseTitle(null)).toBe(
      "California's standard lease agreement",
    );
  });

  it("names the US state from a jurisdiction code", () => {
    expect(tenantStandardLeaseTitle("US-CA")).toBe(
      "California's standard lease agreement",
    );
    expect(tenantStandardLeaseTitle("us-ca-la")).toBe(
      "California's standard lease agreement",
    );
    expect(tenantStandardLeaseTitle("US-NY")).toBe(
      "New York's standard lease agreement",
    );
  });

  it("uses a trailing apostrophe for state names that already end in s", () => {
    expect(tenantStandardLeaseTitle("US-TX")).toBe(
      "Texas' standard lease agreement",
    );
  });
});
