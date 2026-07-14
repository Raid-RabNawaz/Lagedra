using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record AddInquiryPartnerCommand(
    Guid SessionId,
    Guid CallerUserId,
    Guid OrganizationId) : IRequest<Result<InquiryDto>>;

public sealed class AddInquiryPartnerCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IPartnerEndorsementProvider endorsementProvider,
    IPartnerMembershipProvider membershipProvider)
    : IRequestHandler<AddInquiryPartnerCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        AddInquiryPartnerCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
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

        if (session.TenantUserId != request.CallerUserId)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the tenant can invite a partner into this inquiry."));
        }

        var endorsements = await endorsementProvider
            .GetActiveEndorsementsAsync(session.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var endorsement = endorsements.FirstOrDefault(e => e.OrganizationId == request.OrganizationId);
        if (endorsement is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.EndorsementRequired",
                    "You must have an active endorsement from that partner to add them."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(session.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.ListingNotFound", "Listing not found."));
        }

        try
        {
            session.AddPartner(request.OrganizationId, request.CallerUserId, listing.LandlordUserId);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.PartnerConflict", ex.Message));
        }

        var name = await membershipProvider
            .GetOrganizationNameAsync(request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        return Result<InquiryDto>.Success(
            InquiryDtoMapper.ToDto(
                session,
                name ?? endorsement.OrganizationName,
                listing.LandlordUserId));
    }
}
