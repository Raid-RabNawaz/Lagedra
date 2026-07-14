using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record ProposeInquiryOfferCommand(
    Guid SessionId,
    Guid CallerUserId,
    long RentCents,
    long DepositCents,
    string? Note = null) : IRequest<Result<InquiryOfferDto>>;

public sealed class ProposeInquiryOfferCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<ProposeInquiryOfferCommand, Result<InquiryOfferDto>>
{
    public async Task<Result<InquiryOfferDto>> Handle(
        ProposeInquiryOfferCommand request,
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

        InquiryOfferRole role;
        if (session.TenantUserId == request.CallerUserId)
        {
            role = InquiryOfferRole.Tenant;
        }
        else if (landlordId == request.CallerUserId)
        {
            role = InquiryOfferRole.Host;
        }
        else
        {
            return Result<InquiryOfferDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the tenant or host on this thread can propose an offer."));
        }

        try
        {
            var offer = session.ProposeOffer(
                request.CallerUserId,
                role,
                request.RentCents,
                request.DepositCents,
                listing.MaxDepositCents,
                landlordId,
                request.Note);

            dbContext.Entry(offer).State = EntityState.Added;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<InquiryOfferDto>.Success(InquiryDtoMapper.ToOfferDto(offer));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result<InquiryOfferDto>.Failure(
                new Error("Inquiry.InvalidOffer", ex.Message));
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
