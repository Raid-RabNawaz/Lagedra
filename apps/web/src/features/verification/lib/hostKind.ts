import type { HostKind } from "@/api/types";

export function isPropertyManagementCompany(kind: HostKind | null | undefined): boolean {
  return kind === "PropertyManagementCompany";
}

/** Label for the identity check. Companies are verified as a business, not by a personal ID. */
export function identityCheckLabel(
  kind: HostKind | null | undefined,
  variant: "row" | "short" | "badge" | "listing" = "row",
): string {
  const company = isPropertyManagementCompany(kind);
  switch (variant) {
    case "short":
      return company ? "Business" : "ID";
    case "badge":
      return company ? "Business verified" : "ID verified";
    case "listing":
      return company ? "Business verified" : "Identity verified";
    default:
      return company ? "Business" : "Government ID";
  }
}
