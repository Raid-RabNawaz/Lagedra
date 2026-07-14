using System.Security.Claims;
using Lagedra.Modules.PartnerNetwork.Application.Commands;
using Lagedra.Modules.PartnerNetwork.Application.Queries;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Presentation.Contracts;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.PartnerNetwork.Presentation.Endpoints;

public static class PartnerEndpoints
{
    public static IEndpointRouteBuilder MapPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Partner-facing surface. Anyone with role "InstitutionPartner" or "PlatformAdmin"
        // may attempt to enter; the per-organization authorization (member / admin-member /
        // verified-org-admin) is enforced inside each handler via IPartnerAccessService.
        var group = app.MapGroup("/v1/partners")
            .RequireAuthorization("RequireInstitutionPartner")
            .WithTags("Partners");

        group.MapPost("/", async (
            RegisterPartnerRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var userId = GetUserId(user);
            var result = await sender.Send(new RegisterPartnerOrganizationCommand(
                req.Name, req.OrganizationType, req.ContactEmail, req.TaxId, userId,
                req.EndorsementTermsAccepted))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/partners/{result.Value.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapGet("/me", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            var result = await sender.Send(new GetMyPartnerOrganizationQuery(userId))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new GetPartnerOrganizationQuery(
                id, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/verify", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var userId = GetUserId(user);
            var result = await sender.Send(new VerifyPartnerOrganizationCommand(id, userId))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        }).RequireAuthorization("RequirePlatformAdmin");

        group.MapPost("/{id:guid}/members", async (
            Guid id,
            AddMemberRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new AddPartnerMemberCommand(
                id, req.UserId, req.Role, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/partners/{id}/members/{result.Value.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapGet("/{id:guid}/members", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new ListPartnerMembersQuery(
                id, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/referral-links", async (
            Guid id,
            GenerateReferralLinkRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new GenerateReferralLinkCommand(
                id, GetUserId(user), IsPlatformAdmin(user), req.ExpiresAt, req.MaxUses))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/partners/{id}/referral-links/{result.Value.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapGet("/{id:guid}/referral-links", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new ListReferralLinksQuery(
                id, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/referral-links/{linkId:guid}/deactivate", async (
            Guid id,
            Guid linkId,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new DeactivateReferralLinkCommand(
                id, linkId, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/reservations", async (
            Guid id,
            CreateReservationRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new CreateDirectReservationCommand(
                id,
                req.TenantUserId,
                req.ListingId,
                req.PayerType,
                GetUserId(user),
                IsPlatformAdmin(user),
                req.RequestedCheckIn,
                req.RequestedCheckOut,
                req.StripePaymentMethodId))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/partners/{id}/reservations/{result.Value.Reservation.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapGet("/{id:guid}/reservations", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            string? status,
            int? skip,
            int? take) =>
        {
            var statusFilter = ParseReservationStatus(status);
            var result = await sender.Send(new ListDirectReservationsQuery(
                id, GetUserId(user), IsPlatformAdmin(user),
                statusFilter, skip ?? 0, take ?? 50))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/setup-intent", async (
            Guid id,
            CreatePartnerSetupIntentRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new CreatePartnerBookingSetupIntentCommand(
                id, req.ListingId, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapGet("/{id:guid}/endorsed-members", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new ListEndorsedMembersQuery(
                id, GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Endorsements (partner-side surface).
        group.MapPost("/{id:guid}/endorsements", async (
            Guid id,
            RequestEndorsementRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new RequestPartnerEndorsementCommand(
                id, req.TenantUserId, GetUserId(user), IsPlatformAdmin(user),
                RequestPartnerEndorsementCallerKind.Partner, req.Note))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/partners/{id}/endorsements/{result.Value.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapGet("/{id:guid}/endorsements", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            string? status,
            int? skip,
            int? take) =>
        {
            var statusFilter = ParseEndorsementStatus(status);
            var result = await sender.Send(new ListPartnerEndorsementsQuery(
                id, GetUserId(user), IsPlatformAdmin(user),
                statusFilter, skip ?? 0, take ?? 50))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/endorsements/{endorsementId:guid}/approve", async (
            Guid id,
            Guid endorsementId,
            ApproveEndorsementRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new ApprovePartnerEndorsementCommand(
                id, endorsementId, GetUserId(user), IsPlatformAdmin(user), req.Note))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/endorsements/{endorsementId:guid}/revoke", async (
            Guid id,
            Guid endorsementId,
            RevokeEndorsementRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new RevokePartnerEndorsementCommand(
                id, endorsementId, GetUserId(user), IsPlatformAdmin(user), req.Reason))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Partner-driven guest provisioning (Phase 18.4).
        group.MapPost("/{id:guid}/invites", async (
            Guid id,
            InvitePartnerGuestRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new InvitePartnerGuestCommand(
                id, req.Email, req.FullName,
                req.WithEndorsement, req.EndorsementNote,
                GetUserId(user), IsPlatformAdmin(user)))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/partners/{id}/invites/{result.Value.InviteId}", result.Value)
                : ToHttpResult(result);
        });

        // Public-ish discovery: any authenticated user can search verified partners by name
        // so they can request an endorsement. Returns a minimal projection only.
        app.MapGet("/v1/partners/discover", async (
            ISender sender,
            string? search,
            int? take) =>
        {
            var result = await sender.Send(new DiscoverVerifiedPartnersQuery(search, take ?? 25))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        }).RequireAuthorization().WithTags("Partners");

        // Tenant-facing endorsement surface ("/v1/me/partner-endorsements").
        var tenantGroup = app.MapGroup("/v1/me/partner-endorsements")
            .RequireAuthorization()
            .WithTags("Partners (Tenant)");

        tenantGroup.MapPost("/", async (
            RequestEndorsementByTenantRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var userId = GetUserId(user);
            var result = await sender.Send(new RequestPartnerEndorsementCommand(
                req.OrganizationId, userId, userId, IsPlatformAdmin(user),
                RequestPartnerEndorsementCallerKind.Tenant, req.Note))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created(
                    $"/v1/partners/{req.OrganizationId}/endorsements/{result.Value.Id}",
                    result.Value)
                : ToHttpResult(result);
        });

        tenantGroup.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            var result = await sender.Send(new GetTenantEndorsementsQuery(
                userId, userId, IsPlatformAdmin(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Admin-only partner-management surface.
        var adminGroup = app.MapGroup("/v1/admin/partners")
            .RequireAuthorization("RequirePlatformAdmin")
            .WithTags("Partners (Admin)");

        adminGroup.MapGet("/", async (
            ISender sender,
            string? status,
            string? search,
            int? skip,
            int? take) =>
        {
            var statusFilter = ParseOrgStatus(status);
            var result = await sender.Send(new ListPartnerOrganizationsQuery(
                statusFilter, search, skip ?? 0, take ?? 50))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        adminGroup.MapGet("/pending", async (
            ISender sender,
            int? skip,
            int? take) =>
        {
            var result = await sender.Send(new ListPartnerOrganizationsQuery(
                PartnerOrganizationStatus.PendingVerification, null, skip ?? 0, take ?? 50))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        adminGroup.MapPost("/{id:guid}/suspend", async (
            Guid id,
            SuspendPartnerRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new SuspendPartnerOrganizationCommand(
                id, req.Reason, GetUserId(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Referral redemption is open to any authenticated user; per-deal authorization
        // is implicit (one-shot per user via partner.referral_redemptions unique constraint).
        app.MapPost("/v1/referral/{code}/redeem", async (
            string code,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var userId = GetUserId(user);
            var result = await sender.Send(new RedeemReferralLinkCommand(code, userId))
                .ConfigureAwait(false);
            return result.IsSuccess
                ? Results.Ok()
                : ToHttpResult(result);
        }).RequireAuthorization().WithTags("Partners");

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static bool IsPlatformAdmin(ClaimsPrincipal user) =>
        user.IsInRole("PlatformAdmin");

    private static DirectReservationStatusFilter ParseReservationStatus(string? raw) =>
        raw?.ToUpperInvariant() switch
        {
            "PENDING" => DirectReservationStatusFilter.Pending,
            "LINKED" => DirectReservationStatusFilter.Linked,
            _ => DirectReservationStatusFilter.All
        };

    private static PartnerOrganizationStatus? ParseOrgStatus(string? raw) =>
        raw?.ToUpperInvariant() switch
        {
            "PENDING" or "PENDINGVERIFICATION" => PartnerOrganizationStatus.PendingVerification,
            "VERIFIED" => PartnerOrganizationStatus.Verified,
            "SUSPENDED" => PartnerOrganizationStatus.Suspended,
            _ => null
        };

    private static PartnerEndorsementStatus? ParseEndorsementStatus(string? raw) =>
        raw?.ToUpperInvariant() switch
        {
            "REQUESTED" => PartnerEndorsementStatus.Requested,
            "APPROVED" => PartnerEndorsementStatus.Approved,
            "REVOKED" => PartnerEndorsementStatus.Revoked,
            "EXPIRED" => PartnerEndorsementStatus.Expired,
            _ => null
        };

    private static IResult ToHttpResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return MapErrorToHttpResult(result.Error);
    }

    private static IResult ToHttpResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return MapErrorToHttpResult(result.Error);
    }

    private static IResult MapErrorToHttpResult(Error error) =>
        error.Code switch
        {
            "Partner.NotFound" or
            "Partner.NoMembership" or
            "Referral.NotFound" or
            "Endorsement.NotFound" => Results.NotFound(error),

            "Partner.Forbidden" or
            "Partner.AdminRequired" or
            "Partner.OrgNotVerified" or
            "Partner.OrgSuspended" => Results.Json(error, statusCode: StatusCodes.Status403Forbidden),

            _ => Results.BadRequest(error)
        };
}
