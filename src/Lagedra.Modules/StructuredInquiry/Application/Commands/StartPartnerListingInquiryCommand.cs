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
/// Partner staff starts (or returns) an open inquiry for an endorsed member
/// on a listing, attaching their organization to the thread.
/// </summary>
public sealed record StartPartnerListingInquiryCommand(
    Guid ListingId,
    Guid TenantUserId,
    Guid CallerUserId) : IRequest<Result<InquiryDto>>;

public sealed class StartPartnerListingInquiryCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IPartnerMembershipProvider membershipProvider,
    IPartnerEndorsementProvider endorsementProvider)
    : IRequestHandler<StartPartnerListingInquiryCommand, Result<InquiryDto>>
{
    public async Task<Result<InquiryDto>> Handle(
        StartPartnerListingInquiryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var callerOrgId = await membershipProvider
            .GetPartnerOrganizationIdAsync(request.CallerUserId, cancellationToken)
            .ConfigureAwait(false);

        if (callerOrgId is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only partner organization staff can start an inquiry for a member."));
        }

        var endorsements = await endorsementProvider
            .GetActiveEndorsementsAsync(request.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var endorsement = endorsements.FirstOrDefault(e => e.OrganizationId == callerOrgId.Value);
        if (endorsement is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.EndorsementRequired",
                    "That tenant must have an active endorsement from your organization."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.ListingNotFound", "Listing not found."));
        }

        if (listing.LandlordUserId == request.TenantUserId)
        {
            return Result<InquiryDto>.Failure(
                new Error("Inquiry.SelfInquiry",
                    "Cannot start an inquiry for the listing host as the tenant."));
        }

        var existing = await dbContext.Sessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Include(s => s.Offers)
            .Where(s => s.ListingId == request.ListingId
                && s.TenantUserId == request.TenantUserId
                && s.Status == InquirySessionStatus.Open)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.PartnerOrganizationId is null)
            {
                try
                {
                    existing.AddPartner(callerOrgId.Value, request.CallerUserId, listing.LandlordUserId);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (InvalidOperationException ex)
                {
                    return Result<InquiryDto>.Failure(
                        new Error("Inquiry.PartnerConflict", ex.Message));
                }
            }
            else if (existing.PartnerOrganizationId != callerOrgId)
            {
                return Result<InquiryDto>.Failure(
                    new Error("Inquiry.PartnerConflict",
                        "This inquiry already has a different partner attached."));
            }

            var existingName = await membershipProvider
                .GetOrganizationNameAsync(callerOrgId.Value, cancellationToken)
                .ConfigureAwait(false);

            return Result<InquiryDto>.Success(
                InquiryDtoMapper.ToDto(
                    existing,
                    existingName ?? endorsement.OrganizationName,
                    listing.LandlordUserId));
        }

        var session = InquirySession.CreateForListing(
            request.ListingId, request.TenantUserId, listing.LandlordUserId);
        session.AddPartner(callerOrgId.Value, request.CallerUserId, listing.LandlordUserId);

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var name = await membershipProvider
            .GetOrganizationNameAsync(callerOrgId.Value, cancellationToken)
            .ConfigureAwait(false);

        return Result<InquiryDto>.Success(
            InquiryDtoMapper.ToDto(
                session,
                name ?? endorsement.OrganizationName,
                listing.LandlordUserId));
    }
}
