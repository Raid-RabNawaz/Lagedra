using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

/// <summary>
/// Returns the caller's partner organization + role, or <c>Partner.NoMembership</c> if
/// they are not a member of any organization.
///
/// Resolution: any active <see cref="Lagedra.Modules.PartnerNetwork.Domain.Entities.PartnerMember"/>
/// row keyed off <see cref="CallerUserId"/>. The <c>partner_org_id</c> JWT claim populated by
/// <see cref="Lagedra.Modules.PartnerNetwork.Infrastructure.Services.PartnerMembershipProvider"/>
/// is a hint for the frontend; the source of truth is the database.
/// </summary>
public sealed record GetMyPartnerOrganizationQuery(
    Guid CallerUserId) : IRequest<Result<MyPartnerMembershipDto>>;

public sealed class GetMyPartnerOrganizationQueryHandler(PartnerDbContext dbContext)
    : IRequestHandler<GetMyPartnerOrganizationQuery, Result<MyPartnerMembershipDto>>
{
    public async Task<Result<MyPartnerMembershipDto>> Handle(
        GetMyPartnerOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var membership = await dbContext.Members
            .AsNoTracking()
            .Where(m => m.UserId == request.CallerUserId)
            .OrderByDescending(m => m.JoinedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return Result<MyPartnerMembershipDto>.Failure(
                new Error("Partner.NoMembership", "You are not a member of any partner organization."));
        }

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == membership.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<MyPartnerMembershipDto>.Failure(
                new Error("Partner.NoMembership", "Your partner organization is no longer available."));
        }

        return Result<MyPartnerMembershipDto>.Success(new MyPartnerMembershipDto(
            new PartnerOrganizationDto(org.Id, org.Name, org.OrganizationType,
                org.Status, org.ContactEmail, org.TaxId, org.VerifiedAt, org.CreatedAt),
            membership.MemberRole,
            membership.JoinedAt));
    }
}
