using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

/// <summary>
/// Attaches host stay-review reputation onto listing cards for marketplace surfaces.
/// </summary>
internal static class ListingSummaryEnricher
{
    public static async Task<IReadOnlyList<ListingSummaryDto>> ToSummariesAsync(
        IReadOnlyList<Listing> listings,
        IReviewReputationProvider? reputationProvider,
        CancellationToken cancellationToken)
    {
        if (listings.Count == 0)
        {
            return Array.Empty<ListingSummaryDto>();
        }

        IReadOnlyDictionary<Guid, UserReputationDto> reputations =
            new Dictionary<Guid, UserReputationDto>();

        if (reputationProvider is not null)
        {
            reputations = await reputationProvider
                .GetListingHostReputationsAsync(listings.Select(l => l.Id).ToList(), cancellationToken)
                .ConfigureAwait(false);
        }

        return listings.Select(listing =>
        {
            reputations.TryGetValue(listing.Id, out var rep);
            return ListingMapper.ToSummary(
                listing,
                qualityScore: null,
                hostAverageRating: rep?.AverageOverall,
                hostReviewCount: rep?.ReviewCount ?? 0);
        }).ToList();
    }
}
