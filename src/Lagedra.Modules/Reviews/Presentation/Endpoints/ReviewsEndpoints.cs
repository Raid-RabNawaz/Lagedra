using System.Security.Claims;
using Lagedra.Modules.Reviews.Application.Commands;
using Lagedra.Modules.Reviews.Application.Queries;
using Lagedra.Modules.Reviews.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Reviews.Presentation.Endpoints;

public static class ReviewsEndpoints
{
    public static IEndpointRouteBuilder MapReviewsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var deals = app.MapGroup("/v1/deals/{dealId:guid}/reviews")
            .WithTags("Reviews")
            .RequireAuthorization();

        deals.MapGet("/", GetDealReviews);
        deals.MapPost("/", SubmitStayReview);

        var users = app.MapGroup("/v1/users/{userId:guid}")
            .WithTags("Reviews")
            .RequireAuthorization();

        users.MapGet("/reviews", GetUserReviews);
        users.MapGet("/reputation", GetUserReputation);

        var listings = app.MapGroup("/v1/listings/{listingId:guid}/reviews")
            .WithTags("Reviews");

        listings.MapGet("/", GetListingReviews).AllowAnonymous();

        var partners = app.MapGroup("/v1/partners/organizations/{orgId:guid}")
            .WithTags("Reviews")
            .RequireAuthorization();

        partners.MapGet("/reviews", ListPartnerReviews);
        partners.MapPost("/reviews", SubmitPartnerReview);
        partners.MapGet("/reputation", GetPartnerReputation);

        return app;
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static IResult ToError(SharedKernel.Results.Error? error) =>
        Results.Problem(
            detail: error?.Description ?? "Request failed.",
            statusCode: error?.Code?.Contains("Forbidden", StringComparison.Ordinal) == true
                ? StatusCodes.Status403Forbidden
                : error?.Code?.Contains("NotFound", StringComparison.Ordinal) == true
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest);

    private static async Task<IResult> GetDealReviews(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetDealReviewsQuery(dealId, GetUserId(user), user.IsInRole("PlatformAdmin")), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> SubmitStayReview(
        [FromRoute] Guid dealId,
        [FromBody] SubmitStayReviewRequest body,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await mediator.Send(new SubmitStayReviewCommand(
            dealId,
            GetUserId(user),
            body.OverallRating,
            body.PublicComment,
            body.PrivateFeedback,
            body.Cleanliness,
            body.Accuracy,
            body.Communication,
            body.Location,
            body.CheckIn,
            body.Value,
            body.RespectHouseRules), ct).ConfigureAwait(false);

        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> GetUserReviews(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserReviewsQuery(userId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> GetUserReputation(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserReputationQuery(userId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> GetListingReviews(
        [FromRoute] Guid listingId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetListingReviewsQuery(listingId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> ListPartnerReviews(
        [FromRoute] Guid orgId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ListPartnerServiceReviewsQuery(orgId), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> SubmitPartnerReview(
        [FromRoute] Guid orgId,
        [FromBody] SubmitPartnerServiceReviewRequest body,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);
        var result = await mediator.Send(new SubmitPartnerServiceReviewCommand(
            orgId,
            GetUserId(user),
            body.OverallRating,
            body.Responsiveness,
            body.Reliability,
            body.SupportQuality,
            body.PublicComment), ct).ConfigureAwait(false);

        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }

    private static async Task<IResult> GetPartnerReputation(
        [FromRoute] Guid orgId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var caller = GetUserId(user);
        var result = await mediator.Send(
            new GetPartnerReputationQuery(orgId, caller == Guid.Empty ? null : caller), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : ToError(result.Error);
    }
}
