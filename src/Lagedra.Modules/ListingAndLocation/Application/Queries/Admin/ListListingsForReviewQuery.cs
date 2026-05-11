using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries.Admin;

/// <summary>
/// Admin queue showing every listing currently awaiting moderation, oldest
/// submission first so the longest-waiting landlords get reviewed first.
/// </summary>
public sealed record ListListingsForReviewQuery() : IRequest<Result<IReadOnlyList<ListingReviewItemDto>>>;

public sealed record ListingReviewItemDto(
    Guid Id,
    Guid LandlordUserId,
    string Title,
    PropertyType PropertyType,
    int Bedrooms,
    decimal Bathrooms,
    long MonthlyRentCents,
    Uri? CoverPhotoUrl,
    int PhotoCount,
    DateTime? SubmittedForReviewAt,
    DateTime CreatedAt);

public sealed class ListListingsForReviewQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<ListListingsForReviewQuery, Result<IReadOnlyList<ListingReviewItemDto>>>
{
    public async Task<Result<IReadOnlyList<ListingReviewItemDto>>> Handle(
        ListListingsForReviewQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listings = await dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .Where(l => l.Status == ListingStatus.InReview)
            .OrderBy(l => l.SubmittedForReviewAt ?? l.UpdatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = listings.Select(l => new ListingReviewItemDto(
            l.Id,
            l.LandlordUserId,
            l.Title,
            l.PropertyType,
            l.Bedrooms,
            l.Bathrooms,
            l.MonthlyRentCents,
            l.Photos.FirstOrDefault(p => p.IsCover)?.Url
                ?? l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault()?.Url,
            l.Photos.Count,
            l.SubmittedForReviewAt,
            l.CreatedAt)).ToList();

        return Result<IReadOnlyList<ListingReviewItemDto>>.Success(items);
    }
}
