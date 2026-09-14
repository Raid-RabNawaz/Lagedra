import type { ScreeningOrderStatus } from "@/api/types";

export function isScreeningInFlight(status: ScreeningOrderStatus | string | null | undefined): boolean {
  return (
    status === "PendingPayment" ||
    status === "Paid" ||
    status === "WaitingForApplicant" ||
    status === "Pending"
  );
}

export function screeningStatusLabel(
  status: ScreeningOrderStatus | string | null | undefined,
  result?: string | null,
): string {
  switch (status) {
    case "PendingPayment":
      return "Payment needed";
    case "Paid":
      return "Starting check";
    case "WaitingForApplicant":
      return "Waiting for guest";
    case "Pending":
      return "Check in progress";
    case "Completed":
      if (result === "Review") return "Review";
      if (result === "Fail") return "Did not pass";
      return "Pass";
    case "Canceled":
      return "Canceled";
    case "Failed":
      return "Could not start";
    default:
      return "Not requested";
  }
}

export function screeningStatusDetail(
  status: ScreeningOrderStatus | string | null | undefined,
  result?: string | null,
  forHost = true,
  failureReason?: string | null,
): string | null {
  switch (status) {
    case "PendingPayment":
      return forHost
        ? "Finish paying to place this background and credit check."
        : "The host is finishing payment for this check.";
    case "Paid":
      return "The screening provider is creating this order.";
    case "WaitingForApplicant":
      return forHost
        ? "We emailed the guest a link. They must finish the provider form (name, date of birth, SSN) before a report is produced."
        : "Open the provider link to finish the form. Lagedra never collects your SSN.";
    case "Pending":
      return "The screening provider is running this report. It will appear here when it is ready.";
    case "Completed":
      if (result === "Review") {
        return "The report is ready and needs a closer look before you accept.";
      }
      if (result === "Fail") {
        return "The report is ready and did not clear.";
      }
      return "The report is ready. A Pass is one trust factor — it does not grant Elite on its own.";
    case "Canceled":
      return "This check was canceled. You can request a new one on a pending request.";
    case "Failed":
      return failureReason?.trim()
        || "The provider could not start this check. Confirm the listing street address and your profile name and email, then try again.";
    default:
      return null;
  }
}

export function screeningStatusClassName(
  status: ScreeningOrderStatus | string | null | undefined,
  result?: string | null,
): string {
  if (status === "Completed") {
    if (result === "Review") return "border-amber-200 bg-amber-50 text-amber-900";
    if (result === "Fail") return "border-red-200 bg-red-50 text-red-800";
    return "border-emerald-200 bg-emerald-50 text-emerald-800";
  }
  if (status === "Failed" || status === "Canceled") {
    return "border-red-200 bg-red-50 text-red-800";
  }
  if (isScreeningInFlight(status)) {
    return "border-sky-200 bg-sky-50 text-sky-800";
  }
  return "border-muted bg-muted/40 text-muted-foreground";
}
