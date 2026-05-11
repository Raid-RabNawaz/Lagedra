using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Self-service registration for a new partner organization. The caller becomes the
/// initial admin member. Phase 18.10 — Option A: requires explicit acceptance of the
/// "endorsement is a relationship statement, not a financial guarantee" clause; the
/// acceptance timestamp + actor are stored on the aggregate (audit-grade record).
/// </summary>
public sealed record RegisterPartnerOrganizationCommand(
    string Name,
    PartnerOrganizationType OrganizationType,
    string ContactEmail,
    string? TaxId,
    Guid AdminUserId,
    bool EndorsementTermsAccepted) : IRequest<Result<PartnerOrganizationDto>>;

public sealed class RegisterPartnerOrganizationCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<RegisterPartnerOrganizationCommand, Result<PartnerOrganizationDto>>
{
    private static readonly Error TermsNotAccepted = new(
        "Partner.EndorsementTermsRequired",
        "You must accept the endorsement terms ('endorsement is a relationship statement, not a financial guarantee') to register a partner organization.");

    public async Task<Result<PartnerOrganizationDto>> Handle(
        RegisterPartnerOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.EndorsementTermsAccepted)
        {
            return Result<PartnerOrganizationDto>.Failure(TermsNotAccepted);
        }

        var org = PartnerOrganization.Create(
            request.Name,
            request.OrganizationType,
            request.ContactEmail,
            request.TaxId,
            request.AdminUserId,
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
