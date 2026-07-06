using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;
using Lagedra.Infrastructure.Middleware;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ActivationAndBilling.Presentation.Endpoints;

public static class PaymentConfirmationEndpoints
{
    public static IEndpointRouteBuilder MapPaymentConfirmationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/deals/{dealId:guid}/payment")
            .WithTags("PaymentConfirmation")
            .RequireAuthorization();

        group.MapGet("/details", GetPaymentDetails);
        group.MapGet("/status", GetPaymentStatus);
        group.MapPost("/confirm", ConfirmPayment);
        group.MapPost("/confirm-platform-payment", ConfirmHostPlatformPayment);
        group.MapPost("/dispute", DisputePayment)
            .RequireRateLimiting(RateLimitingSetup.DisputeCapPolicy);
        group.MapPost("/cancel", CancelBooking);
        group.MapPost("/damage-claim", FileDamageClaim);

        // Deposit return handshake (non-custodial, host-held).
        group.MapPost("/begin-move-out", BeginMoveOut);
        group.MapPost("/deposit-return/host-confirm", ConfirmDepositReturnedByHost);
        group.MapPost("/deposit-return/tenant-confirm", ConfirmDepositReceivedByTenant);

        var admin = app.MapGroup("/v1/admin/deals/{dealId:guid}")
            .WithTags("PaymentConfirmation-Admin")
            .RequireAuthorization();

        admin.MapPost("/resolve-payment-dispute", ResolvePaymentDispute);
        admin.MapPost("/force-deposit-return", ForceDepositReturn)
            .RequireAuthorization("RequirePlatformAdmin");

        return app;
    }

    private static async Task<IResult> GetPaymentDetails(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new GetPaymentDetailsForTenantQuery(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetPaymentStatus(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator
            .Send(new GetPaymentConfirmationStatusQuery(dealId, userId, isAdmin), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ConfirmPayment(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ConfirmPaymentCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ConfirmHostPlatformPayment(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ConfirmHostPlatformPaymentCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> DisputePayment(
        [FromRoute] Guid dealId,
        [FromBody] DisputePaymentRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new DisputePaymentCommand(dealId, userId, request.Reason, request.EvidenceManifestId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ResolvePaymentDispute(
        [FromRoute] Guid dealId,
        [FromBody] ResolvePaymentDisputeRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ResolvePaymentDisputeCommand(dealId, request.PaymentValid, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> FileDamageClaim(
        [FromRoute] Guid dealId,
        [FromBody] FileDamageClaimRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new FileDamageClaimCommand(
                dealId, userId, request.Description,
                request.ClaimedAmountCents, request.EvidenceManifestId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> CancelBooking(
        [FromRoute] Guid dealId,
        [FromBody] CancelBookingRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);

        var result = await mediator
            .Send(new CancelBookingCommand(dealId, userId, request.Reason), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> BeginMoveOut(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator
            .Send(new BeginMoveOutCommand(dealId, userId, isAdmin), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ConfirmDepositReturnedByHost(
        [FromRoute] Guid dealId,
        [FromBody] ConfirmDepositReturnRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ConfirmDepositReturnedByHostCommand(
                dealId, userId, request.ReturnedAmountCents, request.Method, request.Note), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ConfirmDepositReceivedByTenant(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ConfirmDepositReceivedByTenantCommand(dealId, userId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ForceDepositReturn(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ForceDepositReturnCommand(dealId, userId), ct)
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
            "PaymentConfirmation.NotFound"
                or "Cancel.DealNotFound"
                or "DamageClaim.DealNotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
