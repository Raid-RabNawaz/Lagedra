using Lagedra.Modules.Reviews.Application.Queries;
using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Reviews.Infrastructure.Services;

public sealed class ReviewReputationProvider(
    ReviewsDbContext dbContext,
    IMediator mediator) : IReviewReputationProvider
{
    public async Task<UserReputationDto?> GetUserReputationAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUserReputationQuery(userId), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<UserReputationDto?> GetListingHostReputationAsync(
        Guid listingId,
        CancellationToken ct = default)
    {
        var map = await GetListingHostReputationsAsync([listingId], ct).ConfigureAwait(false);
        return map.TryGetValue(listingId, out var dto) ? dto : null;
    }

    public async Task<IReadOnlyDictionary<Guid, UserReputationDto>> GetListingHostReputationsAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken ct = default)
    {
        if (listingIds.Count == 0)
        {
            return new Dictionary<Guid, UserReputationDto>();
        }

        var distinctIds = listingIds.Distinct().ToList();
        var reviews = await dbContext.StayReviews
            .AsNoTracking()
            .Where(r => distinctIds.Contains(r.ListingId)
                        && r.Direction == StayReviewDirection.GuestToHost
                        && r.Status == StayReviewStatus.Published)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (reviews.Count == 0)
        {
            return new Dictionary<Guid, UserReputationDto>();
        }

        var result = new Dictionary<Guid, UserReputationDto>(reviews.Count);
        foreach (var group in reviews.GroupBy(r => r.ListingId))
        {
            var list = group.ToList();
            var categories = new Dictionary<string, double>(StringComparer.Ordinal);
            void Avg(string key, Func<Domain.Aggregates.StayReview, int?> selector)
            {
                var vals = list.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                if (vals.Count > 0)
                {
                    categories[key] = Math.Round(vals.Average(), 2);
                }
            }

            Avg("cleanliness", r => r.Cleanliness);
            Avg("accuracy", r => r.Accuracy);
            Avg("communication", r => r.Communication);
            Avg("location", r => r.Location);
            Avg("checkIn", r => r.CheckIn);
            Avg("value", r => r.Value);

            result[group.Key] = new UserReputationDto(
                list[0].RevieweeUserId,
                Math.Round(list.Average(r => r.OverallRating), 2),
                list.Count,
                categories);
        }

        return result;
    }
}
