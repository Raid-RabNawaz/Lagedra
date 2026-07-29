import { z } from "zod";
import type { CancellationPolicyType, PropertyType } from "@/api/types";

const propertyTypes = [
  "Apartment",
  "House",
  "Condo",
  "Townhouse",
  "Studio",
  "Loft",
  "Villa",
  "Cottage",
  "Cabin",
  "Other",
] as const satisfies readonly PropertyType[];

const cancellationTypes = [
  "Flexible",
  "Moderate",
  "Strict",
  "NonRefundable",
  "Custom",
] as const satisfies readonly CancellationPolicyType[];

const optionalInt = z.preprocess(
  (v) => {
    if (v === "" || v === undefined || v === null) return undefined;
    const n = Number(v);
    return Number.isNaN(n) ? undefined : n;
  },
  z.number().int().min(0).optional(),
);

const optionalDepositDollars = z.preprocess(
  (v) => {
    if (v === "" || v === undefined || v === null) return undefined;
    const n = Number(v);
    return Number.isNaN(n) ? undefined : n;
  },
  z.number().min(0, "Deposit cannot be negative").optional(),
);

export const listingFormSchema = z
  .object({
    propertyType: z.enum(propertyTypes),
    title: z.string().min(5, "Title must be at least 5 characters"),
    description: z.string().min(50, "Description must be at least 50 characters"),
    monthlyRentDollars: z.number().positive("Enter monthly rent"),
    maxDepositDollars: z.number().min(0, "Deposit cannot be negative"),
    defaultDepositDollars: z.preprocess(
      (v) => {
        if (v === "" || v === undefined || v === null) return undefined;
        const n = Number(v);
        return Number.isNaN(n) ? undefined : n;
      },
      z.number().min(0, "Default deposit cannot be negative").optional(),
    ),
    depositUnverifiedDollars: optionalDepositDollars,
    depositBackgroundVerifiedDollars: optionalDepositDollars,
    depositPartnerGuaranteedDollars: optionalDepositDollars,
    bedrooms: z.number().int().min(0),
    bathrooms: z.number().min(0.5),
    minStayDays: z.number().int().min(30).max(180),
    maxStayDays: z.number().int().min(30).max(180),
    squareFootage: optionalInt,
    instantBookingEnabled: z.boolean(),
    virtualTourUrl: z.string().optional().nullable(),
    amenityIds: z.array(z.string()),
    safetyDeviceIds: z.array(z.string()),
    considerationIds: z.array(z.string()),
    checkInTime: z.string().min(1),
    checkOutTime: z.string().min(1),
    maxGuests: z.number().int().min(1),
    petsAllowed: z.boolean(),
    petsNotes: z.string().optional().nullable(),
    smokingAllowed: z.boolean(),
    partiesAllowed: z.boolean(),
    quietHoursStart: z.string().optional().nullable(),
    quietHoursEnd: z.string().optional().nullable(),
    leavingInstructions: z.string().optional().nullable(),
    additionalRules: z.string().optional().nullable(),
    cancellationType: z.enum(cancellationTypes),
    freeCancellationDays: z.number().int().min(0),
    partialRefundPercent: optionalInt,
    partialRefundDays: optionalInt,
    customTerms: z.string().optional().nullable(),
    // Lease terms (edit form only) — merged into the generated lease agreement.
    rentDueDayOfMonth: z.number().int().min(1, "Must be 1–28").max(28, "Must be 1–28"),
    paymentMethods: z.string().optional().nullable(),
    nsfFirstFeeDollars: z.number().min(0),
    nsfSubsequentFeeDollars: z.number().min(0),
    lateFeePercent: z.number().min(0).max(100),
    lateFeeGraceDays: z.number().int().min(0),
    utilitiesResponsibility: z.string().optional().nullable(),
    yardMaintenanceByTenant: z.boolean(),
    furnished: z.boolean(),
    includedAppliancesNotes: z.string().optional().nullable(),
    keyCount: z.number().int().min(0),
    mailboxKeyCount: z.number().int().min(0),
    keyReplacementFeeDollars: z.number().min(0),
    lockoutFeeDollars: z.number().min(0),
    parkingSpaceCount: z.number().int().min(0),
    parkingDescription: z.string().optional().nullable(),
    parkingIncludedInRent: z.boolean(),
    maxGuestConsecutiveDays: z.number().int().min(0),
    rentersInsuranceMinLiabilityDollars: z.number().min(0),
    earlyTerminationFeeMonths: z.number().int().min(0),
    builtBefore1978: z.boolean(),
    leadPaintKnowledge: z.string().optional().nullable(),
    rentCapJustCauseExempt: z.boolean(),
  })
  .refine((d) => d.minStayDays <= d.maxStayDays, {
    message: "Min stay cannot exceed max stay",
    path: ["maxStayDays"],
  })
  .refine(
    (d) =>
      d.defaultDepositDollars === undefined
      || d.defaultDepositDollars <= d.maxDepositDollars,
    {
      message: "Default deposit cannot exceed maximum deposit",
      path: ["defaultDepositDollars"],
    },
  )
  .refine(
    (d) =>
      d.depositUnverifiedDollars === undefined
      || d.depositUnverifiedDollars <= d.maxDepositDollars,
    {
      message: "Unverified deposit cannot exceed maximum deposit",
      path: ["depositUnverifiedDollars"],
    },
  )
  .refine(
    (d) =>
      d.depositBackgroundVerifiedDollars === undefined
      || d.depositBackgroundVerifiedDollars <= d.maxDepositDollars,
    {
      message: "Verified deposit cannot exceed maximum deposit",
      path: ["depositBackgroundVerifiedDollars"],
    },
  )
  .refine(
    (d) =>
      d.depositPartnerGuaranteedDollars === undefined
      || d.depositPartnerGuaranteedDollars <= d.maxDepositDollars,
    {
      message: "Partner-guaranteed deposit cannot exceed maximum deposit",
      path: ["depositPartnerGuaranteedDollars"],
    },
  )
  .refine(
    (d) =>
      d.depositBackgroundVerifiedDollars === undefined
      || d.depositUnverifiedDollars === undefined
      || d.depositBackgroundVerifiedDollars <= d.depositUnverifiedDollars,
    {
      message: "Verified deposit should not exceed the unverified deposit",
      path: ["depositBackgroundVerifiedDollars"],
    },
  )
  .refine(
    (d) =>
      d.depositPartnerGuaranteedDollars === undefined
      || d.depositBackgroundVerifiedDollars === undefined
      || d.depositPartnerGuaranteedDollars <= d.depositBackgroundVerifiedDollars,
    {
      message: "Partner-guaranteed deposit should not exceed the verified deposit",
      path: ["depositPartnerGuaranteedDollars"],
    },
  )
  .refine(
    (d) =>
      d.depositPartnerGuaranteedDollars === undefined
      || d.depositUnverifiedDollars === undefined
      || d.depositPartnerGuaranteedDollars <= d.depositUnverifiedDollars,
    {
      message: "Partner-guaranteed deposit should not exceed the unverified deposit",
      path: ["depositPartnerGuaranteedDollars"],
    },
  );

export type ListingFormValues = z.infer<typeof listingFormSchema>;

export const defaultListingFormValues: ListingFormValues = {
  propertyType: "Apartment",
  title: "",
  description: "",
  monthlyRentDollars: 1500,
  maxDepositDollars: 1500,
  defaultDepositDollars: undefined,
  depositUnverifiedDollars: undefined,
  depositBackgroundVerifiedDollars: undefined,
  depositPartnerGuaranteedDollars: undefined,
  bedrooms: 1,
  bathrooms: 1,
  minStayDays: 30,
  maxStayDays: 180,
  squareFootage: undefined,
  instantBookingEnabled: false,
  virtualTourUrl: "",
  amenityIds: [],
  safetyDeviceIds: [],
  considerationIds: [],
  checkInTime: "15:00",
  checkOutTime: "11:00",
  maxGuests: 2,
  petsAllowed: false,
  petsNotes: "",
  smokingAllowed: false,
  partiesAllowed: false,
  quietHoursStart: "",
  quietHoursEnd: "",
  leavingInstructions: "",
  additionalRules: "",
  cancellationType: "Moderate",
  freeCancellationDays: 14,
  partialRefundPercent: undefined,
  partialRefundDays: undefined,
  customTerms: "",
  rentDueDayOfMonth: 1,
  paymentMethods: "",
  nsfFirstFeeDollars: 25,
  nsfSubsequentFeeDollars: 35,
  lateFeePercent: 5,
  lateFeeGraceDays: 3,
  utilitiesResponsibility: "",
  yardMaintenanceByTenant: false,
  furnished: false,
  includedAppliancesNotes: "",
  keyCount: 1,
  mailboxKeyCount: 0,
  keyReplacementFeeDollars: 200,
  lockoutFeeDollars: 200,
  parkingSpaceCount: 0,
  parkingDescription: "",
  parkingIncludedInRent: true,
  maxGuestConsecutiveDays: 7,
  rentersInsuranceMinLiabilityDollars: 100000,
  earlyTerminationFeeMonths: 2,
  builtBefore1978: false,
  leadPaintKnowledge: "",
  rentCapJustCauseExempt: false,
};

export function timeToApi(t: string): string {
  const s = t.trim();
  if (!s) return "00:00:00";
  if (s.length === 5 && s.includes(":")) return `${s}:00`;
  return s;
}

export function apiTimeToInput(t: string | undefined | null): string {
  if (!t) return "12:00";
  const parts = t.split(":");
  return `${parts[0] ?? "12"}:${(parts[1] ?? "00").slice(0, 2)}`;
}
