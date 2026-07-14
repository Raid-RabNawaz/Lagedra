using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

public sealed record ListMyPartnerInquiriesQuery(Guid CallerUserId)
    : IRequest<Result<IReadOnlyList<PartnerInquirySummaryDto>>>;

public sealed class ListMyPartnerInquiriesQueryHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IHostProfileProvider hostProfileProvider,
    IPartnerMembershipProvider membershipProvider)
    : IRequestHandler<ListMyPartnerInquiriesQuery, Result<IReadOnlyList<PartnerInquirySummaryDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerInquirySummaryDto>>> Handle(
        ListMyPartnerInquiriesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgId = await membershipProvider
            .GetPartnerOrganizationIdAsync(request.CallerUserId, cancellationToken)
            .ConfigureAwait(false);

        if (orgId is null)
        {
            return Result<IReadOnlyList<PartnerInquirySummaryDto>>.Success(
                Array.Empty<PartnerInquirySummaryDto>());
        }

        var orgName = await membershipProvider
            .GetOrganizationNameAsync(orgId.Value, cancellationToken)
            .ConfigureAwait(false);

        var rows = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.PartnerOrganizationId == orgId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.ListingId,
                s.TenantUserId,
                s.PartnerOrganizationId,
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
            return Result<IReadOnlyList<PartnerInquirySummaryDto>>.Success(
                Array.Empty<PartnerInquirySummaryDto>());
        }

        var listingSummaries = await listingProvider
            .GetListingSummariesAsync(
                rows.Select(r => r.ListingId).Distinct().ToList(),
                cancellationToken)
            .ConfigureAwait(false);
        var listingLookup = listingSummaries.ToDictionary(l => l.Id);

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

                return new PartnerInquirySummaryDto(
                    r.Id,
                    r.ListingId,
                    listing?.Title,
                    listing?.CoverPhotoUri,
                    listing?.City,
                    r.TenantUserId,
                    tenantLookup.GetValueOrDefault(r.TenantUserId),
                    r.PartnerOrganizationId!.Value,
                    orgName,
                    r.Status,
                    r.DealId,
                    r.CreatedAt,
                    lastActivity,
                    r.Questions.Count,
                    unanswered);
            })
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return Result<IReadOnlyList<PartnerInquirySummaryDto>>.Success(summaries);
    }
}
