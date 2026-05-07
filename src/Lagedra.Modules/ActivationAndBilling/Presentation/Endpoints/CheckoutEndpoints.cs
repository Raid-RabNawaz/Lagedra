using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/deals")
            .WithTags("Checkout")
            .RequireAuthorization();

        group.MapPost("/{dealId:guid}/checkout", CreateCheckout);
        group.MapPost("/{dealId:guid}/checkout/confirm", ConfirmCheckout);
        group.MapGet("/{dealId:guid}/checkout/status", GetCheckoutStatus);

        return app;
    }

    private static async Task<IResult> CreateCheckout(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

        var result = await mediator
            .Send(new CreateCheckoutPaymentIntentCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ConfirmCheckout(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

        var result = await mediator
            .Send(new ConfirmCheckoutPaymentCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetCheckoutStatus(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

        var result = await mediator
            .Send(new GetCheckoutStatusQuery(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
