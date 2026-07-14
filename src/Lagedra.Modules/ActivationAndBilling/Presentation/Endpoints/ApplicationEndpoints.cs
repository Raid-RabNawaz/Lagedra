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

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/applications")
            .WithTags("Applications")
            .RequireAuthorization();

        group.MapPost("/", SubmitApplication);
        group.MapPost("/setup-intent", CreateBookingSetupIntent);
        group.MapGet("/preview", GetReservationPreview);
        group.MapGet("/mine", ListMyApplications);
        group.MapPost("/{id:guid}/approve", ApproveApplication);
        group.MapPost("/{id:guid}/reject", RejectApplication);
        group.MapPost("/{id:guid}/attach-payment", AttachApplicationPayment);
        group.MapGet("/{id:guid}", GetApplication);
        group.MapGet("/listing/{listingId:guid}", ListApplicationsForListing);

        return app;
    }

    private static async Task<IResult> CreateBookingSetupIntent(
        [FromBody] CreateBookingSetupIntentRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var tenantUserId = GetUserId(user);
        var result = await mediator
            .Send(new CreateBookingSetupIntentCommand(tenantUserId, request.ListingId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetReservationPreview(
        [FromQuery] Guid listingId,
        [FromQuery] DateOnly checkIn,
        [FromQuery] DateOnly checkOut,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var tenantUserId = GetUserId(user);
        var result = await mediator
            .Send(new GetReservationPreviewQuery(listingId, tenantUserId, checkIn, checkOut), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListMyApplications(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new ListMyApplicationsQuery(userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SubmitApplication(
        [FromBody] SubmitApplicationRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var tenantUserId = GetUserId(user);
        var result = await mediator.Send(
            new SubmitApplicationCommand(
                request.ListingId, tenantUserId,
                request.RequestedCheckIn, request.RequestedCheckOut,
                request.GuestCount, request.Message,
                request.StripePaymentMethodId,
                request.TruthSurfaceConsentGiven,
                request.ConsentVersion,
                GetClientIp(httpContext),
                GetUserAgent(httpContext)), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created(
                $"/v1/applications/{result.Value.Application.ApplicationId}",
                result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ApproveApplication(
        [FromRoute] Guid id,
        [FromBody] ApproveApplicationRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetUserId(user);
        var result = await mediator.Send(
            new ApproveDealApplicationCommand(
                id,
                userId,
                request.TruthSurfaceConsentGiven,
                request.ConsentVersion,
                GetClientIp(httpContext),
                GetUserAgent(httpContext)), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> RejectApplication(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new RejectDealApplicationCommand(id, userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> AttachApplicationPayment(
        [FromRoute] Guid id,
        [FromBody] AttachApplicationPaymentRequest request,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetUserId(user);
        var result = await mediator.Send(
            new AttachApplicationPaymentCommand(
                id,
                userId,
                request.StripePaymentMethodId,
                request.TruthSurfaceConsentGiven,
                request.ConsentVersion,
                GetClientIp(httpContext),
                GetUserAgent(httpContext)), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetApplication(
        [FromRoute] Guid id,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator.Send(new GetApplicationStatusQuery(id, userId, isAdmin), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListApplicationsForListing(
        [FromRoute] Guid listingId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new ListApplicationsForListingQuery(listingId, userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    private static string? GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext httpContext)
    {
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua)
            ? null
            : (ua.Length > 512 ? ua[..512] : ua);
    }

    private static IResult ToErrorResult(Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };
        return error.Code switch
        {
            "Application.Forbidden" or "Application.OwnListing" =>
                Results.Json(payload, statusCode: StatusCodes.Status403Forbidden),
            "Application.NotFound" or "Listing.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
