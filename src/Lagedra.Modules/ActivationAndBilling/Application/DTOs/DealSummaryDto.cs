using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.SharedKernel.Integration;

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
    DateTime CreatedAt,
    TenantVerificationTier? TenantVerificationTier = null,
    string? DepositReason = null,
    bool? TruthSurfaceLocked = null);
