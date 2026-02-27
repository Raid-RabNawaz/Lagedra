using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/deals")
            .WithTags("Billing")
            .RequireAuthorization();

        group.MapGet("/{dealId:guid}/billing", GetBillingStatus);
        group.MapGet("/{dealId:guid}/proration-quote", GetProrationQuote);
        group.MapPost("/{dealId:guid}/stop-billing", StopBilling);

        return app;
    }

    private static async Task<IResult> GetBillingStatus(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetDealBillingStatusQuery(dealId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetProrationQuote(
        [FromRoute] Guid dealId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetProrationQuoteQuery(dealId, startDate, endDate), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> StopBilling(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new StopBillingCommand(dealId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
