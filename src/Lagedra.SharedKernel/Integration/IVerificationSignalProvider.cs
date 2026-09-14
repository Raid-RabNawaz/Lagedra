namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns current identity and screening signals for a user.
/// Implemented by the IdentityAndVerification module.
/// </summary>
public interface IVerificationSignalProvider
{
    Task<VerificationSignalDto?> GetSignalsAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, DateTime>> GetLatestScreeningTimesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, ApplicationScreeningOrderDto>> GetLatestScreeningOrdersAsync(
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken ct = default);
}

public sealed record VerificationSignalDto(
    bool IsIdentityVerified,
    bool IsIdentityPending,
    bool IsIdentityFailed,
    bool IsBackgroundCheckPassed,
    bool IsBackgroundCheckFailed,
    bool IsBackgroundCheckUnderReview,
    DateTime? LastScreeningAt = null);

public sealed record ApplicationScreeningOrderDto(
    Guid ApplicationId,
    Guid HostUserId,
    string Status,
    string? Result);
