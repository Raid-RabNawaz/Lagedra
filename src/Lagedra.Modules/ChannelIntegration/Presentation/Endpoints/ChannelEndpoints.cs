using System.Security.Claims;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Application.Commands;
using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Application.Queries;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
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

        group.MapGet("/providers", (IChannelProviderRegistry registry, OwnerRezOAuthFlow ownerRezOAuth) =>
        {
            var providers = registry.All
                .Select(p => new ChannelProviderDto(
                    p.ProviderKey,
                    UsesOAuth: p.ProviderKey == OwnerRezOAuthFlow.ProviderKey && ownerRezOAuth.IsConfigured))
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

        // OwnerRez is linked by authorizing Lagedra's OAuth app rather than by
        // pasting a key, so connecting is a redirect the host is sent on.
        group.MapPost("/ownerrez/oauth/start", async (ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new StartOwnerRezOAuthCommand(GetUserId(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/enable", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new SetChannelStatusCommand(id, GetUserId(user), Enable: true))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Pull the host's listings from the provider now (rather than waiting for
        // the scheduled job) and import them into Lagedra as drafts.
        group.MapPost("/{id:guid}/sync", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new SyncChannelConnectionCommand(id, GetUserId(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // The listings pulled for a connection + their imported Lagedra ids.
        group.MapGet("/{id:guid}/listings", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new ListChannelListingsQuery(id, GetUserId(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        group.MapPost("/{id:guid}/disable", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new SetChannelStatusCommand(id, GetUserId(user), Enable: false))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Disconnect the account so the host can link a different one for this
        // provider. Imported listings are kept; the credentials are destroyed.
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender) =>
        {
            var result = await sender.Send(new DisconnectChannelCommand(id, GetUserId(user)))
                .ConfigureAwait(false);
            return ToHttpResult(result);
        });

        // Where OwnerRez returns the host after the consent screen. Anonymous
        // because it arrives as a top-level browser redirect carrying no token —
        // the signed state parameter is what identifies the host. Always ends in a
        // redirect back to the SPA, since the host is looking at this response.
        app.MapGet("/v1/channels/ownerrez/oauth/callback", async (
            string? code,
            string? state,
            string? error,
            IConfiguration configuration,
            ISender sender) =>
        {
            var appUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var channels = $"{appUrl}/app/channels";

            if (!string.IsNullOrWhiteSpace(error))
            {
                // access_denied means the host chose not to authorize, which is a
                // normal outcome rather than something that went wrong.
                var outcome = string.Equals(error, "access_denied", StringComparison.OrdinalIgnoreCase)
                    ? "denied"
                    : "error";
                return Results.Redirect($"{channels}?ownerrez={outcome}");
            }

            var result = await sender.Send(new CompleteOwnerRezOAuthCommand(code, state))
                .ConfigureAwait(false);

            return Results.Redirect(result.IsSuccess
                ? $"{channels}?ownerrez=connected&connection={result.Value}"
                : $"{channels}?ownerrez=error&reason={Uri.EscapeDataString(result.Error.Code)}");
        }).AllowAnonymous().WithTags("Channels");

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

            "Channel.ProviderAlreadyConnected" =>
                Results.Json(error, statusCode: StatusCodes.Status409Conflict),

            _ => Results.BadRequest(error)
        };
}
