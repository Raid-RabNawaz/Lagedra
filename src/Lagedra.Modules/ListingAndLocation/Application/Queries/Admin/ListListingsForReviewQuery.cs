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
    int HostProfileCompletenessPercent,
    string? City = null,
    string? State = null,
    string? Country = null,
    bool InstantBookingEnabled = false,
    // Surfaced so a reviewer knows to read the host's own lease before
    // approving. Does not block approval.
    bool UsesCustomLeaseAgreement = false,
    string? CustomLeaseFileName = null);

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

        // Project cover URL + count only. Including the full photo graph for
        // Hostaway imports (hundreds of images) timed the admin SPA out and
        // made the review page look broken.
        var rows = await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.InReview)
            .OrderBy(l => l.SubmittedForReviewAt ?? l.UpdatedAt)
            .Select(l => new ReviewRow(
                l.Id,
                l.LandlordUserId,
                l.Title,
                l.PropertyType,
                l.Bedrooms,
                l.Bathrooms,
                l.MonthlyRentCents,
                l.Photos.Where(p => p.IsCover).Select(p => p.Url).FirstOrDefault()
                    ?? l.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault(),
                l.Photos.Count,
                l.SubmittedForReviewAt,
                l.CreatedAt,
                l.PreciseAddress != null ? l.PreciseAddress.City : null,
                l.PreciseAddress != null ? l.PreciseAddress.State : null,
                l.PreciseAddress != null ? l.PreciseAddress.Country : null,
                l.InstantBookingEnabled,
                l.LeaseAgreementSource == LeaseAgreementSource.HostProvided,
                l.CustomLeaseDocument != null ? l.CustomLeaseDocument.FileName : null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var hostIds = rows.Select(r => r.LandlordUserId).Distinct().ToList();
        var hosts = await hostProfileProvider
            .GetReviewSnapshotsAsync(hostIds, cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(r =>
        {
            hosts.TryGetValue(r.LandlordUserId, out var host);
            return new ListingReviewItemDto(
                r.Id,
                r.LandlordUserId,
                r.Title,
                r.PropertyType,
                r.Bedrooms,
                r.Bathrooms,
                r.MonthlyRentCents,
                r.CoverPhotoUrl,
                r.PhotoCount,
                r.SubmittedForReviewAt,
                r.CreatedAt,
                host?.Profile?.DisplayName,
                host?.Profile?.ProfilePhotoUrl,
                host?.Profile?.IsGovernmentIdVerified ?? false,
                host?.Profile?.IsPhoneVerified ?? false,
                host?.Profile?.ResponseRatePercent,
                host?.Profile?.MemberSince,
                host?.Completeness.PercentComplete ?? 0,
                r.City,
                r.State,
                r.Country,
                r.InstantBookingEnabled,
                r.UsesCustomLeaseAgreement,
                r.CustomLeaseFileName);
        }).ToList();

        return Result<IReadOnlyList<ListingReviewItemDto>>.Success(items);
    }

    private sealed record ReviewRow(
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
        string? City,
        string? State,
        string? Country,
        bool InstantBookingEnabled,
        bool UsesCustomLeaseAgreement,
        string? CustomLeaseFileName);
}
