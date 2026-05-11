using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using Lagedra.SharedKernel.Results;
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
        var userId = GetUserId(user);

        var result = await mediator
            .Send(new CreateCheckoutPaymentIntentCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ConfirmCheckout(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);

        var result = await mediator
            .Send(new ConfirmCheckoutPaymentCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetCheckoutStatus(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);

        var result = await mediator
            .Send(new GetCheckoutStatusQuery(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    private static IResult ToErrorResult(Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };

        if (error.Code.EndsWith(".Forbidden", StringComparison.Ordinal))
        {
            return Results.Json(payload, statusCode: StatusCodes.Status403Forbidden);
        }

        return error.Code switch
        {
            "Checkout.NoApplication" or "Checkout.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
