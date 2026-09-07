import { describe, expect, it } from "vitest";
import { COMPANY, COMPANY_MAILING_ADDRESS } from "./companyInfo";
import { privacyDocument } from "./privacyContent";
import { termsDocument } from "./termsContent";

function uniqueIds(ids: string[]) {
  return new Set(ids).size === ids.length;
}

describe("legal documents", () => {
  it("uses unique section ids on each page", () => {
    expect(uniqueIds(termsDocument.sections.map((s) => s.id))).toBe(true);
    expect(uniqueIds(privacyDocument.sections.map((s) => s.id))).toBe(true);
  });

  it("covers the platform topics guests and hosts need", () => {
    const termIds = termsDocument.sections.map((s) => s.id);
    expect(termIds).toEqual(
      expect.arrayContaining([
        "platform",
        "hosts",
        "guests",
        "payments",
        "agreements",
        "verification",
        "disputes",
        "sms",
        "contact",
      ]),
    );

    const privacyIds = privacyDocument.sections.map((s) => s.id);
    expect(privacyIds).toEqual(
      expect.arrayContaining(["collect", "share", "sms", "location", "cookies", "rights", "contact"]),
    );
  });

  it("publishes the legal entity and mailing address", () => {
    expect(COMPANY.legalName).toBe("Lagedra LLC");
    expect(COMPANY_MAILING_ADDRESS).toContain("14622 Ventura Boulevard");
    expect(COMPANY_MAILING_ADDRESS).toContain("Sherman Oaks");
  });
});
