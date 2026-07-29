import { describe, expect, it } from "vitest";
import { getNotificationRoute } from "./getNotificationRoute";
import type { InAppNotificationDto } from "@/api/types";

function n(
  partial: Partial<InAppNotificationDto> & Pick<InAppNotificationDto, "category">,
): InAppNotificationDto {
  return {
    id: "n1",
    title: "t",
    body: "b",
    relatedEntityId: partial.relatedEntityId ?? null,
    relatedEntityType: partial.relatedEntityType ?? null,
    isRead: false,
    createdAt: new Date().toISOString(),
    ...partial,
  };
}

describe("getNotificationRoute", () => {
  it("routes truth_surface deal notifications to deal truth-surface page", () => {
    expect(
      getNotificationRoute(
        n({
          category: "truth_surface_initiated",
          relatedEntityType: "Deal",
          relatedEntityId: "deal-1",
        }),
      ),
    ).toBe("/app/deals/deal-1/truth-surface");
  });

  it("does not treat deal id as a snapshot id", () => {
    const route = getNotificationRoute(
      n({
        category: "truth_surface_confirmed",
        relatedEntityType: "Deal",
        relatedEntityId: "deal-1",
      }),
    );
    expect(route).not.toContain("/app/truth-surface/");
  });

  it("routes inquiry and arbitration entities", () => {
    expect(
      getNotificationRoute(
        n({
          category: "inquiry_started",
          relatedEntityType: "InquirySession",
          relatedEntityId: "sess-1",
        }),
      ),
    ).toBe("/app/inquiry/sess-1");

    expect(
      getNotificationRoute(
        n({
          category: "arbitration_case_filed",
          relatedEntityType: "ArbitrationCase",
          relatedEntityId: "case-1",
        }),
      ),
    ).toBe("/app/arbitration/case-1");
  });

  it("routes review reminders to the deal detail (not billing)", () => {
    expect(
      getNotificationRoute(
        n({
          category: "review_due",
          relatedEntityType: "Deal",
          relatedEntityId: "deal-1",
        }),
      ),
    ).toBe("/app/deals/deal-1");
  });

  it("routes listing host notifications to the owner listing page", () => {
    expect(
      getNotificationRoute(
        n({
          category: "listing_published",
          relatedEntityType: "Listing",
          relatedEntityId: "list-1",
        }),
      ),
    ).toBe("/app/listings/list-1");
  });

  it("routes compliance deal notifications to compliance page", () => {
    expect(
      getNotificationRoute(
        n({
          category: "compliance_violation_created",
          relatedEntityType: "Deal",
          relatedEntityId: "deal-1",
        }),
      ),
    ).toBe("/app/deals/deal-1/compliance");
  });
});
