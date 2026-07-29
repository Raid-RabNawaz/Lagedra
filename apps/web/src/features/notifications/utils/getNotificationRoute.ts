import type { InAppNotificationDto } from "@/api/types";

/**
 * Resolves an in-app notification to a deep-link route.
 *
 * Backend stores RelatedEntityId/Type only (payload dict is email/SMS-only).
 * Truth-surface notifications store the **deal** id under type "Deal" — never
 * route them to `/app/truth-surface/:snapshotId` (that page expects a snapshot id).
 */
export function getNotificationRoute(n: InAppNotificationDto): string | null {
  const id = n.relatedEntityId;
  if (!id) {
    // Account-level notices with no entity — send users somewhere useful.
    if (n.category.startsWith("identity") || n.category.startsWith("verification")) {
      return "/app/verification";
    }
    if (n.category === "welcome" || n.category === "role_changed") {
      return "/app";
    }
    if (n.category === "account_restricted") {
      return "/app/profile";
    }
    return null;
  }

  const category = n.category;

  switch (n.relatedEntityType) {
    case "Deal":
      if (category.startsWith("truth_surface")) {
        return `/app/deals/${id}/truth-surface`;
      }
      if (
        category.startsWith("payment") ||
        category === "deal_activated" ||
        category.startsWith("damage_claim") ||
        category === "billing_stopped" ||
        category.startsWith("deposit_")
      ) {
        return `/app/deals/${id}/billing`;
      }
      if (category.startsWith("compliance") || category.startsWith("insurance")) {
        return `/app/deals/${id}/compliance`;
      }
      // application_approved, review_due/reminder, booking_cancelled (tenant), default
      return `/app/deals/${id}`;

    case "Listing":
      if (
        category === "application_submitted" ||
        category === "application_received" ||
        category === "application_rejected" ||
        category === "application_expired" ||
        category === "application_superseded"
      ) {
        return "/app/applications";
      }
      // listing_submitted_for_review / published / denied — host owns the listing
      if (category.startsWith("listing_")) {
        return `/app/listings/${id}`;
      }
      // booking_cancelled for landlord, etc.
      return `/app/listings/${id}`;

    case "InquirySession":
      return `/app/inquiry/${id}`;

    case "ArbitrationCase":
      return `/app/arbitration/${id}`;

    case "Violation":
      // Prefer deal-linked compliance once backend stores Deal id; legacy rows
      // with a bare violation id have no dedicated page.
      return null;

    case "BillingAccount":
      return "/app";

    case "TruthSurfaceSnapshot":
      return `/app/truth-surface/${id}`;

    default:
      return null;
  }
}
