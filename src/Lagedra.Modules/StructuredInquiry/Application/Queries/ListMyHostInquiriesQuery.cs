using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

/// <summary>
/// Phase 17 — list every inquiry thread that targets one of the calling
/// user's listings. Powers the host "Inquiries" inbox so pre-booking
/// conversations are reachable without relying solely on the email
/// notification link.
/// </summary>
/// <remarks>
/// We resolve the host's listing ids through <see cref="IListingProvider"/>
/// rather than storing <c>LandlordUserId</c> on <c>InquirySession</c> — this
/// keeps the inquiry aggregate from snapshotting auth data that already
/// lives on the listing and avoids another EF migration.
/// </remarks>
public sealed record ListMyHostInquiriesQuery(Guid LandlordUserId)
    : IRequest<Result<IReadOnlyList<HostInquirySummaryDto>>>;

public sealed class ListMyHostInquiriesQueryHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IHostProfileProvider hostProfileProvider)
    : IRequestHandler<ListMyHostInquiriesQuery, Result<IReadOnlyList<HostInquirySummaryDto>>>
{
    public async Task<Result<IReadOnlyList<HostInquirySummaryDto>>> Handle(
        ListMyHostInquiriesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listingIds = await listingProvider
            .GetListingIdsForLandlordAsync(request.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        if (listingIds.Count == 0)
        {
            return Result<IReadOnlyList<HostInquirySummaryDto>>.Success(
                Array.Empty<HostInquirySummaryDto>());
        }

        // Project to a flat row in SQL — last-activity is the max of the
        // session createdAt and any answer's answeredAt, computed in-memory
        // after we hydrate the question/answer projection (Npgsql cannot
        // translate Max over a nested .Select with conditional expressions).
        var rows = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => listingIds.Contains(s.ListingId))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.ListingId,
                s.TenantUserId,
                s.Status,
                s.DealId,
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
            return Result<IReadOnlyList<HostInquirySummaryDto>>.Success(
                Array.Empty<HostInquirySummaryDto>());
        }

        var listingSummaries = await listingProvider
            .GetListingSummariesAsync(
                rows.Select(r => r.ListingId).Distinct().ToList(),
                cancellationToken)
            .ConfigureAwait(false);

        var listingLookup = listingSummaries.ToDictionary(l => l.Id);

        // Resolve tenant display names in parallel — still bounded by the
        // total number of distinct tenants in the host's inbox, which is
        // small in practice.
        var tenantIds = rows.Select(r => r.TenantUserId).Distinct().ToList();
        var tenantLookup = new Dictionary<Guid, string?>();
        foreach (var tenantId in tenantIds)
        {
            var profile = await hostProfileProvider
                .GetProfileAsync(tenantId, cancellationToken)
                .ConfigureAwait(false);
            tenantLookup[tenantId] = profile?.DisplayName;
        }

        var summaries = rows
            .Select(r =>
            {
                var listing = listingLookup.GetValueOrDefault(r.ListingId);
                var unanswered = r.Questions.Count(q => q.AnsweredAt is null);
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

                return new HostInquirySummaryDto(
                    r.Id,
                    r.ListingId,
                    listing?.Title,
                    listing?.CoverPhotoUri,
                    listing?.City,
                    r.TenantUserId,
                    tenantLookup.GetValueOrDefault(r.TenantUserId),
                    r.Status,
                    r.DealId,
                    r.CreatedAt,
                    lastActivity,
                    r.Questions.Count,
                    unanswered);
            })
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return Result<IReadOnlyList<HostInquirySummaryDto>>.Success(summaries);
    }
}
