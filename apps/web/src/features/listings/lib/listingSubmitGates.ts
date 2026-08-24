import type { ListingStatus } from "@/api/types";

/**
 * Mirrors backend `SubmitListingForReviewCommandHandler.RequirePayoutSetupToSubmitForReview`.
 * Temporarily off so hosts can send listings for review before Stripe Connect
 * is complete. Flip back to `true` (and the matching C# constant) to restore
 * the gate. Accepting a booking still requires payouts.
 */
export const REQUIRE_PAYOUT_SETUP_TO_SUBMIT_FOR_REVIEW = false;

/** First submission or resubmit after an admin denial. */
export function canSubmitListingForReview(status: ListingStatus): boolean {
  return status === "Draft" || status === "Denied";
}

/**
 * Mirrors `Listing.EnsureEditable`: draft, denied, and live listings.
 * InReview and Closed stay read-only.
 */
export function canEditListingDetails(status: ListingStatus): boolean {
  return (
    status === "Draft" ||
    status === "Denied" ||
    status === "Published" ||
    status === "Activated"
  );
}
