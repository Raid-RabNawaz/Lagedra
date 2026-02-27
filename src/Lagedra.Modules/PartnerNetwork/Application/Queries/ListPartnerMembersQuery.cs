using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record ListPartnerMembersQuery(
    Guid OrganizationId) : IRequest<Result<IReadOnlyList<PartnerMemberDto>>>;

public sealed class ListPartnerMembersQueryHandler(PartnerDbContext dbContext)
    : IRequestHandler<ListPartnerMembersQuery, Result<IReadOnlyList<PartnerMemberDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerMemberDto>>> Handle(
        ListPartnerMembersQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var members = await dbContext.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == request.OrganizationId)
            .OrderByDescending(m => m.JoinedAt)
            .Select(m => new PartnerMemberDto(
                m.Id, m.OrganizationId, m.UserId,
                m.MemberRole, m.JoinedAt, m.InvitedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PartnerMemberDto>>.Success(members);
    }
}
