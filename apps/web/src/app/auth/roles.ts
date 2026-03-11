export const roles = {
  tenant: "Tenant",
  landlord: "Landlord",
  arbitrator: "Arbitrator",
  platformAdmin: "PlatformAdmin",
  insurancePartner: "InsurancePartner",
  institutionPartner: "InstitutionPartner",
} as const;

export type UserRole = (typeof roles)[keyof typeof roles];

export const selfRegisterRoles: UserRole[] = [roles.tenant, roles.landlord];

