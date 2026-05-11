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
        group.MapPost("/{dealId:guid}/questions", SubmitInquiryQuestion);
        group.MapPost("/{dealId:guid}/answers", SubmitLandlordResponse);
        // Inquiries close automatically when the Truth Surface is confirmed.
        // Manual close is restricted to platform admins (e.g. dispute support).
        group.MapPost("/{dealId:guid}/close", CloseInquiry)
            .RequireAuthorization("RequirePlatformAdmin");
        group.MapGet("/{dealId:guid}", GetInquiryThread);
        group.MapGet("/predefined-questions", ListPredefinedQuestions);

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
                request.Category, request.PredefinedQuestionId, request.CustomQuestionText),
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
            "Inquiry.NotFound" or "Inquiry.DealNotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
