namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module reputation aggregates from published stay reviews.
/// Implemented by the Reviews module.
/// </summary>
public interface IReviewReputationProvider
{
    Task<UserReputationDto?> GetUserReputationAsync(Guid userId, CancellationToken ct = default);

    Task<UserReputationDto?> GetListingHostReputationAsync(Guid listingId, CancellationToken ct = default);

    /// <summary>
    /// Batch host reputation for marketplace / search cards. Keys are listing IDs.
    /// Listings with no published guest→host reviews are omitted.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, UserReputationDto>> GetListingHostReputationsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken ct = default);
}

public sealed record UserReputationDto(
    Guid SubjectId,
    double AverageOverall,
    int ReviewCount,
    IReadOnlyDictionary<string, double> CategoryAverages);
