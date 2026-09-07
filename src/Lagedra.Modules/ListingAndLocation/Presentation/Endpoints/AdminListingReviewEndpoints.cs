using Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;
using Lagedra.Modules.ListingAndLocation.Application.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;

public static class AdminListingReviewEndpoints
{
    public static IEndpointRouteBuilder MapAdminListingReviewEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/listings")
            .WithTags("Admin - Listing Review")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/pending-review", GetPendingReview);
        group.MapPost("/approve-bulk", BulkApproveListings);
        group.MapPost("/deny-bulk", BulkDenyListings);
        group.MapPost("/{listingId:guid}/approve", ApproveListing);
        group.MapPost("/{listingId:guid}/deny", DenyListing);

        return app;
    }

    private static async Task<IResult> GetPendingReview(
        IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListListingsForReviewQuery(), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> BulkApproveListings(
        [FromBody] BulkApproveListingsRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var adminUserId = GetUserId(httpContext);
        var result = await mediator.Send(
            new BulkApproveListingsCommand(request.ListingIds ?? [], adminUserId),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> BulkDenyListings(
        [FromBody] BulkDenyListingsRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var adminUserId = GetUserId(httpContext);
        var result = await mediator.Send(
            new BulkDenyListingsCommand(request.ListingIds ?? [], adminUserId, request.Reason ?? string.Empty),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ApproveListing(
        [FromRoute] Guid listingId,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetUserId(httpContext);
        var result = await mediator.Send(
            new ApproveListingCommand(listingId, adminUserId), cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> DenyListing(
        [FromRoute] Guid listingId,
        [FromBody] DenyListingRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var adminUserId = GetUserId(httpContext);
        var result = await mediator.Send(
            new DenyListingCommand(listingId, adminUserId, request.Reason),
            cancellationToken).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }

    private static IResult ToErrorResult(Lagedra.SharedKernel.Results.Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };
        return error.Code switch
        {
            "Listing.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}

public sealed record DenyListingRequest(string Reason);

public sealed record BulkApproveListingsRequest(IReadOnlyList<Guid>? ListingIds);

public sealed record BulkDenyListingsRequest(IReadOnlyList<Guid>? ListingIds, string Reason);
