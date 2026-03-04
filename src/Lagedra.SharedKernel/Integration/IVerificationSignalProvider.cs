namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns current identity and background-check signals for a user.
/// Implemented by the IdentityAndVerification module.
/// </summary>
public interface IVerificationSignalProvider
{
    Task<VerificationSignalDto?> GetSignalsAsync(Guid userId, CancellationToken ct = default);
}

public sealed record VerificationSignalDto(
    bool IsIdentityVerified,
    bool IsIdentityPending,
    bool IsIdentityFailed,
    bool IsBackgroundCheckPassed,
    bool IsBackgroundCheckFailed,
    bool IsBackgroundCheckUnderReview);
