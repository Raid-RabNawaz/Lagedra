using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

/// <summary>
/// Phase 17 — list every inquiry thread the calling tenant has started,
/// across every listing. Powers the Member-side "My conversations"
/// inbox, which mirrors <see cref="ListMyHostInquiriesQuery"/> on the
/// receiving side. Tenants and hosts share the same role today, so we
/// surface both inboxes via different sidebar groups (Bookings vs.
/// Hosting) rather than via role gating.
/// </summary>
public sealed record ListMyTenantInquiriesQuery(Guid TenantUserId)
    : IRequest<Result<IReadOnlyList<TenantInquirySummaryDto>>>;

public sealed class ListMyTenantInquiriesQueryHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IHostProfileProvider hostProfileProvider)
    : IRequestHandler<ListMyTenantInquiriesQuery, Result<IReadOnlyList<TenantInquirySummaryDto>>>
{
    public async Task<Result<IReadOnlyList<TenantInquirySummaryDto>>> Handle(
        ListMyTenantInquiriesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rows = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.TenantUserId == request.TenantUserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.ListingId,
                s.Status,
                s.DealId,
                s.PartnerOrganizationId,
                s.CreatedAt,
                Questions = s.Questions
                    .Select(q => new
                    {
                        q.SubmittedAt,
                        AnsweredAt = q.Answer != null ? (DateTime?)q.Answer.AnsweredAt : null,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return Result<IReadOnlyList<TenantInquirySummaryDto>>.Success(
                Array.Empty<TenantInquirySummaryDto>());
        }

        var listingIds = rows.Select(r => r.ListingId).Distinct().ToList();

        var listingSummaries = await listingProvider
            .GetListingSummariesAsync(listingIds, cancellationToken)
            .ConfigureAwait(false);
        var listingLookup = listingSummaries.ToDictionary(l => l.Id);

        var landlordLookup = await listingProvider
            .GetLandlordIdsForListingsAsync(listingIds, cancellationToken)
            .ConfigureAwait(false);

        // Resolve host display names — distinct landlord ids only, so the
        // round-trip count stays bounded by the number of unique hosts in
        // the tenant's inbox.
        var landlordIds = landlordLookup.Values.Distinct().ToList();
        var landlordNames = new Dictionary<Guid, string?>();
        foreach (var landlordId in landlordIds)
        {
            var profile = await hostProfileProvider
                .GetProfileAsync(landlordId, cancellationToken)
                .ConfigureAwait(false);
            landlordNames[landlordId] = profile?.DisplayName;
        }

        var summaries = rows
            .Select(r =>
            {
                var listing = listingLookup.GetValueOrDefault(r.ListingId);
                var landlordId = landlordLookup.GetValueOrDefault(r.ListingId);
                var unansweredByHost = r.Questions.Count(q => q.AnsweredAt is null);
                var lastAnsweredAt = r.Questions
                    .Where(q => q.AnsweredAt is not null)
                    .Select(q => q.AnsweredAt!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
                var lastSubmittedAt = r.Questions
                    .Select(q => q.SubmittedAt)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
                var lastActivity = new[] { r.CreatedAt, lastAnsweredAt, lastSubmittedAt }.Max();

                return new TenantInquirySummaryDto(
                    r.Id,
                    r.ListingId,
                    listing?.Title,
                    listing?.CoverPhotoUri,
                    listing?.City,
                    landlordId,
                    landlordId == Guid.Empty
                        ? null
                        : landlordNames.GetValueOrDefault(landlordId),
                    r.Status,
                    r.DealId,
                    r.CreatedAt,
                    lastActivity,
                    r.Questions.Count,
                    unansweredByHost,
                    r.PartnerOrganizationId);
            })
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return Result<IReadOnlyList<TenantInquirySummaryDto>>.Success(summaries);
    }
}
