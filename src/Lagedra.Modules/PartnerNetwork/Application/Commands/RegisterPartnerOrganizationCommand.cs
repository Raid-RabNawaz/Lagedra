using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record RegisterPartnerOrganizationCommand(
    string Name,
    PartnerOrganizationType OrganizationType,
    string ContactEmail,
    string? TaxId,
    Guid AdminUserId) : IRequest<Result<PartnerOrganizationDto>>;

public sealed class RegisterPartnerOrganizationCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<RegisterPartnerOrganizationCommand, Result<PartnerOrganizationDto>>
{
    public async Task<Result<PartnerOrganizationDto>> Handle(
        RegisterPartnerOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var org = PartnerOrganization.Create(
            request.Name,
            request.OrganizationType,
            request.ContactEmail,
            request.TaxId,
            clock);

        var adminMember = PartnerMember.Create(
            org.Id, request.AdminUserId, PartnerMemberRole.Admin, null, clock);

        dbContext.Organizations.Add(org);
        dbContext.Members.Add(adminMember);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerOrganizationDto>.Success(ToDto(org));
    }

    private static PartnerOrganizationDto ToDto(PartnerOrganization o) =>
        new(o.Id, o.Name, o.OrganizationType, o.Status, o.ContactEmail,
            o.TaxId, o.VerifiedAt, o.CreatedAt);
}
