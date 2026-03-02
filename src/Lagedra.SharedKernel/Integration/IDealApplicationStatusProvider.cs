namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module provider for deal application status and participant info.
/// Implemented by ActivationAndBilling, consumed by TruthSurface.
/// </summary>
public interface IDealApplicationStatusProvider
{
    Task<bool> IsApprovedAsync(Guid dealId, CancellationToken ct = default);
    Task<DealParticipantsDto?> GetParticipantsAsync(Guid dealId, CancellationToken ct = default);
}

public sealed record DealParticipantsDto(
    Guid LandlordUserId,
    Guid TenantUserId);
