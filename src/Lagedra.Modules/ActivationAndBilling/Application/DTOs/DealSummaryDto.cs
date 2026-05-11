using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record DealSummaryDto(
    Guid DealId,
    Guid ApplicationId,
    Guid ListingId,
    string ListingTitle,
    Uri? ListingCoverPhotoUri,
    string? ListingCity,
    Guid TenantUserId,
    Guid LandlordUserId,
    DealApplicationStatus ApplicationStatus,
    string DealPhase,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    int StayDurationDays,
    long? MonthlyRentCents,
    long? DepositAmountCents,
    long? TotalAmountCents,
    BillingAccountStatus? BillingStatus,
    PaymentConfirmationStatus? PaymentStatus,
    DateTime CreatedAt);
