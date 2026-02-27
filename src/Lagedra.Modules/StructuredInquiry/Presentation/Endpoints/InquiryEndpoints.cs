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
        group.MapPost("/{dealId:guid}/close", CloseInquiry);
        group.MapGet("/{dealId:guid}", GetInquiryThread);
        group.MapGet("/predefined-questions", ListPredefinedQuestions);

        return app;
    }

    private static async Task<IResult> RequestDetailUnlock(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RequestDetailUnlockCommand(dealId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiries/{dealId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ApproveInquiryUnlock(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ApproveInquiryUnlockCommand(dealId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SubmitInquiryQuestion(
        [FromRoute] Guid dealId,
        [FromBody] SubmitInquiryQuestionRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SubmitInquiryQuestionCommand(dealId, request.Category, request.PredefinedQuestionId),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/v1/inquiries/{dealId}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> SubmitLandlordResponse(
        [FromRoute] Guid dealId,
        [FromBody] SubmitLandlordResponseRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SubmitLandlordResponseCommand(dealId, request.QuestionId, request.ResponseType, request.AnswerValue),
            cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
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
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetInquiryThread(
        [FromRoute] Guid dealId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInquiryThreadQuery(dealId), cancellationToken)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
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
}
