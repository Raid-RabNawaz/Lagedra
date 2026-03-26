import type { CreateListingRequest, UpdateListingRequest } from "@/api/types";
import type { ListingFormValues } from "./listingFormSchema";
import { timeToApi } from "./listingFormSchema";

function houseRulesFromForm(v: ListingFormValues): CreateListingRequest["houseRules"] {
  return {
    checkInTime: timeToApi(v.checkInTime),
    checkOutTime: timeToApi(v.checkOutTime),
    maxGuests: v.maxGuests,
    petsAllowed: v.petsAllowed,
    petsNotes: v.petsNotes?.trim() || null,
    smokingAllowed: v.smokingAllowed,
    partiesAllowed: v.partiesAllowed,
    quietHoursStart: v.quietHoursStart?.trim() ? timeToApi(v.quietHoursStart) : null,
    quietHoursEnd: v.quietHoursEnd?.trim() ? timeToApi(v.quietHoursEnd) : null,
    leavingInstructions: v.leavingInstructions?.trim() || null,
    additionalRules: v.additionalRules?.trim() || null,
  };
}

function cancellationFromForm(v: ListingFormValues): CreateListingRequest["cancellationPolicy"] {
  return {
    type: v.cancellationType,
    freeCancellationDays: v.freeCancellationDays,
    partialRefundPercent: v.partialRefundPercent ?? null,
    partialRefundDays: v.partialRefundDays ?? null,
    customTerms: v.customTerms?.trim() || null,
  };
}

export function toCreateListingRequest(
  v: ListingFormValues,
  landlordUserId: string,
): CreateListingRequest {
  return {
    landlordUserId,
    propertyType: v.propertyType,
    title: v.title.trim(),
    description: v.description.trim(),
    monthlyRentCents: Math.round(v.monthlyRentDollars * 100),
    insuranceRequired: v.insuranceRequired,
    bedrooms: v.bedrooms,
    bathrooms: v.bathrooms,
    minStayDays: v.minStayDays,
    maxStayDays: v.maxStayDays,
    maxDepositCents: Math.round(v.maxDepositDollars * 100),
    squareFootage: v.squareFootage ?? null,
    houseRules: houseRulesFromForm(v),
    cancellationPolicy: cancellationFromForm(v),
    amenityIds: v.amenityIds.length ? v.amenityIds : null,
    safetyDeviceIds: v.safetyDeviceIds.length ? v.safetyDeviceIds : null,
    considerationIds: v.considerationIds.length ? v.considerationIds : null,
    instantBookingEnabled: v.instantBookingEnabled,
    virtualTourUrl: v.virtualTourUrl?.trim() || null,
  };
}

export function toUpdateListingRequest(v: ListingFormValues): UpdateListingRequest {
  return {
    propertyType: v.propertyType,
    title: v.title.trim(),
    description: v.description.trim(),
    monthlyRentCents: Math.round(v.monthlyRentDollars * 100),
    insuranceRequired: v.insuranceRequired,
    bedrooms: v.bedrooms,
    bathrooms: v.bathrooms,
    minStayDays: v.minStayDays,
    maxStayDays: v.maxStayDays,
    maxDepositCents: Math.round(v.maxDepositDollars * 100),
    squareFootage: v.squareFootage ?? null,
    houseRules: houseRulesFromForm(v),
    cancellationPolicy: cancellationFromForm(v),
    amenityIds: v.amenityIds.length ? v.amenityIds : null,
    safetyDeviceIds: v.safetyDeviceIds.length ? v.safetyDeviceIds : null,
    considerationIds: v.considerationIds.length ? v.considerationIds : null,
    instantBookingEnabled: v.instantBookingEnabled,
    virtualTourUrl: v.virtualTourUrl?.trim() || null,
  };
}
