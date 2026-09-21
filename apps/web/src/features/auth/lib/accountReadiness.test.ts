import { describe, expect, it } from "vitest";
import {
  buildAccountReadiness,
  identityReadinessStatus,
  payoutReadinessStatus,
} from "./accountReadiness";

describe("accountReadiness", () => {
  it("treats government ID or a verified KYC case as identity complete", () => {
    expect(identityReadinessStatus({ isGovernmentIdVerified: true })).toBe("complete");
    expect(identityReadinessStatus({ isGovernmentIdVerified: false }, "Verified")).toBe(
      "complete",
    );
    expect(identityReadinessStatus({ isGovernmentIdVerified: false }, "Pending")).toBe(
      "pending",
    );
    expect(identityReadinessStatus({ isGovernmentIdVerified: false })).toBe("empty");
  });

  it("marks payouts complete only when charges and payouts are enabled", () => {
    expect(payoutReadinessStatus(null)).toBe("empty");
    expect(
      payoutReadinessStatus({
        chargesEnabled: false,
        payoutsEnabled: false,
        detailsSubmitted: true,
        onboardingStatus: "Pending",
      }),
    ).toBe("pending");
    expect(
      payoutReadinessStatus({
        chargesEnabled: true,
        payoutsEnabled: true,
        detailsSubmitted: true,
        onboardingStatus: "Completed",
      }),
    ).toBe("complete");
  });

  it("omits payouts and background check for guests", () => {
    const readiness = buildAccountReadiness({
      audience: "guest",
      user: { isGovernmentIdVerified: true },
      kycStatus: "Verified",
    });

    expect(readiness.steps.map((s) => s.id)).toEqual(["identity", "eligible"]);
    expect(readiness.isEligible).toBe(true);
    expect(readiness.percent).toBe(100);
    expect(readiness.steps.find((s) => s.id === "payouts")).toBeUndefined();
  });

  it("requires identity and payouts before a host is ready to accept", () => {
    const blocked = buildAccountReadiness({
      audience: "host",
      user: { isGovernmentIdVerified: true },
      kycStatus: "Verified",
      stripe: {
        chargesEnabled: false,
        payoutsEnabled: false,
        detailsSubmitted: false,
        onboardingStatus: "Pending",
      },
    });

    expect(blocked.steps.map((s) => s.id)).toEqual(["identity", "payouts", "eligible"]);
    expect(blocked.isEligible).toBe(false);
    expect(blocked.percent).toBe(50);

    const ready = buildAccountReadiness({
      audience: "host",
      user: { isGovernmentIdVerified: true },
      kycStatus: "Verified",
      stripe: {
        chargesEnabled: true,
        payoutsEnabled: true,
        detailsSubmitted: true,
        onboardingStatus: "Completed",
      },
    });

    expect(ready.isEligible).toBe(true);
    expect(ready.percent).toBe(100);
  });
});
