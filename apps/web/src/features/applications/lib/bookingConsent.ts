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

function normalizeTier(tier: string | null | undefined): string {
  if (tier === "BackgroundVerified") return "Trusted";
  if (tier === "PartnerGuaranteed") return "Elite";
  return tier ?? "Unverified";
}

/** Human-readable label for a tenant verification tier. */
export function tierLabel(tier: string | null | undefined): string {
  switch (normalizeTier(tier)) {
    case "Elite":
      return "Elite";
    case "Trusted":
      return "Trusted";
    case "Verified":
      return "Verified";
    case "Blocklisted":
      return "Blocked";
    default:
      return "Unverified";
  }
}

/** Short label for compact badges. */
export function tierShortLabel(tier: string | null | undefined): string {
  switch (normalizeTier(tier)) {
    case "Elite":
      return "Elite";
    case "Trusted":
      return "Trusted";
    case "Verified":
      return "Verified";
    case "Blocklisted":
      return "Blocked";
    default:
      return "Unverified";
  }
}

export function tierDescription(
  tier: TenantVerificationTier | string | null | undefined,
): string {
  switch (normalizeTier(tier)) {
    case "Elite":
      return "Strong stay history plus a partner organization that vouches for this guest. Lowest predetermined deposit.";
    case "Trusted":
      return "Identity verified plus platform history and/or a recent background and credit check. Reduced deposit.";
    case "Verified":
      return "Government ID complete. Deposit follows the standard listing amount.";
    case "Blocklisted":
      return "This account cannot book on Lagedra.";
    default:
      return "Identity verification is not complete. Standard deposit applies.";
  }
}

export function tierBadgeClassName(
  tier: TenantVerificationTier | string | null | undefined,
): string {
  switch (normalizeTier(tier)) {
    case "Elite":
      return "border-violet-200 bg-violet-50 text-violet-800";
    case "Trusted":
      return "border-emerald-200 bg-emerald-50 text-emerald-800";
    case "Verified":
      return "border-sky-200 bg-sky-50 text-sky-800";
    case "Blocklisted":
      return "border-red-200 bg-red-50 text-red-800";
    default:
      return "border-amber-200 bg-amber-50 text-amber-900";
  }
}

export function formatScreenedOn(iso: string | null | undefined): string | null {
  if (!iso) return null;
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return null;
  return d.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}
