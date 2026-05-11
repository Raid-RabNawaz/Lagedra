using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record GetPartnerOrganizationQuery(
    Guid OrganizationId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<PartnerOrganizationDto>>;

public sealed class GetPartnerOrganizationQueryHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService)
    : IRequestHandler<GetPartnerOrganizationQuery, Result<PartnerOrganizationDto>>
{
    public async Task<Result<PartnerOrganizationDto>> Handle(
        GetPartnerOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireMemberAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<PartnerOrganizationDto>.Failure(authzResult.Error);
        }

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<PartnerOrganizationDto>.Failure(PartnerAccessErrors.NotFound);
        }

        return Result<PartnerOrganizationDto>.Success(
            new PartnerOrganizationDto(org.Id, org.Name, org.OrganizationType,
                org.Status, org.ContactEmail, org.TaxId, org.VerifiedAt, org.CreatedAt));
    }
}
