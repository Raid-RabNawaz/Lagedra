using System.Security.Claims;
using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Application.Queries;
using Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;
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
        group.MapGet("/mine", ListMyApplications);
        group.MapPost("/{id:guid}/approve", ApproveApplication);
        group.MapPost("/{id:guid}/reject", RejectApplication);
        group.MapGet("/{id:guid}", GetApplication);
        group.MapGet("/listing/{listingId:guid}", ListApplicationsForListing);

        return app;
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
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new SubmitApplicationCommand(
                request.ListingId, request.TenantUserId, request.LandlordUserId,
                request.RequestedCheckIn, request.RequestedCheckOut), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/applications/{result.Value.ApplicationId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ApproveApplication(
        [FromRoute] Guid id,
        [FromBody] ApproveApplicationRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new ApproveDealApplicationCommand(id, request.DepositAmountCents), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> RejectApplication(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new RejectDealApplicationCommand(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetApplication(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetApplicationStatusQuery(id), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ListApplicationsForListing(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ListApplicationsForListingQuery(listingId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
