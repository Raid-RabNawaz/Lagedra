using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record AcceptInquiryOfferCommand(
    Guid SessionId,
    Guid OfferId,
    Guid CallerUserId) : IRequest<Result<InquiryOfferDto>>;

public sealed class AcceptInquiryOfferCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<AcceptInquiryOfferCommand, Result<InquiryOfferDto>>
{
    public async Task<Result<InquiryOfferDto>> Handle(
        AcceptInquiryOfferCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryOfferDto>.Failure(
                new Error("Inquiry.NotFound", "Inquiry thread not found."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(session.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<InquiryOfferDto>.Failure(
                new Error("Inquiry.ListingNotFound", "Listing not found."));
        }

        var landlordId = await ResolveLandlordIdAsync(
                session.DealId, listing.LandlordUserId, cancellationToken)
            .ConfigureAwait(false) ?? listing.LandlordUserId;

        var isParticipant = session.TenantUserId == request.CallerUserId
            || landlordId == request.CallerUserId;

        if (!isParticipant)
        {
            return Result<InquiryOfferDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the tenant or host on this thread can accept an offer."));
        }

        try
        {
            var offer = session.AcceptOffer(request.OfferId, request.CallerUserId, landlordId);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<InquiryOfferDto>.Success(InquiryDtoMapper.ToOfferDto(offer));
        }
        catch (InvalidOperationException ex)
        {
            return Result<InquiryOfferDto>.Failure(
                new Error("Inquiry.OfferConflict", ex.Message));
        }
    }

    private async Task<Guid?> ResolveLandlordIdAsync(
        Guid? dealId,
        Guid listingLandlordId,
        CancellationToken ct)
    {
        if (dealId is { } id)
        {
            var participants = await dealStatusProvider
                .GetParticipantsAsync(id, ct)
                .ConfigureAwait(false);

            if (participants is not null)
            {
                return participants.LandlordUserId;
            }
        }

        return listingLandlordId;
    }
}
