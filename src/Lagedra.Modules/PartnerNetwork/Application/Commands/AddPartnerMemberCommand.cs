using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record AddPartnerMemberCommand(
    Guid OrganizationId,
    Guid UserId,
    PartnerMemberRole Role,
    Guid InvitedByUserId) : IRequest<Result<PartnerMemberDto>>;

public sealed class AddPartnerMemberCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<AddPartnerMemberCommand, Result<PartnerMemberDto>>
{
    public async Task<Result<PartnerMemberDto>> Handle(
        AddPartnerMemberCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgExists = await dbContext.Organizations
            .AnyAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (!orgExists)
        {
            return Result<PartnerMemberDto>.Failure(
                new Error("Partner.NotFound", "Partner organization not found."));
        }

        var alreadyMember = await dbContext.Members
            .AnyAsync(m => m.OrganizationId == request.OrganizationId
                        && m.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyMember)
        {
            return Result<PartnerMemberDto>.Failure(
                new Error("Partner.AlreadyMember", "User is already a member of this organization."));
        }

        var member = PartnerMember.Create(
            request.OrganizationId, request.UserId, request.Role,
            request.InvitedByUserId, clock);

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerMemberDto>.Success(
            new PartnerMemberDto(member.Id, member.OrganizationId, member.UserId,
                member.MemberRole, member.JoinedAt, member.InvitedBy));
    }
}
