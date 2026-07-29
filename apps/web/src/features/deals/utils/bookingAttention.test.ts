import { describe, expect, it } from "vitest";
import {
  daysUntilDate,
  getDealIssue,
  getEndingSoon,
  NEAR_END_DAYS,
} from "./bookingAttention";
import type { DealSummaryDto } from "@/api/types";

function deal(partial: Partial<DealSummaryDto>): DealSummaryDto {
  return {
    dealId: "d1",
    applicationId: "a1",
    listingId: "l1",
    listingTitle: "Hidden Gem in Encino",
    listingCity: "Encino",
    listingCoverPhotoUri: null,
    landlordUserId: "host",
    tenantUserId: "guest",
    applicationStatus: "Approved",
    dealPhase: "Active",
    requestedCheckIn: "2026-06-26",
    requestedCheckOut: "2026-07-31",
    stayDurationDays: 35,
    monthlyRentCents: 150000,
    depositAmountCents: 150000,
    totalAmountCents: 300000,
    billingStatus: null,
    paymentStatus: null,
    createdAt: "2026-06-01T00:00:00Z",
    truthSurfaceLocked: true,
    ...partial,
  };
}

describe("bookingAttention", () => {
  it("flags payment failed as a critical host issue with resolution copy", () => {
    const issue = getDealIssue(
      deal({ dealPhase: "PaymentFailed" }),
      "host",
    );
    expect(issue?.kind).toBe("PaymentFailed");
    expect(issue?.title).toContain("Payment failed");
    expect(issue?.resolution.length).toBeGreaterThan(10);
    expect(issue?.href).toBe("/app/deals/d1");
  });

  it("shows ending soon within 15 days for active bookings", () => {
    const now = new Date(2026, 6, 20); // Jul 20 local
    const ending = getEndingSoon(
      deal({
        dealPhase: "Active",
        requestedCheckOut: "2026-07-31",
      }),
      now,
    );
    expect(ending?.daysRemaining).toBe(11);
    expect(ending?.label).toBe("11 days remaining");
  });

  it("does not mark ending soon beyond the window", () => {
    const now = new Date(2026, 6, 1);
    const ending = getEndingSoon(
      deal({
        dealPhase: "Active",
        requestedCheckOut: "2026-07-31",
      }),
      now,
    );
    expect(ending).toBeNull();
    expect(NEAR_END_DAYS).toBe(15);
  });

  it("computes days until checkout from local calendar dates", () => {
    const now = new Date(2026, 6, 17);
    expect(daysUntilDate("2026-07-17", now)).toBe(0);
    expect(daysUntilDate("2026-07-18", now)).toBe(1);
  });

  it("shows deposit return attention until the acting party has confirmed", () => {
    const issue = getDealIssue(
      deal({ dealPhase: "AwaitingDepositReturn" }),
      "host",
    );
    expect(issue?.kind).toBe("AwaitingDepositReturn");
    expect(issue?.title).toContain("Deposit return needed");
  });

  it("hides deposit return attention after the host has confirmed the return", () => {
    const issue = getDealIssue(
      deal({
        dealPhase: "AwaitingDepositReturn",
        hostConfirmedDepositReturnedAt: "2026-07-20T00:00:00Z",
      }),
      "host",
    );
    expect(issue?.kind).toBe("AwaitingDepositReturn");
    expect(issue?.title).toContain("Waiting on guest");
    expect(issue?.ctaLabel).toBe("View booking");
  });

  it("hides host attention once the guest has also confirmed", () => {
    const issue = getDealIssue(
      deal({
        dealPhase: "AwaitingDepositReturn",
        hostConfirmedDepositReturnedAt: "2026-07-20T00:00:00Z",
        tenantConfirmedDepositReceivedAt: "2026-07-21T00:00:00Z",
      }),
      "host",
    );
    expect(issue).toBeNull();
  });

  it("still prompts the guest after the host has confirmed", () => {
    const issue = getDealIssue(
      deal({
        dealPhase: "AwaitingDepositReturn",
        hostConfirmedDepositReturnedAt: "2026-07-20T00:00:00Z",
      }),
      "guest",
    );
    expect(issue?.kind).toBe("AwaitingDepositReturn");
  });

  it("hides deposit return attention once settled", () => {
    const issue = getDealIssue(
      deal({
        dealPhase: "AwaitingDepositReturn",
        depositReturnSettledAt: "2026-07-21T00:00:00Z",
      }),
      "guest",
    );
    expect(issue).toBeNull();
  });
});
