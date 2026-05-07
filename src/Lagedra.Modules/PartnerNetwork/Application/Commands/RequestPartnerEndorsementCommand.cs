using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Creates a <see cref="PartnerEndorsement"/> in the <see cref="PartnerEndorsementStatus.Requested"/> state.
///
/// Caller authorization (per <see cref="RequestPartnerEndorsementCallerKind"/>):
///   - Tenant: <see cref="CallerUserId"/> must equal <see cref="TenantUserId"/>.
///   - Partner: caller must be an admin member of <see cref="OrganizationId"/> AND the org must be Verified.
///
/// Idempotent: if an active (Requested or Approved) endorsement already exists for the
/// (org, tenant) pair, the existing endorsement is returned with <c>Result.Success</c>.
/// </summary>
public sealed record RequestPartnerEndorsementCommand(
    Guid OrganizationId,
    Guid TenantUserId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin,
    RequestPartnerEndorsementCallerKind CallerKind,
    string? Note) : IRequest<Result<PartnerEndorsementDto>>;

public enum RequestPartnerEndorsementCallerKind
{
    Tenant,
    Partner
}

public sealed class RequestPartnerEndorsementCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IClock clock)
    : IRequestHandler<RequestPartnerEndorsementCommand, Result<PartnerEndorsementDto>>
{
    public async Task<Result<PartnerEndorsementDto>> Handle(
        RequestPartnerEndorsementCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<PartnerEndorsementDto>.Failure(PartnerAccessErrors.NotFound);
        }

        if (org.Status != PartnerOrganizationStatus.Verified)
        {
            return Result<PartnerEndorsementDto>.Failure(PartnerAccessErrors.OrgNotVerified);
        }

        if (request.CallerKind == RequestPartnerEndorsementCallerKind.Partner)
        {
            var authzResult = await accessService.RequireVerifiedOrgAdminAsync(
                request.CallerUserId,
                request.OrganizationId,
                request.CallerIsPlatformAdmin,
                cancellationToken).ConfigureAwait(false);

            if (authzResult.IsFailure)
            {
                return Result<PartnerEndorsementDto>.Failure(authzResult.Error);
            }
        }
        else
        {
            // Tenant-driven request: caller must be the tenant themselves.
            if (request.CallerUserId != request.TenantUserId && !request.CallerIsPlatformAdmin)
            {
                return Result<PartnerEndorsementDto>.Failure(PartnerAccessErrors.Forbidden);
            }
        }

        var existingActive = await dbContext.Endorsements
            .FirstOrDefaultAsync(
                e => e.OrganizationId == request.OrganizationId
                  && e.TenantUserId == request.TenantUserId
                  && (e.Status == PartnerEndorsementStatus.Requested
                   || e.Status == PartnerEndorsementStatus.Approved),
                cancellationToken)
            .ConfigureAwait(false);

        if (existingActive is not null)
        {
            return Result<PartnerEndorsementDto>.Success(
                EndorsementMapper.ToDto(existingActive, org.Name));
        }

        var endorsement = PartnerEndorsement.Request(
            request.OrganizationId,
            request.TenantUserId,
            request.CallerUserId,
            request.Note,
            clock);

        dbContext.Endorsements.Add(endorsement);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerEndorsementDto>.Success(
            EndorsementMapper.ToDto(endorsement, org.Name));
    }
}
