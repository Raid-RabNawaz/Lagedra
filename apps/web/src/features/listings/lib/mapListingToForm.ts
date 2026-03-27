import type { ListingDetailsDto } from "@/api/types";
import type { ListingFormValues } from "./listingFormSchema";
import { apiTimeToInput } from "./listingFormSchema";

export function listingDetailsToFormValues(listing: ListingDetailsDto): ListingFormValues {
  const hr = listing.houseRules;
  const cp = listing.cancellationPolicy;

  return {
    propertyType: listing.propertyType,
    title: listing.title,
    description: listing.description,
    monthlyRentDollars: listing.monthlyRentCents / 100,
    maxDepositDollars: listing.maxDepositCents / 100,
    insuranceRequired: listing.insuranceRequired,
    bedrooms: listing.bedrooms,
    bathrooms: Number(listing.bathrooms),
    minStayDays: listing.minStayDays ?? 30,
    maxStayDays: listing.maxStayDays ?? 180,
    squareFootage: listing.squareFootage ?? undefined,
    instantBookingEnabled: listing.instantBookingEnabled,
    virtualTourUrl: listing.virtualTourUrl ?? "",
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
  };
}
