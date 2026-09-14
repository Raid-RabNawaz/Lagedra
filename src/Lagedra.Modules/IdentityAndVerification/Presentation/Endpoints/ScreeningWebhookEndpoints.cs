using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class ScreeningWebhookEndpoints
{
    public static IEndpointRouteBuilder MapScreeningWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/v1/webhooks/screening/{provider}", Handle)
            .WithTags("ScreeningWebhook")
            .AllowAnonymous()
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> Handle(
        string provider,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var headers = httpContext.Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        await mediator.Send(new ProcessScreeningWebhookCommand(provider, payload, headers), ct)
            .ConfigureAwait(false);

        return Results.Ok();
    }
}
