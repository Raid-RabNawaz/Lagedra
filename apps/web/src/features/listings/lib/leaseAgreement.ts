import type { LeaseAgreementSource } from "@/api/types";

/** True unless the host explicitly opted this listing out of a lease. */
export function listingHasLeaseAgreement(
  source?: LeaseAgreementSource | null,
): boolean {
  return source !== "None";
}

export function leaseAgreementChoiceLabel(
  source?: LeaseAgreementSource | null,
): string {
  switch (source) {
    case "HostProvided":
      return "Your own lease agreement";
    case "None":
      return "No lease agreement";
    default:
      return "Lagedra / California lease";
  }
}
