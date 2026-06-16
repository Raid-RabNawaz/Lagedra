using System.Security.Claims;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Application.Commands;
using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Application.Queries;
using Lagedra.Modules.ChannelIntegration.Presentation.Contracts;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Modules.ChannelIntegration.Presentation.Endpoints;

public static class ChannelEndpoints
{
    public static IEndpointRouteBuilder MapChannelEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Host-facing surface: connect/manage external PMS accounts.
        var group = app.MapGroup("/v1/channels")
            .RequireAuthorization("RequireMember")
            .WithTags("Channels");

        group.MapGet("/providers", (IChannelProviderRegistry registry) =>
        {
            var providers = registry.All
                .Select(p => new ChannelProviderDto(p.ProviderKey))
                .ToList();
            return Results.Ok(providers);
        });

        group.MapGet("/", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new ListMyChannelConnectionsQuery(GetUserId(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/", async (
            ConnectChannelRequest req,
            ClaimsPrincipal user,
            ISender sender) =>
        {
            var result = await sender.Send(new ConnectChannelCommand(
                GetUserId(user), req.ProviderKey, req.ExternalAccountId,
                req.DisplayName, req.Username, req.Secret))
                .ConfigureAwait(false);

            return result.IsSuccess
                ? Results.Created($"/v1/channels/{result.Value.Id}", result.Value)
                : ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/enable", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new SetChannelStatusCommand(id, GetUserId(user), Enable: true))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/disable", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new SetChannelStatusCommand(id, GetUserId(user), Enable: false))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Public deep-link target handed to channels (e.g. OwnerRez "redirect"
        // mode) so an external listing page sends the guest to our listing.
        app.MapGet("/channels/{provider}/listing/{externalId}", async (
            string provider,
            string externalId,
            IConfiguration configuration,
            ISender sender) =>
        {
            var result = await sender.Send(new GetChannelListingRedirectQuery(provider, externalId))
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Results.NotFound(result.Error);
            }

            var baseUrl = configuration["App:FrontendUrl"] ?? "http://localhost:3000";
            return Results.Redirect($"{baseUrl}/listings/{result.Value}");
        }).AllowAnonymous().WithTags("Channels");

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IResult ToHttpResult<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : MapErrorToHttpResult(result.Error);

    private static IResult ToHttpResult(Result result) =>
        result.IsSuccess ? Results.NoContent() : MapErrorToHttpResult(result.Error);

    private static IResult MapErrorToHttpResult(Error error) =>
        error.Code switch
        {
            "Channel.NotFound" or
            "Channel.ListingNotMapped" => Results.NotFound(error),

            "Channel.AlreadyConnected" =>
                Results.Json(error, statusCode: StatusCodes.Status409Conflict),

            _ => Results.BadRequest(error)
        };
}
