using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Queries;

/// <summary>
/// Fetch a single inquiry thread by session id. Authorized for the tenant,
/// listing/deal host, attached partner org staff, or platform admin.
/// </summary>
public sealed record GetInquiryBySessionIdQuery(
    Guid SessionId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<InquiryDto>>;

public sealed class GetInquiryBySessionIdQueryHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IDealApplicationStatusProvider dealStatusProvider,
    IPartnerMembershipProvider membershipProvider)
    : IRequestHandler<GetInquiryBySessionIdQuery, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        GetInquiryBySessionIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.NotFound", "Inquiry thread not found."));
        }

        if (!request.IsAdmin && !await IsAuthorizedAsync(session, request.CallerUserId, cancellationToken)
            .ConfigureAwait(false))
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden", "You do not have access to this inquiry thread."));
        }

        string? partnerName = null;
        if (session.PartnerOrganizationId is { } orgId)
        {
            partnerName = await membershipProvider
                .GetOrganizationNameAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
        }

        var landlordUserId = await ResolveLandlordUserIdAsync(session, cancellationToken)
            .ConfigureAwait(false);

        return Result<InquiryDto>.Success(
            InquiryDtoMapper.ToDto(session, partnerName, landlordUserId));
    }

    private async Task<Guid?> ResolveLandlordUserIdAsync(
        InquirySession session,
        CancellationToken ct)
    {
        if (session.DealId is { } dealId)
        {
            var participants = await dealStatusProvider
                .GetParticipantsAsync(dealId, ct)
                .ConfigureAwait(false);

            if (participants is not null)
            {
                return participants.LandlordUserId;
            }
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(session.ListingId, ct)
            .ConfigureAwait(false);

        return listing?.LandlordUserId;
    }

    private async Task<bool> IsAuthorizedAsync(
        InquirySession session,
        Guid callerUserId,
        CancellationToken ct)
    {
        if (session.TenantUserId == callerUserId)
        {
            return true;
        }

        if (session.PartnerOrganizationId is { } partnerOrgId)
        {
            var callerOrgId = await membershipProvider
                .GetPartnerOrganizationIdAsync(callerUserId, ct)
                .ConfigureAwait(false);

            if (callerOrgId == partnerOrgId)
            {
                return true;
            }
        }

        if (session.DealId is { } dealId)
        {
            var participants = await dealStatusProvider
                .GetParticipantsAsync(dealId, ct)
                .ConfigureAwait(false);

            if (participants is not null
                && (participants.LandlordUserId == callerUserId
                    || participants.TenantUserId == callerUserId))
            {
                return true;
            }
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(session.ListingId, ct)
            .ConfigureAwait(false);

        return listing is not null && listing.LandlordUserId == callerUserId;
    }
}
