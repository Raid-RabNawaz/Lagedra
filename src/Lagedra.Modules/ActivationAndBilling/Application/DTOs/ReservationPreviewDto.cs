using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// Full price breakdown for the apply dialog. When
/// <see cref="IsNegotiatedOffer"/> is true, rent and deposit come from an
/// accepted inquiry offer rather than listing/tier defaults.
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
    int StayDurationDays,
    bool IsNegotiatedOffer = false,
    Guid? NegotiatedOfferId = null);
