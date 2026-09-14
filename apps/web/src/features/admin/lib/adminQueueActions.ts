import type { ViolationStatus } from "@/api/types";

export function violationActions(status: ViolationStatus) {
  const open = status === "Open" || status === "UnderReview";
  const closed = status === "Resolved" || status === "Dismissed";
  return {
    canResolve: !closed,
    canDismiss: !closed,
    canEscalate: open,
  };
}
