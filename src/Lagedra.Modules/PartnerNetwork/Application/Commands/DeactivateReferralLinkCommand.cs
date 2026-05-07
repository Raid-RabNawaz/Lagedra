using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record DeactivateReferralLinkCommand(
    Guid OrganizationId,
    Guid ReferralLinkId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<ReferralLinkDto>>;

public sealed class DeactivateReferralLinkCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IClock clock)
    : IRequestHandler<DeactivateReferralLinkCommand, Result<ReferralLinkDto>>
{
    public async Task<Result<ReferralLinkDto>> Handle(
        DeactivateReferralLinkCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireAdminMemberAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<ReferralLinkDto>.Failure(authzResult.Error);
        }

        var link = await dbContext.ReferralLinks
            .FirstOrDefaultAsync(l => l.Id == request.ReferralLinkId
                                   && l.OrganizationId == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (link is null)
        {
            return Result<ReferralLinkDto>.Failure(
                new Error("Referral.NotFound", "Referral link not found for this organization."));
        }

        if (!link.IsActive)
        {
            return Result<ReferralLinkDto>.Success(
                new ReferralLinkDto(link.Id, link.OrganizationId, link.Code,
                    link.CreatedByUserId, link.ExpiresAt, link.MaxUses,
                    link.UsageCount, link.IsActive, link.CreatedAt));
        }

        link.Deactivate(clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ReferralLinkDto>.Success(
            new ReferralLinkDto(link.Id, link.OrganizationId, link.Code,
                link.CreatedByUserId, link.ExpiresAt, link.MaxUses,
                link.UsageCount, link.IsActive, link.CreatedAt));
    }
}
