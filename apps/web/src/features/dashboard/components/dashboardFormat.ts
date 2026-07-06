import type { BadgeProps } from "@/components/ui/badge";
import type {
  DealPhase,
  DealApplicationStatus,
  ListingStatus,
} from "@/api/types";

const dayRangeFmt = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "numeric",
});

export function formatDayRange(checkIn: string, checkOut: string): string {
  const a = new Date(checkIn);
  const b = new Date(checkOut);
  if (Number.isNaN(a.getTime()) || Number.isNaN(b.getTime())) return "—";
  return `${dayRangeFmt.format(a)} – ${dayRangeFmt.format(b)}, ${b.getFullYear()}`;
}

type BadgeMeta = { label: string; variant: BadgeProps["variant"] };

export function dealPhaseMeta(phase: DealPhase): BadgeMeta {
  switch (phase) {
    case "Active":
      return { label: "Active", variant: "success" };
    case "AwaitingDepositReturn":
      return { label: "Deposit return", variant: "accent" };
    case "Checkout":
      return { label: "Checkout", variant: "accent" };
    case "TruthSurface":
      return { label: "Agreement", variant: "default" };
    case "PaymentFailed":
      return { label: "Payment failed", variant: "destructive" };
    case "Cancelled":
      return { label: "Cancelled", variant: "outline" };
    case "Closed":
      return { label: "Closed", variant: "secondary" };
    default:
      return { label: phase, variant: "secondary" };
  }
}

export function appStatusMeta(status: DealApplicationStatus): BadgeMeta {
  switch (status) {
    case "Pending":
      return { label: "Pending", variant: "secondary" };
    case "Approved":
      return { label: "Approved", variant: "success" };
    case "Rejected":
      return { label: "Rejected", variant: "destructive" };
    case "PaymentFailed":
      return { label: "Payment failed", variant: "destructive" };
    case "Expired":
      return { label: "Expired", variant: "outline" };
    case "Cancelled":
      return { label: "Cancelled", variant: "outline" };
    default:
      return { label: status, variant: "secondary" };
  }
}

export function listingStatusMeta(status: ListingStatus): BadgeMeta {
  switch (status) {
    case "Published":
      return { label: "Published", variant: "success" };
    case "Activated":
      return { label: "Activated", variant: "accent" };
    case "InReview":
      return { label: "In review", variant: "default" };
    case "Draft":
      return { label: "Draft", variant: "secondary" };
    case "Denied":
      return { label: "Needs changes", variant: "destructive" };
    case "Closed":
      return { label: "Closed", variant: "outline" };
    default:
      return { label: status, variant: "secondary" };
  }
}
