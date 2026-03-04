using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

public static class StripeWebhookEndpoints
{
    public static IEndpointRouteBuilder MapStripeWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/webhooks/stripe", HandleStripeWebhook)
            .AllowAnonymous()
            .WithTags("Webhooks")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> HandleStripeWebhook(
        HttpRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(ct).ConfigureAwait(true);

        var signature = request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrEmpty(signature))
        {
            return Results.BadRequest(new { error = "Missing Stripe-Signature header" });
        }

        var result = await mediator.Send(
            new ProcessStripeWebhookCommand(payload, signature), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
