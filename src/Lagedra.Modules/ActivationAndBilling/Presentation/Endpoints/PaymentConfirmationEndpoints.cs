using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;
using Lagedra.SharedKernel.Settings;
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
        group.MapPost("/dispute", DisputePayment);
        group.MapPost("/cancel", CancelBooking);
        group.MapPost("/damage-claim", FileDamageClaim);

        var admin = app.MapGroup("/v1/admin/deals/{dealId:guid}")
            .WithTags("PaymentConfirmation-Admin")
            .RequireAuthorization();

        admin.MapPost("/resolve-payment-dispute", ResolvePaymentDispute);

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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetPaymentStatus(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new GetPaymentConfirmationStatusQuery(dealId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ConfirmHostPlatformPayment(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new ConfirmHostPlatformPaymentCommand(dealId), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
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
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> CancelBooking(
        [FromRoute] Guid dealId,
        [FromBody] CancelBookingRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        IPlatformSettingsService settings,
        CancellationToken ct)
    {
        var userId = GetUserId(user);

        var result = await mediator
            .Send(new CancelBookingCommand(
                dealId, userId, request.Reason,
                FreeCancellationDays: 14,
                PartialRefundPercent: 50,
                PartialRefundDays: 7), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
