using System.Security.Claims;
using Lagedra.Modules.PartnerNetwork.Application.Commands;
using Lagedra.Modules.PartnerNetwork.Application.Queries;
using Lagedra.Modules.PartnerNetwork.Presentation.Contracts;
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

        var group = app.MapGroup("/v1/partners")
            .RequireAuthorization()
            .WithTags("Partners");

        group.MapPost("/", async (RegisterPartnerRequest req, ClaimsPrincipal user,
            ISender sender) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await sender.Send(new RegisterPartnerOrganizationCommand(
                req.Name, req.OrganizationType, req.ContactEmail, req.TaxId, userId))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/v1/partners/{result.Value.Id}", result.Value)
                                    : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetPartnerOrganizationQuery(id))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        group.MapPost("/{id:guid}/verify", async (Guid id, ClaimsPrincipal user,
            ISender sender) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await sender.Send(new VerifyPartnerOrganizationCommand(id, userId))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequirePlatformAdmin");

        group.MapPost("/{id:guid}/members", async (Guid id, AddMemberRequest req,
            ClaimsPrincipal user, ISender sender) =>
        {
            var invitedBy = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await sender.Send(
                new AddPartnerMemberCommand(id, req.UserId, req.Role, invitedBy))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/v1/partners/{id}/members", result.Value)
                                    : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}/members", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ListPartnerMembersQuery(id))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/referral-links", async (Guid id,
            GenerateReferralLinkRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await sender.Send(
                new GenerateReferralLinkCommand(id, userId, req.ExpiresAt, req.MaxUses))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/v1/partners/{id}/referral-links", result.Value)
                                    : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}/referral-links", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ListReferralLinksQuery(id))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/{id:guid}/reservations", async (Guid id,
            CreateReservationRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await sender.Send(
                new CreateDirectReservationCommand(id, req.GuestName, req.GuestEmail,
                    req.ListingId, userId))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Created($"/v1/partners/{id}/reservations", result.Value)
                                    : Results.BadRequest(result.Error);
        });

        app.MapPost("/v1/referral/{code}/redeem", async (string code,
            ClaimsPrincipal user, ISender sender) =>
        {
            var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await sender.Send(new RedeemReferralLinkCommand(code, userId))
                .ConfigureAwait(false);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization().WithTags("Partners");

        return app;
    }
}
