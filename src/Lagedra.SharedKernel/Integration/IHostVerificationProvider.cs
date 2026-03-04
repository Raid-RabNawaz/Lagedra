namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Provides host (landlord) verification status for listing display.
/// Implemented by IdentityAndVerification module.
/// </summary>
public interface IHostVerificationProvider
{
    Task<HostVerificationDto?> GetVerificationAsync(Guid userId, CancellationToken ct = default);
}

public sealed record HostVerificationDto(bool IsVerified, bool IsKycComplete);
