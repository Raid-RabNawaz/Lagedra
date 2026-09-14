export const STAY_PROTECTION_LABEL = "Stay protection";
export const STAY_PROTECTION_GUEST_AGREEMENT_URL =
  "https://truvi.com/screen-and-protect/guest-agreement/";
export const STAY_PROTECTION_CLAIM_QUERY = "claim";

export type StayProtectionBadgeVariant =
  | "success"
  | "warning"
  | "destructive"
  | "secondary"
  | "outline";

export type StayProtectionStatusInput = {
  state?: string | null;
  screeningStatus?: string | null;
  coverageScope?: string | null;
  flaggedReason?: string | null;
};

export type StayProtectionStatusView = {
  headline: string;
  screeningLabel: string | null;
  detail: string | null;
  badgeVariant: StayProtectionBadgeVariant;
};

export function stayProtectionStatus(
  input: StayProtectionStatusInput | null | undefined,
): StayProtectionStatusView {
  if (!input) {
    return {
      headline: "Not requested",
      screeningLabel: null,
      detail: "Stay protection has not been requested for this booking yet.",
      badgeVariant: "outline",
    };
  }

  const screening = input.screeningStatus?.trim() || null;
  const coverage = input.coverageScope?.trim() || null;
  const flagged = input.flaggedReason?.trim() || null;

  switch (screening) {
    case "Approved":
      return {
        headline: "Protected",
        screeningLabel: "Approved",
        detail: coverage ?? "Complete Protection is active for this stay.",
        badgeVariant: "success",
      };
    case "Flagged":
      return {
        headline: "Protected (flagged)",
        screeningLabel: "Flagged",
        detail: flagged ?? coverage ?? "Protection is active at a reduced level.",
        badgeVariant: "warning",
      };
    case "Rejected":
      return {
        headline: "Not protected",
        screeningLabel: "Rejected",
        detail: coverage ?? "Screening was rejected. This stay is not protected.",
        badgeVariant: "destructive",
      };
    case "Cancelled":
      return {
        headline: "Cancelled",
        screeningLabel: "Cancelled",
        detail: coverage ?? "Stay protection was cancelled.",
        badgeVariant: "secondary",
      };
    case "Failed":
      return {
        headline: "Could not confirm",
        screeningLabel: "Failed",
        detail: coverage ?? "The screening request did not complete.",
        badgeVariant: "destructive",
      };
    default:
      break;
  }

  switch (input.state) {
    case "Active":
    case "InstitutionBacked":
      return {
        headline: "Protected",
        screeningLabel: screening,
        detail: coverage,
        badgeVariant: "success",
      };
    case "Unknown":
      return {
        headline: "Checking",
        screeningLabel: screening,
        detail: coverage ?? "Stay protection status is still being confirmed.",
        badgeVariant: "warning",
      };
    case "NotActive":
      return {
        headline: "Not protected",
        screeningLabel: screening,
        detail: coverage ?? "Stay protection is not active.",
        badgeVariant: "secondary",
      };
    default:
      return {
        headline: "Unknown",
        screeningLabel: screening,
        detail: coverage,
        badgeVariant: "outline",
      };
  }
}

export function stayProtectionDamageClaimPath(dealId: string): string {
  return `/app/deals/${dealId}/billing?${STAY_PROTECTION_CLAIM_QUERY}=1`;
}

export function isStayProtectionClaimable(
  input: StayProtectionStatusInput | null | undefined,
): boolean {
  const screening = input?.screeningStatus?.trim();
  if (screening === "Approved" || screening === "Flagged") {
    return true;
  }
  if (
    screening === "Rejected"
    || screening === "Cancelled"
    || screening === "Failed"
  ) {
    return false;
  }
  return input?.state === "Active" || input?.state === "InstitutionBacked";
}
