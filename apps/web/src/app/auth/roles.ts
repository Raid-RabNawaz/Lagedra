export const roles = {
  tenant: "Tenant",
  landlord: "Landlord",
  arbitrator: "Arbitrator",
  platformAdmin: "PlatformAdmin",
  insurancePartner: "InsurancePartner",
  institutionPartner: "InstitutionPartner",
} as const;

export type UserRole = (typeof roles)[keyof typeof roles];

export const selfRegisterRoles: UserRole[] = [
  roles.tenant,
  roles.landlord,
  roles.institutionPartner,
];

export const allRoles: UserRole[] = Object.values(roles);

export function roleLabel(role: UserRole | string | number): string {
  const labels: Record<string, string> = {
    Tenant: "Tenant",
    Landlord: "Landlord",
    Arbitrator: "Arbitrator",
    PlatformAdmin: "Platform Admin",
    InsurancePartner: "Insurance Partner",
    InstitutionPartner: "Institution Partner",
  };
  return labels[String(role)] ?? String(role);
}
