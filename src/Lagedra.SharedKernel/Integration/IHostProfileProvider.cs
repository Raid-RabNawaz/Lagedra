namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns public profile data for a host (landlord) to display alongside listings.
/// Implemented by the Auth module.
/// </summary>
public interface IHostProfileProvider
{
    Task<HostProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// How complete the host's public profile is, used to gate going live and
    /// to surface guidance in the UI. A tenant authorising a multi-thousand
    /// dollar booking needs to see who they're transacting with, so a listing
    /// can only be submitted for review once the host's profile is sufficiently
    /// filled in (name, photo, bio, location, etc.).
    /// </summary>
    Task<HostProfileCompletenessDto> GetProfileCompletenessAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Profile + completeness for many hosts in one read. Used by the admin
    /// review queue so a large pending set does not fan out into 2N lookups.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, HostReviewSnapshot>> GetReviewSnapshotsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default);
}

public sealed record HostReviewSnapshot(
    HostProfileDto? Profile,
    HostProfileCompletenessDto Completeness);

public sealed record HostProfileDto(
    string? DisplayName,
    Uri? ProfilePhotoUrl,
    bool IsGovernmentIdVerified,
    bool IsPhoneVerified,
    int? ResponseRatePercent,
    int? ResponseTimeMinutes,
    DateTime MemberSince);

/// <summary>
/// Snapshot of how filled-in a host's profile is.
/// <paramref name="PercentComplete"/> is 0–100, computed over a fixed set of
/// public-trust fields. <paramref name="MissingFields"/> carries human-readable
/// labels for the still-empty fields so callers can tell the host exactly what
/// to add.
/// </summary>
public sealed record HostProfileCompletenessDto(
    int PercentComplete,
    IReadOnlyList<string> MissingFields);
