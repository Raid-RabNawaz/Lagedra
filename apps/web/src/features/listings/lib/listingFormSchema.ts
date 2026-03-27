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

export const listingFormSchema = z
  .object({
    propertyType: z.enum(propertyTypes),
    title: z.string().min(5, "Title must be at least 5 characters"),
    description: z.string().min(50, "Description must be at least 50 characters"),
    monthlyRentDollars: z.number().positive("Enter monthly rent"),
    maxDepositDollars: z.number().min(0, "Deposit cannot be negative"),
    insuranceRequired: z.boolean(),
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
  })
  .refine((d) => d.minStayDays <= d.maxStayDays, {
    message: "Min stay cannot exceed max stay",
    path: ["maxStayDays"],
  });

export type ListingFormValues = z.infer<typeof listingFormSchema>;

export const defaultListingFormValues: ListingFormValues = {
  propertyType: "Apartment",
  title: "",
  description: "",
  monthlyRentDollars: 1500,
  maxDepositDollars: 1500,
  insuranceRequired: false,
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
