import type { PenaltyType } from "@/api/types";

/** Keep in sync with `PenaltyTypeRules` in Arbitration module. */
export const PENALTY_TYPES: PenaltyType[] = [
  "Monetary",
  "DepositWithhold",
  "RentCredit",
  "LateFee",
  "DamageRestitution",
  "InsuranceRecovery",
  "CleaningFee",
  "UtilitiesRecovery",
  "ProtocolFee",
  "TrustLedgerMark",
  "AccountWarning",
  "AccountRestriction",
  "PlatformBan",
  "CorrectiveAction",
  "LeaseTermination",
  "Custom",
];

export const penaltyTypesRequiringAmount: PenaltyType[] = [
  "Monetary",
  "DepositWithhold",
  "ProtocolFee",
  "RentCredit",
  "LateFee",
  "DamageRestitution",
  "InsuranceRecovery",
  "CleaningFee",
  "UtilitiesRecovery",
];

export const penaltyTypeLabels: Record<PenaltyType, string> = {
  Monetary: "Monetary payment",
  DepositWithhold: "Deposit withhold",
  TrustLedgerMark: "Trust ledger mark",
  AccountWarning: "Account warning",
  ProtocolFee: "Protocol fee",
  RentCredit: "Rent credit (owed to party)",
  LateFee: "Late fee",
  DamageRestitution: "Damage restitution",
  InsuranceRecovery: "Insurance recovery",
  AccountRestriction: "Account restriction",
  PlatformBan: "Platform ban",
  CorrectiveAction: "Mandatory corrective action",
  LeaseTermination: "Lease termination notice",
  CleaningFee: "Cleaning / turnover fee",
  UtilitiesRecovery: "Utilities recovery",
  Custom: "Custom (describe in notes)",
};

export function penaltyRequiresAmount(type: PenaltyType): boolean {
  return penaltyTypesRequiringAmount.includes(type);
}
