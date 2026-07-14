using System.Security.Claims;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Lagedra.Modules.StructuredInquiry.Application.Commands;
using Lagedra.Modules.StructuredInquiry.Application.Queries;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Presentation.Contracts;

namespace Lagedra.Modules.StructuredInquiry.Presentation.Endpoints;

public static class InquiryEndpoints
{
    public static IEndpointRouteBuilder MapInquiryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/inquiries")
            .WithTags("StructuredInquiry")
            .RequireAuthorization();

        group.MapPost("/{dealId:guid}/unlock-request", RequestDetailUnlock);
        group.MapPost("/{dealId:guid}/approve-unlock", ApproveInquiryUnlock);
        group.MapPost("/{dealId:guid}/lock", LockInquirySession);
        group.MapPost("/{dealId:guid}/questions", SubmitInquiryQuestion);
        group.MapPost("/{dealId:guid}/answers", SubmitLandlordResponse);
        // Inquiries close automatically when the Truth Surface is confirmed.
        // Manual close is restricted to platform admins (e.g. dispute support).
        group.MapPost("/{dealId:guid}/close", CloseInquiry)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapGet("/{dealId:guid}", GetInquiryThread);
        group.MapGet("/predefined-questions", ListPredefinedQuestions);

        // Phase 17 — session-id-based routes that work for both pre-booking
        // and deal-linked threads. Pre-booking sessions have no deal id, so
        // the deal-id-based routes above can't address them.
        var sessions = app.MapGroup("/v1/inquiry-sessions")
            .WithTags("StructuredInquiry")
            .RequireAuthorization();

        sessions.MapGet("/host", ListHostInquiries);
        sessions.MapGet("/mine", ListMyTenantInquiries);
        sessions.MapGet("/partner", ListPartnerInquiries);
        sessions.MapGet("/{sessionId:guid}", GetInquiryBySession);
        sessions.MapPost("/{sessionId:guid}/questions", SubmitQuestionToSession);
        sessions.MapPost("/{sessionId:guid}/answers", SubmitResponseToSession);
        sessions.MapPost("/{sessionId:guid}/offers", ProposeOffer);
        sessions.MapPost("/{sessionId:guid}/offers/{offerId:guid}/accept", AcceptOffer);
        sessions.MapPost("/{sessionId:guid}/offers/{offerId:guid}/counter", CounterOffer);
        sessions.MapPost("/{sessionId:guid}/offers/accepted/withdraw", WithdrawAcceptedOffer);
        sessions.MapPost("/{sessionId:guid}/partner", AddPartner);
        sessions.MapDelete("/{sessionId:guid}/partner", RemovePartner);

        // Phase 17 — listing-scoped pre-booking inquiry endpoints.
        var listings = app.MapGroup("/v1/listings/{listingId:guid}/inquiry")
            .WithTags("StructuredInquiry")
            .RequireAuthorization();

        listings.MapPost("", StartListingInquiry);
        listings.MapGet("/mine", GetMyListingInquiry);
        listings.MapPost("/partner", StartPartnerListingInquiry);

        return app;
    }

    private static async Task<IResult> RequestDetailUnlock(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new RequestDetailUnlockCommand(dealId, userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiries/{dealId}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ApproveInquiryUnlock(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ApproveInquiryUnlockCommand(dealId, userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> LockInquirySession(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new LockInquirySessionCommand(dealId, userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> SubmitInquiryQuestion(
        [FromRoute] Guid dealId,
        [FromBody] SubmitInquiryQuestionRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new SubmitInquiryQuestionCommand(
                dealId, userId,
                request.Category, request.PredefinedQuestionId,
                request.CustomQuestionText, request.OpenQuestionText),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiries/{dealId}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> SubmitLandlordResponse(
        [FromRoute] Guid dealId,
        [FromBody] SubmitLandlordResponseRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new SubmitLandlordResponseCommand(
                dealId, userId,
                request.QuestionId, request.ResponseType, request.AnswerValue),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> CloseInquiry(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CloseInquiryOnTruthSurfaceConfirmationCommand(dealId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.NoContent()
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetInquiryThread(
        [FromRoute] Guid dealId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator
            .Send(new GetInquiryThreadQuery(dealId, userId, isAdmin), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListPredefinedQuestions(
        [FromQuery] InquiryCategory? category,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListPredefinedQuestionsQuery(category), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> StartListingInquiry(
        [FromRoute] Guid listingId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new StartListingInquiryCommand(listingId, userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiry-sessions/{result.Value.SessionId}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetMyListingInquiry(
        [FromRoute] Guid listingId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new GetMyListingInquiryQuery(listingId, userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListHostInquiries(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ListMyHostInquiriesQuery(userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListMyTenantInquiries(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ListMyTenantInquiriesQuery(userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetInquiryBySession(
        [FromRoute] Guid sessionId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var isAdmin = user.IsInRole("PlatformAdmin");
        var result = await mediator
            .Send(new GetInquiryBySessionIdQuery(sessionId, userId, isAdmin), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> SubmitQuestionToSession(
        [FromRoute] Guid sessionId,
        [FromBody] SubmitInquiryQuestionRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new SubmitQuestionToSessionCommand(
                sessionId, userId,
                request.Category, request.PredefinedQuestionId,
                request.CustomQuestionText, request.OpenQuestionText),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiry-sessions/{sessionId}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> SubmitResponseToSession(
        [FromRoute] Guid sessionId,
        [FromBody] SubmitLandlordResponseRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new SubmitResponseToSessionCommand(
                sessionId, userId,
                request.QuestionId, request.ResponseType, request.AnswerValue),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ProposeOffer(
        [FromRoute] Guid sessionId,
        [FromBody] ProposeInquiryOfferRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new ProposeInquiryOfferCommand(
                sessionId, userId, request.RentCents, request.DepositCents, request.Note),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiry-sessions/{sessionId}", result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> AcceptOffer(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid offerId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new AcceptInquiryOfferCommand(sessionId, offerId, userId),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> CounterOffer(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid offerId,
        [FromBody] CounterInquiryOfferRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new CounterInquiryOfferCommand(
                sessionId, offerId, userId, request.RentCents, request.DepositCents, request.Note),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> WithdrawAcceptedOffer(
        [FromRoute] Guid sessionId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new WithdrawAcceptedInquiryOfferCommand(sessionId, userId),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> AddPartner(
        [FromRoute] Guid sessionId,
        [FromBody] AddInquiryPartnerRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new AddInquiryPartnerCommand(sessionId, userId, request.OrganizationId),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> RemovePartner(
        [FromRoute] Guid sessionId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(
            new RemoveInquiryPartnerCommand(sessionId, userId),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> ListPartnerInquiries(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new ListMyPartnerInquiriesQuery(userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> StartPartnerListingInquiry(
        [FromRoute] Guid listingId,
        [FromBody] StartPartnerListingInquiryRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var result = await mediator
            .Send(new StartPartnerListingInquiryCommand(listingId, request.TenantUserId, userId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiry-sessions/{result.Value.SessionId}", result.Value)
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
            "Inquiry.NotFound"
                or "Inquiry.DealNotFound"
                or "Inquiry.ListingNotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
