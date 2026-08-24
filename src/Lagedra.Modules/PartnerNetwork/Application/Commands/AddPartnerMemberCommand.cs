using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Adds an existing Lagedra user to a partner organization. The member is
/// identified by email (what the inviting admin actually knows) or, for
/// admin tooling, directly by user id.
/// </summary>
public sealed record AddPartnerMemberCommand(
    Guid OrganizationId,
    Guid? UserId,
    string? Email,
    PartnerMemberRole Role,
    Guid InvitedByUserId,
    bool InvitedByIsPlatformAdmin) : IRequest<Result<PartnerMemberDto>>;

public sealed class AddPartnerMemberCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IUserEmailResolver emailResolver,
    IClock clock)
    : IRequestHandler<AddPartnerMemberCommand, Result<PartnerMemberDto>>
{
    public async Task<Result<PartnerMemberDto>> Handle(
        AddPartnerMemberCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireAdminMemberAsync(
            request.InvitedByUserId,
            request.OrganizationId,
            request.InvitedByIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<PartnerMemberDto>.Failure(authzResult.Error);
        }

        var memberUserId = request.UserId;
        if (memberUserId is null)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result<PartnerMemberDto>.Failure(
                    new Error("Partner.MemberIdentifierRequired",
                        "Provide the member's email address."));
            }

            memberUserId = await emailResolver
                .GetUserIdByEmailAsync(request.Email, cancellationToken)
                .ConfigureAwait(false);

            if (memberUserId is null)
            {
                return Result<PartnerMemberDto>.Failure(
                    new Error("Partner.UserNotFound",
                        "No Lagedra account exists with that email address. Ask them to sign up first, then add them."));
            }
        }
        else
        {
            // Guard the direct-id path too: without this check any GUID would be
            // accepted and the roster would show a phantom row that can never
            // resolve to a name or email.
            var accountEmail = await emailResolver
                .GetEmailAsync(memberUserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (accountEmail is null)
            {
                return Result<PartnerMemberDto>.Failure(
                    new Error("Partner.UserNotFound",
                        "No Lagedra account exists with that user id."));
            }
        }

        var alreadyMember = await dbContext.Members
            .AnyAsync(m => m.OrganizationId == request.OrganizationId
                        && m.UserId == memberUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyMember)
        {
            return Result<PartnerMemberDto>.Failure(
                new Error("Partner.AlreadyMember", "User is already a member of this organization."));
        }

        var member = PartnerMember.Create(
            request.OrganizationId, memberUserId.Value, request.Role,
            request.InvitedByUserId, clock);

        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PartnerMemberDto>.Success(
            new PartnerMemberDto(member.Id, member.OrganizationId, member.UserId,
                member.MemberRole, member.JoinedAt, member.InvitedBy));
    }
}
