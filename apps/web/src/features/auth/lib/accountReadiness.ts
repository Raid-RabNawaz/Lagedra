import type { ProfileSignalStatus } from "@/features/auth/lib/profileCompleteness";
import type {
  HostStripeStatusDto,
  UserProfileDto,
  VerificationStatus,
} from "@/api/types";

export type ReadinessAudience = "host" | "guest";

export type ReadinessStepId = "identity" | "payouts" | "eligible";

export type ReadinessStep = {
  id: ReadinessStepId;
  label: string;
  detail: string;
  status: ProfileSignalStatus;
  href: string;
};

export type AccountReadiness = {
  audience: ReadinessAudience;
  title: string;
  description: string;
  steps: ReadinessStep[];
  completedCount: number;
  percent: number;
  isEligible: boolean;
};

export function identityReadinessStatus(
  user: Pick<UserProfileDto, "isGovernmentIdVerified">,
  kycStatus?: VerificationStatus | null,
): ProfileSignalStatus {
  if (user.isGovernmentIdVerified || kycStatus === "Verified") return "complete";
  if (kycStatus === "Pending" || kycStatus === "ManualReviewRequired") {
    return "pending";
  }
  return "empty";
}

export function payoutReadinessStatus(
  stripe?: Pick<
    HostStripeStatusDto,
    "chargesEnabled" | "payoutsEnabled" | "detailsSubmitted" | "onboardingStatus"
  > | null,
): ProfileSignalStatus {
  if (stripe?.chargesEnabled && stripe.payoutsEnabled) return "complete";
  if (
    stripe?.detailsSubmitted
    || stripe?.onboardingStatus === "Pending"
    || stripe?.onboardingStatus === "Restricted"
  ) {
    return "pending";
  }
  return "empty";
}

/**
 * Hosts must verify identity and finish payout setup before they can accept
 * a paid booking. Guests only need identity. Background checks are not a
 * pre-booking step for either side, so they never appear here.
 */
export function buildAccountReadiness(input: {
  audience: ReadinessAudience;
  user: Pick<UserProfileDto, "isGovernmentIdVerified">;
  kycStatus?: VerificationStatus | null;
  stripe?: Parameters<typeof payoutReadinessStatus>[0];
}): AccountReadiness {
  const identity = identityReadinessStatus(input.user, input.kycStatus);
  const isHost = input.audience === "host";
  const payouts = isHost ? payoutReadinessStatus(input.stripe) : "complete";
  const isEligible = identity === "complete" && (!isHost || payouts === "complete");

  const steps: ReadinessStep[] = [
    {
      id: "identity",
      label: "Identity",
      detail: "Verify a government-issued ID so the other party knows who you are.",
      status: identity,
      href: "/app/verification#kyc",
    },
  ];

  if (isHost) {
    steps.push({
      id: "payouts",
      label: "Payout setup",
      detail: "Connect Stripe so first-month rent and the deposit can be paid to you.",
      status: payouts,
      href: "/app/payout-setup",
    });
  }

  steps.push({
    id: "eligible",
    label: isHost ? "Ready to accept bookings" : "Ready to request a booking",
    detail: isHost
      ? "Identity and payouts must be complete before you can accept a paid stay."
      : "Identity must be verified before you can request a booking.",
    status: isEligible
      ? "complete"
      : identity === "pending" || payouts === "pending"
        ? "pending"
        : "empty",
    href: isHost ? "/app/listings" : "/listings",
  });

  const required = steps.filter((s) => s.id === "identity" || s.id === "payouts");
  const completedRequired = required.filter((s) => s.status === "complete").length;
  const percent = Math.round((completedRequired * 100) / required.length);

  return {
    audience: input.audience,
    title: isHost ? "Ready to host" : "Ready to book",
    description: isHost
      ? "Finish these steps so guests can pay you and stay with confidence."
      : "Finish the steps you need before you request a stay.",
    steps,
    completedCount: steps.filter((s) => s.status === "complete").length,
    percent,
    isEligible,
  };
}
