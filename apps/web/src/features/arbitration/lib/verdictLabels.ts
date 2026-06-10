import type { DecisionOutcome, DecisionSeverity } from "@/api/types";

export { penaltyTypeLabels, penaltyRequiresAmount, PENALTY_TYPES } from "./penaltyTypes";

export const outcomeLabels: Record<DecisionOutcome, string> = {
  LandlordFavored: "In favor of host",
  TenantFavored: "In favor of guest",
  SharedFault: "Shared fault (both parties)",
  Dismissed: "Dismissed / no fault",
};

export const severityLabels: Record<DecisionSeverity, string> = {
  Low: "Low",
  Medium: "Medium",
  High: "High",
};

export function severityGuidance(severity: DecisionSeverity, outcome: DecisionOutcome): string {
  if (outcome === "Dismissed") {
    return "Dismissed cases typically need no penalties unless you add optional notes.";
  }
  switch (severity) {
    case "Low":
      return "Low severity: warnings or ledger marks are typical; monetary penalties are optional.";
    case "Medium":
      return "Medium severity: at least one penalty for the non-favored party (or both if shared fault)—monetary, deposit, fees, or account actions.";
    case "High":
      return "High severity: stronger monetary/restitution penalties; consider account restriction or platform ban for egregious cases.";
  }
}

export function defaultPenaltyParty(
  outcome: DecisionOutcome,
  landlordUserId: string,
  tenantUserId: string,
): string | null {
  switch (outcome) {
    case "LandlordFavored":
      return tenantUserId;
    case "TenantFavored":
      return landlordUserId;
    default:
      return null;
  }
}
