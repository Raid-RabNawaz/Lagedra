using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record SuspendPartnerOrganizationCommand(
    Guid OrganizationId,
    string Reason,
    Guid SuspendedByUserId) : IRequest<Result<PartnerOrganizationDto>>;

public sealed class SuspendPartnerOrganizationCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<SuspendPartnerOrganizationCommand, Result<PartnerOrganizationDto>>
{
    public async Task<Result<PartnerOrganizationDto>> Handle(
        SuspendPartnerOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<PartnerOrganizationDto>.Failure(
                new Error("Partner.SuspendReasonRequired",
                    "A reason is required when suspending a partner organization."));
        }

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<PartnerOrganizationDto>.Failure(PartnerAccessErrors.NotFound);
        }

        org.Suspend(request.Reason, clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerOrganizationDto>.Success(ToDto(org));
    }

    private static PartnerOrganizationDto ToDto(PartnerOrganization o) =>
        new(o.Id, o.Name, o.OrganizationType, o.Status, o.ContactEmail,
            o.TaxId, o.VerifiedAt, o.CreatedAt);
}
