using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

/// <summary>
/// Phase 17 — start (or return the existing open) pre-booking inquiry
/// thread the calling tenant has for a given listing. Idempotent for
/// open <em>listing-scoped</em> sessions only (<c>DealId == null</c>).
/// Once a thread is linked to a booking, a new ask-the-host CTA creates
/// a fresh pre-booking thread for a potential next booking.
/// </summary>
public sealed record StartListingInquiryCommand(
    Guid ListingId,
    Guid TenantUserId) : IRequest<Result<InquiryDto>>;

public sealed class StartListingInquiryCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider)
    : IRequestHandler<StartListingInquiryCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        StartListingInquiryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.ListingNotFound", "Listing not found."));
        }

        // The tenant can't start an inquiry against their own listing — that
        // would be self-conversation and would also pollute host inboxes
        // when they navigate to one of their own pages.
        if (listing.LandlordUserId == request.TenantUserId)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.SelfInquiry",
                    "You cannot start an inquiry on your own listing."));
        }

        // One open pre-booking thread per (listing, tenant). Deal-linked
        // threads stay on the booking and must not be reused from the listing.
        var existing = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Include(s => s.Offers)
            .Where(s => s.ListingId == request.ListingId
                && s.TenantUserId == request.TenantUserId
                && s.DealId == null
                && s.Status == InquirySessionStatus.Open)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result<InquiryDto>.Success(
                InquiryDtoMapper.ToDto(existing, landlordUserId: listing.LandlordUserId));
        }

        var session = InquirySession.CreateForListing(
            request.ListingId, request.TenantUserId, listing.LandlordUserId);

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryDto>.Success(
            InquiryDtoMapper.ToDto(session, landlordUserId: listing.LandlordUserId));
    }
}
