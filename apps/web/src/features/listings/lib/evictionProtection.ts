import { formatMoney } from "@/utils/format";

export const EVICTION_PROTECTION_LABEL = "Eviction Protection";
export const EVICTION_PROTECTION_CAP_CENTS = 1_000_000;
export const EVICTION_PROTECTION_CLAIMS_EMAIL = "info@lagedra.com";

export const EVICTION_PROTECTION_TERMS = [
  "If the booking is cancelled before check-in, the Eviction Protection fee is refunded.",
  "If the tenant moves out early for a reason that is not a qualifying nonpayment or eviction event, coverage ends when the booking ends and no claim applies.",
  "If the early move-out is connected to documented nonpayment and meets the qualifying-event requirements, the claim may still be reviewed under the Eviction Protection terms.",
] as const;

export type EvictionProtectionClaimStanding = "EndedNoClaim" | "ClaimReviewable";

/** True when the stay is ending before the scheduled check-out date (UTC). */
export function isEarlyMoveOut(checkOut: string, now = new Date()): boolean {
  const scheduled = checkOut.slice(0, 10);
  const today = now.toISOString().slice(0, 10);
  return scheduled > today;
}

export function evictionProtectionAmountLabel(feeCents: number): string {
  return feeCents > 0 ? formatMoney(feeCents) : "Included";
}

export function evictionProtectionApplies(
  included: boolean | null | undefined,
  feeCents: number | null | undefined,
): boolean {
  return included === true || (feeCents ?? 0) > 0;
}
