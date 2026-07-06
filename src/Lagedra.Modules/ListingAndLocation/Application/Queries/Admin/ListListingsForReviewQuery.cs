using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries.Admin;

/// <summary>
/// Admin queue showing every listing currently awaiting moderation, oldest
/// submission first so the longest-waiting landlords get reviewed first. Each
/// item carries a snapshot of the host's public profile so the reviewer can
/// judge who's behind the listing without leaving the queue.
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
    DateTime CreatedAt,
    string? HostDisplayName,
    Uri? HostProfilePhotoUrl,
    bool HostIsGovernmentIdVerified,
    bool HostIsPhoneVerified,
    int? HostResponseRatePercent,
    DateTime? HostMemberSince,
    int HostProfileCompletenessPercent);

public sealed class ListListingsForReviewQueryHandler(
    ListingsDbContext dbContext,
    IHostProfileProvider hostProfileProvider)
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

        // Pull each distinct host's profile + completeness once, so listings
        // from the same landlord don't trigger duplicate cross-module lookups.
        var hostIds = listings.Select(l => l.LandlordUserId).Distinct();
        var hosts = new Dictionary<Guid, (HostProfileDto? Profile, HostProfileCompletenessDto? Completeness)>();
        foreach (var hostId in hostIds)
        {
            var profile = await hostProfileProvider
                .GetProfileAsync(hostId, cancellationToken)
                .ConfigureAwait(false);
            var completeness = await hostProfileProvider
                .GetProfileCompletenessAsync(hostId, cancellationToken)
                .ConfigureAwait(false);
            hosts[hostId] = (profile, completeness);
        }

        var items = listings.Select(l =>
        {
            var host = hosts.GetValueOrDefault(l.LandlordUserId);
            return new ListingReviewItemDto(
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
                l.CreatedAt,
                host.Profile?.DisplayName,
                host.Profile?.ProfilePhotoUrl,
                host.Profile?.IsGovernmentIdVerified ?? false,
                host.Profile?.IsPhoneVerified ?? false,
                host.Profile?.ResponseRatePercent,
                host.Profile?.MemberSince,
                host.Completeness?.PercentComplete ?? 0);
        }).ToList();

        return Result<IReadOnlyList<ListingReviewItemDto>>.Success(items);
    }
}
