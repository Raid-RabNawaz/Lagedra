/**
 * Hosts see Lagedra's lease by that name. Tenants see the same document as
 * the jurisdiction's standard residential lease — never as a Lagedra brand.
 */

const US_STATE_NAMES: Record<string, string> = {
  AL: "Alabama",
  AK: "Alaska",
  AZ: "Arizona",
  AR: "Arkansas",
  CA: "California",
  CO: "Colorado",
  CT: "Connecticut",
  DE: "Delaware",
  DC: "District of Columbia",
  FL: "Florida",
  GA: "Georgia",
  HI: "Hawaii",
  ID: "Idaho",
  IL: "Illinois",
  IN: "Indiana",
  IA: "Iowa",
  KS: "Kansas",
  KY: "Kentucky",
  LA: "Louisiana",
  ME: "Maine",
  MD: "Maryland",
  MA: "Massachusetts",
  MI: "Michigan",
  MN: "Minnesota",
  MS: "Mississippi",
  MO: "Missouri",
  MT: "Montana",
  NE: "Nebraska",
  NV: "Nevada",
  NH: "New Hampshire",
  NJ: "New Jersey",
  NM: "New Mexico",
  NY: "New York",
  NC: "North Carolina",
  ND: "North Dakota",
  OH: "Ohio",
  OK: "Oklahoma",
  OR: "Oregon",
  PA: "Pennsylvania",
  RI: "Rhode Island",
  SC: "South Carolina",
  SD: "South Dakota",
  TN: "Tennessee",
  TX: "Texas",
  UT: "Utah",
  VT: "Vermont",
  VA: "Virginia",
  WA: "Washington",
  WV: "West Virginia",
  WI: "Wisconsin",
  WY: "Wyoming",
};

/** Matches the lease filler's fallback when a listing has no jurisdiction. */
export const DEFAULT_LEASE_JURISDICTION_NAME = "California";

export function resolveLeaseJurisdictionName(
  jurisdictionCode?: string | null,
): string {
  const parts = jurisdictionCode?.trim().toUpperCase().split("-") ?? [];
  if (parts[0] === "US" && parts[1] && US_STATE_NAMES[parts[1]]) {
    return US_STATE_NAMES[parts[1]];
  }
  return DEFAULT_LEASE_JURISDICTION_NAME;
}

function possessive(name: string): string {
  return /s$/i.test(name) ? `${name}'` : `${name}'s`;
}

/** Tenant-facing title for Lagedra's jurisdiction template. */
export function tenantStandardLeaseTitle(
  jurisdictionCode?: string | null,
): string {
  return `${possessive(resolveLeaseJurisdictionName(jurisdictionCode))} standard lease agreement`;
}
