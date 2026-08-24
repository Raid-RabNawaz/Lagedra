import type { CreateListingRequest, ListingAddedVia, UpdateListingRequest } from "@/api/types";
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

function dollarsToCentsOrNull(dollars: number | undefined): number | null {
  return dollars === undefined ? null : Math.round(dollars * 100);
}

function leaseTermsFromForm(v: ListingFormValues): UpdateListingRequest["leaseTerms"] {
  return {
    rentDueDayOfMonth: v.rentDueDayOfMonth,
    nsfFirstFeeCents: Math.round(v.nsfFirstFeeDollars * 100),
    nsfSubsequentFeeCents: Math.round(v.nsfSubsequentFeeDollars * 100),
    lateFeePercent: v.lateFeePercent,
    lateFeeGraceDays: v.lateFeeGraceDays,
    utilitiesResponsibility: v.utilitiesResponsibility?.trim() || null,
    yardMaintenanceByTenant: v.yardMaintenanceByTenant,
    furnished: v.furnished,
    includedAppliancesNotes: v.includedAppliancesNotes?.trim() || null,
    keyCount: v.keyCount,
    mailboxKeyCount: v.mailboxKeyCount,
    keyReplacementFeeCents: Math.round(v.keyReplacementFeeDollars * 100),
    lockoutFeeCents: Math.round(v.lockoutFeeDollars * 100),
    parkingSpaceCount: v.parkingSpaceCount,
    parkingDescription: v.parkingDescription?.trim() || null,
    parkingIncludedInRent: v.parkingIncludedInRent,
    maxGuestConsecutiveDays: v.maxGuestConsecutiveDays,
    rentersInsuranceMinLiabilityCents: Math.round(v.rentersInsuranceMinLiabilityDollars * 100),
    earlyTerminationFeeMonths: v.earlyTerminationFeeMonths,
    builtBefore1978: v.builtBefore1978,
    leadPaintKnowledge: v.leadPaintKnowledge?.trim() || null,
    rentCapJustCauseExempt: v.rentCapJustCauseExempt,
    paymentMethods: v.paymentMethods?.trim() || null,
  };
}

function tierDepositsFromForm(v: ListingFormValues) {
  return {
    depositUnverifiedCents: dollarsToCentsOrNull(v.depositUnverifiedDollars),
    depositBackgroundVerifiedCents: dollarsToCentsOrNull(
      v.depositBackgroundVerifiedDollars,
    ),
    depositPartnerGuaranteedCents: dollarsToCentsOrNull(
      v.depositPartnerGuaranteedDollars,
    ),
  };
}

export function toCreateListingRequest(
  v: ListingFormValues,
  source?: { addedVia?: ListingAddedVia; addedViaDetail?: string | null },
): CreateListingRequest {
  return {
    propertyType: v.propertyType,
    title: v.title.trim(),
    description: v.description.trim(),
    monthlyRentCents: Math.round(v.monthlyRentDollars * 100),
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
    defaultDepositCents:
      v.defaultDepositDollars === undefined
        ? null
        : Math.round(v.defaultDepositDollars * 100),
    ...tierDepositsFromForm(v),
    ...managementFromForm(v),
    addedVia: source?.addedVia ?? "Manual",
    addedViaDetail: source?.addedViaDetail?.trim() || null,
  };
}

export function toUpdateListingRequest(v: ListingFormValues): UpdateListingRequest {
  return {
    propertyType: v.propertyType,
    title: v.title.trim(),
    description: v.description.trim(),
    monthlyRentCents: Math.round(v.monthlyRentDollars * 100),
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
    defaultDepositCents:
      v.defaultDepositDollars === undefined
        ? null
        : Math.round(v.defaultDepositDollars * 100),
    clearDefaultDeposit: v.defaultDepositDollars === undefined,
    ...tierDepositsFromForm(v),
    leaseTerms: leaseTermsFromForm(v),
    ...managementFromForm(v),
  };
}

function managementFromForm(v: ListingFormValues) {
  const isManager = v.managerRole === "PropertyManager";
  return {
    managerRole: v.managerRole,
    homeOwnerUserId: isManager && v.homeOwnerUserId ? v.homeOwnerUserId : null,
    homeOwnerEmail:
      isManager && v.homeOwnerUserId && v.homeOwnerEmail?.trim()
        ? v.homeOwnerEmail.trim()
        : null,
    includeBrokerClause: v.includeBrokerClause,
  };
}
