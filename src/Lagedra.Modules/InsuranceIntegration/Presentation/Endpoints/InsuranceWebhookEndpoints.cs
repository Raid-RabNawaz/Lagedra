using Lagedra.Modules.InsuranceIntegration.Application.Commands;
using Lagedra.Modules.InsuranceIntegration.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.InsuranceIntegration.Presentation.Endpoints;

public static class InsuranceWebhookEndpoints
{
    public static IEndpointRouteBuilder MapInsuranceWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/webhooks/insurance")
            .WithTags("Insurance Webhooks");

        group.MapPost("/purchase", HandlePurchaseWebhook);

        return app;
    }

    private static async Task<IResult> HandlePurchaseWebhook(
        [FromBody] InsurancePurchaseWebhookRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new HandleInsurancePurchaseWebhookCommand(
                request.DealId,
                request.Provider,
                request.PolicyNumber,
                request.CoverageScope,
                request.PolicyExpiresAt,
                request.RawPayload),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
