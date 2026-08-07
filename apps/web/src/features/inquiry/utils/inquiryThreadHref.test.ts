import { describe, expect, it } from "vitest";
import { inquiryThreadHref } from "./inquiryThreadHref";

describe("inquiryThreadHref", () => {
  it("routes pre-booking threads to the session page", () => {
    expect(inquiryThreadHref({ sessionId: "s1", dealId: null })).toBe(
      "/app/inquiry/s1",
    );
  });

  it("routes deal-linked threads to the booking conversation", () => {
    expect(inquiryThreadHref({ sessionId: "s1", dealId: "d1" })).toBe(
      "/app/deals/d1/inquiry",
    );
  });
});
