using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

/// <summary>
/// Phase 17 — submit a question against an inquiry session by session id.
/// Works for both pre-booking and deal-linked sessions because the session
/// owns the (listingId, tenantUserId) identity and we no longer need a
/// deal id round-trip to authorize the tenant.
/// </summary>
public sealed record SubmitQuestionToSessionCommand(
    Guid SessionId,
    Guid CallerUserId,
    InquiryCategory Category,
    Guid? PredefinedQuestionId,
    string? CustomQuestionText = null,
    string? OpenQuestionText = null) : IRequest<Result<InquiryQuestionDto>>;

public sealed class SubmitQuestionToSessionCommandHandler(InquiryDbContext dbContext)
    : IRequestHandler<SubmitQuestionToSessionCommand, Result<InquiryQuestionDto>>
{
    public async Task<Result<InquiryQuestionDto>> Handle(
        SubmitQuestionToSessionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PredefinedQuestionId is null
            && string.IsNullOrWhiteSpace(request.CustomQuestionText)
            && string.IsNullOrWhiteSpace(request.OpenQuestionText))
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.InvalidQuestion",
                    "Either a predefined question ID, custom question text, or open question text must be provided."));
        }

        var session = await dbContext.Sessions
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.NotFound", "Inquiry thread not found."));
        }

        if (session.TenantUserId != request.CallerUserId)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the inquiring tenant can submit questions to this thread."));
        }

        if (session.Status != InquirySessionStatus.Open)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.NotOpen",
                    $"Cannot add a question to a session in status '{session.Status}'."));
        }

        var question = session.AddQuestion(
            request.Category,
            request.PredefinedQuestionId,
            request.CustomQuestionText,
            request.OpenQuestionText);

        dbContext.Entry(question).State = EntityState.Added;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryQuestionDto>.Success(
            new InquiryQuestionDto(question.Id, question.PredefinedQuestionId,
                question.Category, question.SubmittedAt, null,
                question.CustomText, question.OpenQuestionText));
    }
}
