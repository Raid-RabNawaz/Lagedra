using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class KycWebhookEndpoints
{
    public static IEndpointRouteBuilder MapKycWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/v1/webhooks/kyc", HandleKycWebhook)
            .WithTags("KycWebhook")
            .AllowAnonymous()
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> HandleKycWebhook(
        HttpContext httpContext,
        IKycProvider kycProvider,
        ILogger<IKycProvider> logger,
        CancellationToken ct)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var signature = httpContext.Request.Headers["X-Webhook-Signature"].FirstOrDefault()
            ?? httpContext.Request.Headers["Persona-Signature"].FirstOrDefault()
            ?? string.Empty;

        await kycProvider.HandleWebhookAsync(payload, signature, ct).ConfigureAwait(false);

        return Results.Ok();
    }
}
