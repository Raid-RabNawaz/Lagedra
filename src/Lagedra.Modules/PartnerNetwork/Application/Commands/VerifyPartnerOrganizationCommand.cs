using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record VerifyPartnerOrganizationCommand(
    Guid OrganizationId,
    Guid VerifiedByUserId) : IRequest<Result<PartnerOrganizationDto>>;

public sealed class VerifyPartnerOrganizationCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<VerifyPartnerOrganizationCommand, Result<PartnerOrganizationDto>>
{
    public async Task<Result<PartnerOrganizationDto>> Handle(
        VerifyPartnerOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<PartnerOrganizationDto>.Failure(
                new Error("Partner.NotFound", "Partner organization not found."));
        }

        org.Verify(request.VerifiedByUserId, clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerOrganizationDto>.Success(ToDto(org));
    }

    private static PartnerOrganizationDto ToDto(PartnerOrganization o) =>
        new(o.Id, o.Name, o.OrganizationType, o.Status, o.ContactEmail,
            o.TaxId, o.VerifiedAt, o.CreatedAt);
}
