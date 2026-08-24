import type { TenantVerificationTier } from "@/api/types";

/**
 * Mirrors the backend `BookingConsent.CurrentVersion`. Submitted with the
 * tenant's Truth Surface consent at request time and the host's at approval
 * time so the recorded consent is provably tied to the exact agreement
 * wording shown. Bump in lock-step with the backend constant.
 */
export const BOOKING_CONSENT_VERSION = "ts-consent-v3";

/** Mirrors backend `OwnerTenancyConsent.CurrentVersion`. */
export const OWNER_TENANCY_CONSENT_VERSION = "owner-tenancy-consent-v1";

export function isAwaitingOwnerConsent(application: {
  status: string;
  ownerConsentRequired?: boolean;
  ownerConsentGiven?: boolean;
  ownerConsentDeclined?: boolean;
}): boolean {
  return (
    application.status === "Pending" &&
    application.ownerConsentRequired === true &&
    application.ownerConsentGiven !== true &&
    application.ownerConsentDeclined !== true
  );
}

/** Human-readable label for a tenant verification tier. */
export function tierLabel(tier: string | null | undefined): string {
  switch (tier) {
    case "PartnerGuaranteed":
      return "Partner-guaranteed";
    case "BackgroundVerified":
      return "Verified";
    case "Unverified":
      return "Standard";
    default:
      return "Standard";
  }
}

/** Short label for compact badges. */
export function tierShortLabel(tier: string | null | undefined): string {
  switch (tier) {
    case "PartnerGuaranteed":
      return "Partner trust";
    case "BackgroundVerified":
      return "Verified trust";
    case "Unverified":
      return "Standard trust";
    default:
      return "Standard trust";
  }
}

export function tierDescription(
  tier: TenantVerificationTier | string | null | undefined,
): string {
  switch (tier) {
    case "PartnerGuaranteed":
      return "A partner organization vouches for this guest. Lowest predetermined deposit.";
    case "BackgroundVerified":
      return "Government ID and background check completed. Reduced deposit tier.";
    case "Unverified":
      return "Standard platform verification. Deposit follows your listing's unverified tier.";
    default:
      return "Standard platform verification applies to this request.";
  }
}

export function tierBadgeClassName(
  tier: TenantVerificationTier | string | null | undefined,
): string {
  switch (tier) {
    case "PartnerGuaranteed":
      return "border-violet-200 bg-violet-50 text-violet-800";
    case "BackgroundVerified":
      return "border-emerald-200 bg-emerald-50 text-emerald-800";
    case "Unverified":
      return "border-amber-200 bg-amber-50 text-amber-900";
    default:
      return "border-muted bg-muted/50 text-muted-foreground";
  }
}
