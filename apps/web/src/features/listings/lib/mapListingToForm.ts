import type { ListingDetailsDto } from "@/api/types";
import type { ListingFormValues } from "./listingFormSchema";
import { apiTimeToInput } from "./listingFormSchema";

export function listingDetailsToFormValues(listing: ListingDetailsDto): ListingFormValues {
  const hr = listing.houseRules;
  const cp = listing.cancellationPolicy;
  const lt = listing.leaseTerms;

  return {
    propertyType: listing.propertyType,
    title: listing.title,
    description: listing.description,
    monthlyRentDollars: listing.monthlyRentCents / 100,
    maxDepositDollars: listing.maxDepositCents / 100,
    defaultDepositDollars:
      listing.defaultDepositCents != null
        ? listing.defaultDepositCents / 100
        : undefined,
    depositUnverifiedDollars:
      listing.depositUnverifiedCents != null
        ? listing.depositUnverifiedCents / 100
        : undefined,
    depositBackgroundVerifiedDollars:
      listing.depositBackgroundVerifiedCents != null
        ? listing.depositBackgroundVerifiedCents / 100
        : undefined,
    depositPartnerGuaranteedDollars:
      listing.depositPartnerGuaranteedCents != null
        ? listing.depositPartnerGuaranteedCents / 100
        : undefined,
    bedrooms: listing.bedrooms,
    bathrooms: Number(listing.bathrooms),
    minStayDays: listing.minStayDays ?? 30,
    maxStayDays: listing.maxStayDays ?? 180,
    squareFootage: listing.squareFootage ?? undefined,
    instantBookingEnabled: listing.instantBookingEnabled,
    virtualTourUrl: listing.virtualTourUrl ?? "",
    managerRole: listing.managerRole ?? "Owner",
    homeOwnerUserId: listing.homeOwnerUserId ?? listing.homeOwner?.userId ?? "",
    homeOwnerEmail: listing.homeOwner?.email ?? "",
    homeOwnerDisplayName: listing.homeOwner?.displayName ?? "",
    includeBrokerClause: listing.includeBrokerClause ?? false,
    amenityIds: listing.amenities.map((a) => a.id),
    safetyDeviceIds: listing.safetyDevices.map((s) => s.id),
    considerationIds: listing.considerations.map((c) => c.id),
    checkInTime: hr ? apiTimeToInput(hr.checkInTime) : "15:00",
    checkOutTime: hr ? apiTimeToInput(hr.checkOutTime) : "11:00",
    maxGuests: hr?.maxGuests ?? 2,
    petsAllowed: hr?.petsAllowed ?? false,
    petsNotes: hr?.petsNotes ?? "",
    smokingAllowed: hr?.smokingAllowed ?? false,
    partiesAllowed: hr?.partiesAllowed ?? false,
    quietHoursStart: hr?.quietHoursStart ? apiTimeToInput(hr.quietHoursStart) : "",
    quietHoursEnd: hr?.quietHoursEnd ? apiTimeToInput(hr.quietHoursEnd) : "",
    leavingInstructions: hr?.leavingInstructions ?? "",
    additionalRules: hr?.additionalRules ?? "",
    cancellationType: cp?.type ?? "Moderate",
    freeCancellationDays: cp?.freeCancellationDays ?? 14,
    partialRefundPercent: cp?.partialRefundPercent ?? undefined,
    partialRefundDays: cp?.partialRefundDays ?? undefined,
    customTerms: cp?.customTerms ?? "",
    rentDueDayOfMonth: lt?.rentDueDayOfMonth ?? 1,
    paymentMethods: lt?.paymentMethods ?? "",
    nsfFirstFeeDollars: (lt?.nsfFirstFeeCents ?? 2500) / 100,
    nsfSubsequentFeeDollars: (lt?.nsfSubsequentFeeCents ?? 3500) / 100,
    lateFeePercent: lt?.lateFeePercent ?? 5,
    lateFeeGraceDays: lt?.lateFeeGraceDays ?? 3,
    utilitiesResponsibility: lt?.utilitiesResponsibility ?? "",
    yardMaintenanceByTenant: lt?.yardMaintenanceByTenant ?? false,
    furnished: lt?.furnished ?? false,
    includedAppliancesNotes: lt?.includedAppliancesNotes ?? "",
    keyCount: lt?.keyCount ?? 1,
    mailboxKeyCount: lt?.mailboxKeyCount ?? 0,
    keyReplacementFeeDollars: (lt?.keyReplacementFeeCents ?? 20000) / 100,
    lockoutFeeDollars: (lt?.lockoutFeeCents ?? 20000) / 100,
    parkingSpaceCount: lt?.parkingSpaceCount ?? 0,
    parkingDescription: lt?.parkingDescription ?? "",
    parkingIncludedInRent: lt?.parkingIncludedInRent ?? true,
    maxGuestConsecutiveDays: lt?.maxGuestConsecutiveDays ?? 7,
    rentersInsuranceMinLiabilityDollars: (lt?.rentersInsuranceMinLiabilityCents ?? 100_000_00) / 100,
    earlyTerminationFeeMonths: lt?.earlyTerminationFeeMonths ?? 2,
    builtBefore1978: lt?.builtBefore1978 ?? false,
    leadPaintKnowledge: lt?.leadPaintKnowledge ?? "",
    rentCapJustCauseExempt: lt?.rentCapJustCauseExempt ?? false,
    leaseAgreementSource: listing.leaseAgreementSource ?? "LagedraTemplate",
    hasCustomLeaseDocument: listing.customLeaseDocument != null,
  };
}
