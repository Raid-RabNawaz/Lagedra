using Lagedra.Modules.ChannelIntegration.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ChannelIntegration.Presentation.Endpoints;

public static class HostawayWebhookEndpoints
{
    public static IEndpointRouteBuilder MapHostawayWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/v1/webhooks/hostaway", HandleHostawayWebhook)
            .WithTags("ChannelWebhooks")
            .AllowAnonymous()
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> HandleHostawayWebhook(
        HttpRequest request,
        ISender sender,
        CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        var authorization = request.Headers.Authorization.ToString();

        var result = await sender
            .Send(new ProcessHostawayWebhookCommand(authorization, payload), ct)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Channel.WebhookUnauthorized" => Results.Unauthorized(),
                "Channel.WebhookInvalidPayload" => Results.BadRequest(result.Error),
                _ => Results.BadRequest(result.Error),
            };
        }

        // Hostaway expects a 2xx within 30s; return quickly after reconcile.
        return Results.Ok();
    }
}
