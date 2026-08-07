using Lagedra.Modules.ChannelIntegration.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ChannelIntegration.Presentation.Endpoints;

public static class OwnerRezWebhookEndpoints
{
    public static IEndpointRouteBuilder MapOwnerRezWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Anonymous at the framework level; the delivery is authenticated by the
        // Basic credentials configured on the OwnerRez OAuth app.
        app.MapPost("/v1/webhooks/ownerrez", HandleOwnerRezWebhook)
            .WithTags("ChannelWebhooks")
            .AllowAnonymous()
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> HandleOwnerRezWebhook(
        HttpRequest request,
        ISender sender,
        CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        var authorization = request.Headers.Authorization.ToString();

        var result = await sender
            .Send(new ProcessOwnerRezWebhookCommand(authorization, payload), ct)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Channel.WebhookUnauthorized" => Results.Unauthorized(),
                _ => Results.BadRequest(result.Error),
            };
        }

        // OwnerRez waits two seconds for a 2xx and retries ten times otherwise, so
        // the handler above does only cheap work before this returns.
        return Results.Ok();
    }
}
