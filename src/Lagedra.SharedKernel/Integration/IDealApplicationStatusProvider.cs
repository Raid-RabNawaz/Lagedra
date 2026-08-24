namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module provider for deal application status and participant info.
/// Implemented by ActivationAndBilling, consumed by TruthSurface.
/// </summary>
public interface IDealApplicationStatusProvider
{
    Task<bool> IsApprovedAsync(Guid dealId, CancellationToken ct = default);
    Task<DealParticipantsDto?> GetParticipantsAsync(Guid dealId, CancellationToken ct = default);
    Task<DateOnly?> GetRequestedCheckOutAsync(Guid dealId, CancellationToken ct = default);
    Task<DealApplicationDetailsDto?> GetDealDetailsAsync(Guid dealId, CancellationToken ct = default);
}

public sealed record DealParticipantsDto(
    Guid LandlordUserId,
    Guid TenantUserId,
    Guid ListingId);

public sealed record DealApplicationDetailsDto(
    Guid ApplicationId,
    Guid DealId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    int StayDurationDays,
    long? FirstMonthRentCents,
    long? DepositAmountCents,
    long? InsuranceFeeCents,
    string? JurisdictionWarning,
    int GuestCount = 1,
    string? Message = null,
    long? ServiceFeeCents = null,
    long? TotalPayableSnapshotCents = null,
    TenantVerificationTier? TenantVerificationTier = null,
    string? DepositReason = null,
    Guid? HomeOwnerUserId = null,
    bool OwnerConsentRequired = false,
    bool OwnerTenancyConsentGiven = false,
    DateTime? OwnerTenancyConsentAt = null,
    string? OwnerConsentVersion = null);
