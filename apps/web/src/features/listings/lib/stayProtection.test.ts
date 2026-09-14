import { describe, expect, it } from "vitest";
import {
  STAY_PROTECTION_CLAIM_QUERY,
  STAY_PROTECTION_GUEST_AGREEMENT_URL,
  STAY_PROTECTION_LABEL,
  isStayProtectionClaimable,
  stayProtectionDamageClaimPath,
  stayProtectionStatus,
} from "./stayProtection";

describe("stayProtection", () => {
  it("uses tenant-facing stay protection copy", () => {
    expect(STAY_PROTECTION_LABEL).toBe("Stay protection");
    expect(STAY_PROTECTION_GUEST_AGREEMENT_URL).toContain("guest-agreement");
  });

  it("maps Truvi screening to a clear insurance status", () => {
    expect(stayProtectionStatus(null).headline).toBe("Not requested");
    expect(stayProtectionStatus({ screeningStatus: "Approved" })).toMatchObject({
      headline: "Protected",
      screeningLabel: "Approved",
      badgeVariant: "success",
    });
    expect(stayProtectionStatus({
      screeningStatus: "Flagged",
      flaggedReason: "Watchlist",
    })).toMatchObject({
      headline: "Protected (flagged)",
      detail: "Watchlist",
      badgeVariant: "warning",
    });
    expect(stayProtectionStatus({ screeningStatus: "Rejected" })).toMatchObject({
      headline: "Not protected",
      badgeVariant: "destructive",
    });
    expect(stayProtectionStatus({ state: "Unknown" })).toMatchObject({
      headline: "Checking",
      badgeVariant: "warning",
    });
  });

  it("only allows claims when stay protection is active", () => {
    expect(isStayProtectionClaimable({ screeningStatus: "Approved" })).toBe(true);
    expect(isStayProtectionClaimable({ screeningStatus: "Flagged" })).toBe(true);
    expect(isStayProtectionClaimable({ screeningStatus: "Rejected" })).toBe(false);
    expect(isStayProtectionClaimable(null)).toBe(false);
    expect(stayProtectionDamageClaimPath("deal-1")).toBe(
      `/app/deals/deal-1/billing?${STAY_PROTECTION_CLAIM_QUERY}=1`,
    );
  });
});
