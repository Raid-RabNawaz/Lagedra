import type { DealPhase } from "@/api/types";

/**
 * Phase 17 — single source of truth for the human-readable deal phase
 * heading shown anywhere in the UI (deal cards, deal detail header,
 * timeline, badges). Keeping this in one place stops the host and the
 * tenant from seeing slightly different vocabulary on the same screen.
 */
export function dealHeading(phase: DealPhase): string {
  switch (phase) {
    case "TruthSurface":
      return "Truth Surface";
    // The backend phase is still called "Checkout", but "checkout" reads as
    // the end of a stay. This step is really the deposit + first-month
    // payment that activates the booking, so we surface it as "Payment".
    case "Checkout":
      return "Payment";
    case "Active":
      return "Active";
    case "AwaitingDepositReturn":
      return "Deposit Returned";
    case "Closed":
      return "Completed";
    case "PaymentFailed":
      return "Payment failed";
    case "Cancelled":
      return "Cancelled";
    default:
      return phase;
  }
}

/**
 * Phase 17 — short label used in compact surfaces (badges, breadcrumbs).
 * Identical to {@link dealHeading} today; kept as a separate symbol so
 * future divergence (e.g. abbreviations) doesn't require a sweep.
 */
export const dealPhaseLabel = dealHeading;
