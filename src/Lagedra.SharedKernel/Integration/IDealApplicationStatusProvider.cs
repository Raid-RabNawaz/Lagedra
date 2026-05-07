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
    Guid TenantUserId);

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
    string? JurisdictionWarning);
