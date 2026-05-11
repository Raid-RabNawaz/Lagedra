using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Partner-driven guest provisioning. The partner enters an email + name (and optional
/// listing); we create the user (or reuse an existing one), email them a set-password
/// link, write an audit row, and (when a listing is provided) create a
/// <see cref="DirectReservation"/>. If <see cref="WithEndorsement"/> is true, a
/// <see cref="PartnerEndorsement"/> is created in the auto-approved (Approved) state.
///
/// Authorization: caller must be a verified-org admin via
/// <see cref="IPartnerAccessService.RequireVerifiedOrgAdminAsync"/>.
/// </summary>
public sealed record InvitePartnerGuestCommand(
    Guid OrganizationId,
    string Email,
    string FullName,
    Guid? ListingId,
    bool WithEndorsement,
    string? EndorsementNote,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<PartnerGuestInviteResultDto>>;

public sealed record PartnerGuestInviteResultDto(
    Guid InviteId,
    Guid InvitedUserId,
    string Email,
    bool WasUserJustCreated,
    Uri? SetPasswordUrl,
    DateTime? SetPasswordTokenExpiresAt,
    Guid? EndorsementId,
    Guid? DirectReservationId);

public sealed partial class InvitePartnerGuestCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IIdentityInvitationService invitationService,
    IClock clock,
    ILogger<InvitePartnerGuestCommandHandler> logger)
    : IRequestHandler<InvitePartnerGuestCommand, Result<PartnerGuestInviteResultDto>>
{
    public async Task<Result<PartnerGuestInviteResultDto>> Handle(
        InvitePartnerGuestCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<PartnerGuestInviteResultDto>.Failure(
                new Error("Invite.EmailRequired", "Guest email is required."));
        }
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<PartnerGuestInviteResultDto>.Failure(
                new Error("Invite.FullNameRequired", "Guest full name is required."));
        }

        var authzResult = await accessService.RequireVerifiedOrgAdminAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);
        if (authzResult.IsFailure)
        {
            return Result<PartnerGuestInviteResultDto>.Failure(authzResult.Error);
        }

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (org is null)
        {
            return Result<PartnerGuestInviteResultDto>.Failure(PartnerAccessErrors.NotFound);
        }

        var inviteResult = await invitationService.CreateOrFindInvitedUserAsync(
            new InvitedUserRequest(
                request.Email.Trim(),
                request.FullName.Trim(),
                request.CallerUserId,
                org.Name),
            cancellationToken).ConfigureAwait(false);

        if (inviteResult.IsFailure)
        {
            return Result<PartnerGuestInviteResultDto>.Failure(inviteResult.Error);
        }

        var invitedUser = inviteResult.Value;

        Guid? endorsementId = null;
        if (request.WithEndorsement)
        {
            var existingActive = await dbContext.Endorsements
                .FirstOrDefaultAsync(
                    e => e.OrganizationId == request.OrganizationId
                      && e.TenantUserId == invitedUser.UserId
                      && (e.Status == PartnerEndorsementStatus.Requested
                       || e.Status == PartnerEndorsementStatus.Approved),
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingActive is not null)
            {
                endorsementId = existingActive.Id;
                LogEndorsementReused(logger, invitedUser.UserId, request.OrganizationId);
            }
            else
            {
                var endorsement = PartnerEndorsement.RequestAndApprove(
                    request.OrganizationId,
                    org.Name,
                    invitedUser.UserId,
                    request.CallerUserId,
                    request.EndorsementNote,
                    clock);
                dbContext.Endorsements.Add(endorsement);
                endorsementId = endorsement.Id;
            }
        }

        Guid? directReservationId = null;
        if (request.ListingId is { } listingId)
        {
            var reservation = DirectReservation.Create(
                request.OrganizationId,
                request.FullName.Trim(),
                request.Email.Trim(),
                listingId,
                request.CallerUserId,
                clock);
            dbContext.DirectReservations.Add(reservation);
            directReservationId = reservation.Id;
        }

        var invite = PartnerGuestInvite.Create(
            request.OrganizationId,
            request.CallerUserId,
            invitedUser.UserId,
            invitedUser.Email,
            request.FullName.Trim(),
            invitedUser.WasJustCreated,
            endorsementId,
            request.ListingId,
            clock);
        dbContext.GuestInvites.Add(invite);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogInviteRecorded(logger, invite.Id, invitedUser.UserId, request.OrganizationId,
            invitedUser.WasJustCreated, endorsementId, directReservationId);

        return Result<PartnerGuestInviteResultDto>.Success(new PartnerGuestInviteResultDto(
            invite.Id,
            invitedUser.UserId,
            invitedUser.Email,
            invitedUser.WasJustCreated,
            invitedUser.SetPasswordUrl,
            invitedUser.TokenExpiresAt,
            endorsementId,
            directReservationId));
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recorded partner guest invite {InviteId}: user={UserId}, org={OrgId}, justCreated={JustCreated}, endorsementId={EndorsementId}, reservationId={ReservationId}")]
    private static partial void LogInviteRecorded(
        ILogger logger,
        Guid inviteId,
        Guid userId,
        Guid orgId,
        bool justCreated,
        Guid? endorsementId,
        Guid? reservationId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Endorsement already active for tenant {UserId} from org {OrgId}; reusing existing endorsement.")]
    private static partial void LogEndorsementReused(ILogger logger, Guid userId, Guid orgId);
}
