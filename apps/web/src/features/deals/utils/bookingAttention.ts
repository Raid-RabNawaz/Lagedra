import type { DealPhase, DealSummaryDto, ListingStatus } from "@/api/types";

/** Active bookings within this many days of checkout get an ending-soon highlight. */
export const NEAR_END_DAYS = 15;

export type BookingIssueKind = "PaymentFailed" | "AwaitingDepositReturn";

export type BookingIssue = {
  kind: BookingIssueKind;
  /** Short badge / title label */
  title: string;
  /** What went wrong */
  problem: string;
  /** How it gets resolved */
  resolution: string;
  /** Primary CTA label */
  ctaLabel: string;
  /** Deep link relative to app */
  href: string;
};

export type EndingSoonInfo = {
  daysRemaining: number;
  label: string;
};

/** Calendar days from today (local) until the checkout date. Negative = already past. */
export function daysUntilDate(dateStr: string, now = new Date()): number {
  const end = new Date(dateStr);
  if (Number.isNaN(end.getTime())) return Number.POSITIVE_INFINITY;
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const startOfEnd = new Date(end.getFullYear(), end.getMonth(), end.getDate());
  return Math.round(
    (startOfEnd.getTime() - startOfToday.getTime()) / (1000 * 60 * 60 * 24),
  );
}

export function getEndingSoon(
  deal: Pick<DealSummaryDto, "dealPhase" | "requestedCheckOut">,
  now = new Date(),
): EndingSoonInfo | null {
  if (deal.dealPhase !== "Active") return null;
  const days = daysUntilDate(deal.requestedCheckOut, now);
  if (days < 0 || days > NEAR_END_DAYS) return null;
  return {
    daysRemaining: days,
    label:
      days === 0
        ? "Ends today"
        : days === 1
          ? "1 day remaining"
          : `${days} days remaining`,
  };
}

export function getDealIssue(
  deal: Pick<
    DealSummaryDto,
    | "dealId"
    | "dealPhase"
    | "listingTitle"
    | "hostConfirmedDepositReturnedAt"
    | "tenantConfirmedDepositReceivedAt"
    | "depositReturnSettledAt"
  >,
  perspective: "host" | "guest",
): BookingIssue | null {
  const title = deal.listingTitle?.trim() || "Booking";

  if (deal.dealPhase === "PaymentFailed") {
    return {
      kind: "PaymentFailed",
      title: `Payment failed — ${title}`,
      problem:
        perspective === "guest"
          ? "Your deposit payment didn’t go through. The booking stays on hold until payment succeeds."
          : "The guest’s deposit payment failed. The booking is at risk until this is resolved.",
      resolution:
        perspective === "guest"
          ? "Update your card and retry payment on the booking page. You’ll get a confirmation once it clears."
          : "Ask the guest to update their card and retry. You can also open the booking to message them and track status.",
      ctaLabel: perspective === "guest" ? "Resolve payment" : "Resolve",
      href: `/app/deals/${deal.dealId}`,
    };
  }

  if (deal.dealPhase === "AwaitingDepositReturn") {
    // Handshake finished — nothing left to flag.
    if (deal.depositReturnSettledAt) {
      return null;
    }
    // Guest already confirmed receipt.
    if (perspective === "guest" && deal.tenantConfirmedDepositReceivedAt) {
      return null;
    }
    // Host already recorded the return — show waiting-on-guest, not "return deposit".
    if (perspective === "host" && deal.hostConfirmedDepositReturnedAt) {
      if (deal.tenantConfirmedDepositReceivedAt) {
        return null;
      }
      return {
        kind: "AwaitingDepositReturn",
        title: `Waiting on guest — ${title}`,
        problem:
          "You reported returning the deposit. The booking closes once the guest confirms they received it.",
        resolution:
          "Ask the guest to open the booking and tap “I received my deposit,” or wait for their confirmation.",
        ctaLabel: "View booking",
        href: `/app/deals/${deal.dealId}`,
      };
    }

    return {
      kind: "AwaitingDepositReturn",
      title: `Deposit return needed — ${title}`,
      problem:
        perspective === "guest"
          ? "Your stay has ended. Confirm once you’ve received your deposit back."
          : "This stay has ended. Return the deposit and confirm so the booking can close.",
      resolution:
        perspective === "guest"
          ? "When the host returns your deposit, confirm receipt on the booking page."
          : "Return the deposit to the guest, then confirm the return on the booking page.",
      ctaLabel: perspective === "guest" ? "Confirm deposit" : "Return deposit",
      href: `/app/deals/${deal.dealId}`,
    };
  }

  return null;
}

/** Critical issues that should use the red highlight (payment / listing risk). */
export function isCriticalDealIssue(kind: BookingIssueKind): boolean {
  return kind === "PaymentFailed";
}

export function dealNeedsAttention(deal: DealSummaryDto, now = new Date()): boolean {
  return Boolean(getDealIssue(deal, "host") || getEndingSoon(deal, now));
}

export function sortDealsByAttention(
  deals: DealSummaryDto[],
  now = new Date(),
): DealSummaryDto[] {
  return [...deals].sort((a, b) => {
    const aFail = a.dealPhase === "PaymentFailed" ? 0 : 1;
    const bFail = b.dealPhase === "PaymentFailed" ? 0 : 1;
    if (aFail !== bFail) return aFail - bFail;

    const aEnd = getEndingSoon(a, now)?.daysRemaining ?? 999;
    const bEnd = getEndingSoon(b, now)?.daysRemaining ?? 999;
    if (aEnd !== bEnd) return aEnd - bEnd;

    return (
      new Date(a.requestedCheckIn).getTime() -
      new Date(b.requestedCheckIn).getTime()
    );
  });
}

export type ListingIssue = {
  listingId: string;
  title: string;
  problem: string;
  resolution: string;
  href: string;
};

export function getListingIssue(listing: {
  id: string;
  title: string;
  status: ListingStatus;
}): ListingIssue | null {
  if (listing.status !== "Denied") return null;
  return {
    listingId: listing.id,
    title: `Listing needs changes — ${listing.title}`,
    problem:
      "An admin couldn’t approve this listing. It won’t appear in search until you fix the feedback and resubmit.",
    resolution:
      "Open the listing, address the review notes, then submit it for review again.",
    href: `/app/listings/${listing.id}/edit`,
  };
}

export function isIssuePhase(phase: DealPhase): boolean {
  return phase === "PaymentFailed" || phase === "AwaitingDepositReturn";
}
