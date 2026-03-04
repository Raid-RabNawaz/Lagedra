namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns public profile data for a host (landlord) to display alongside listings.
/// Implemented by the Auth module.
/// </summary>
public interface IHostProfileProvider
{
    Task<HostProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);
}

public sealed record HostProfileDto(
    string? DisplayName,
    Uri? ProfilePhotoUrl,
    bool IsGovernmentIdVerified,
    bool IsPhoneVerified,
    int? ResponseRatePercent,
    int? ResponseTimeMinutes,
    DateTime MemberSince);
