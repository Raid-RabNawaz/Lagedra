using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record ApprovePartnerEndorsementCommand(
    Guid OrganizationId,
    Guid EndorsementId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin,
    string? Note) : IRequest<Result<PartnerEndorsementDto>>;

public sealed class ApprovePartnerEndorsementCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IClock clock)
    : IRequestHandler<ApprovePartnerEndorsementCommand, Result<PartnerEndorsementDto>>
{
    public async Task<Result<PartnerEndorsementDto>> Handle(
        ApprovePartnerEndorsementCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireVerifiedOrgAdminAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<PartnerEndorsementDto>.Failure(authzResult.Error);
        }

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<PartnerEndorsementDto>.Failure(PartnerAccessErrors.NotFound);
        }

        var endorsement = await dbContext.Endorsements
            .FirstOrDefaultAsync(
                e => e.Id == request.EndorsementId
                  && e.OrganizationId == request.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (endorsement is null)
        {
            return Result<PartnerEndorsementDto>.Failure(
                new Error("Endorsement.NotFound", "Endorsement not found for this organization."));
        }

        if (endorsement.Status != PartnerEndorsementStatus.Requested)
        {
            return Result<PartnerEndorsementDto>.Failure(
                new Error("Endorsement.InvalidTransition",
                    $"Cannot approve endorsement in status '{endorsement.Status}'."));
        }

        endorsement.Approve(org.Name, request.CallerUserId, request.Note, clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerEndorsementDto>.Success(
            EndorsementMapper.ToDto(endorsement, org.Name));
    }
}
