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
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator.Send(new GetDealBillingStatusQuery(dealId, userId, isAdmin), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetProrationQuote(
        [FromRoute] Guid dealId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator.Send(
            new GetProrationQuoteQuery(dealId, userId, startDate, endDate, isAdmin), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> StopBilling(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator.Send(new StopBillingCommand(dealId, userId, isAdmin), ct)
            .ConfigureAwait(true);

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
            "BillingAccount.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
