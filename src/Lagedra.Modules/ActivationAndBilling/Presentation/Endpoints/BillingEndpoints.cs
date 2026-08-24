using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;
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
        group.MapGet("/{dealId:guid}/rent-checkins", GetRentCheckIns);
        group.MapPost("/{dealId:guid}/rent-checkins/{checkInId:guid}/respond", RespondToRentCheckIn);

        var meGroup = app.MapGroup("/v1/me/billing")
            .WithTags("Billing")
            .RequireAuthorization();

        meGroup.MapGet("/statement", GetHostStatement);

        var adminGroup = app.MapGroup("/v1/admin")
            .WithTags("Billing")
            .RequireAuthorization("RequirePlatformAdmin");

        adminGroup.MapGet("/protocol-fee-reconciliation", GetProtocolFeeReconciliation);

        return app;
    }

    private static async Task<IResult> GetProtocolFeeReconciliation(
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetProtocolFeeReconciliationQuery(), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetHostStatement(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new GetHostBillingStatementQuery(userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
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

    private static async Task<IResult> GetRentCheckIns(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator.Send(new GetRentCheckInsQuery(dealId, userId, isAdmin), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> RespondToRentCheckIn(
        [FromRoute] Guid dealId,
        [FromRoute] Guid checkInId,
        [FromBody] RespondToRentCheckInRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new RespondToRentCheckInCommand(dealId, checkInId, userId, request.Received, request.Note), ct)
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
