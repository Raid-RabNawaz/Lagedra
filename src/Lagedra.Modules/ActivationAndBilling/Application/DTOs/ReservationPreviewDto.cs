using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// Full price breakdown the tenant sees before submitting a reservation
/// request: the predetermined deposit for their verification tier (+ why),
/// rent, fees, and the total they'll be charged on host approval.
/// </summary>
public sealed record ReservationPreviewDto(
    Guid ListingId,
    TenantVerificationTier Tier,
    long DepositCents,
    string DepositReason,
    long FirstMonthRentCents,
    long InsuranceFeeCents,
    long ServiceFeeCents,
    long MonthlyProtocolFeeCents,
    long TotalPayableCents,
    int StayDurationDays);
