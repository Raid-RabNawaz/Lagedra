using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;

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
    bool? TruthSurfaceLocked = null,
    DateTime? HostConfirmedDepositReturnedAt = null,
    DateTime? TenantConfirmedDepositReceivedAt = null,
    DateTime? DepositReturnSettledAt = null,
    long? InsuranceFeeCents = null,
    string? LeaseAgreementSource = null,
    long EvictionProtectionFeeCents = 0,
    bool EvictionProtectionIncluded = false,
    EvictionProtectionClaimStanding? EvictionProtectionClaimStanding = null);
