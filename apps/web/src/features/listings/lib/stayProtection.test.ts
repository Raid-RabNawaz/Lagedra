import { describe, expect, it } from "vitest";
import {
  STAY_PROTECTION_GUEST_AGREEMENT_URL,
  STAY_PROTECTION_LABEL,
} from "./stayProtection";

describe("stayProtection", () => {
  it("uses tenant-facing stay protection copy", () => {
    expect(STAY_PROTECTION_LABEL).toBe("Stay protection");
    expect(STAY_PROTECTION_GUEST_AGREEMENT_URL).toContain("guest-agreement");
  });
});
