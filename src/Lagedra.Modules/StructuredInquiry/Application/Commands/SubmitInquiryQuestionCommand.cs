using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record SubmitInquiryQuestionCommand(
    Guid DealId,
    Guid CallerUserId,
    InquiryCategory Category,
    Guid? PredefinedQuestionId,
    string? CustomQuestionText = null,
    string? OpenQuestionText = null) : IRequest<Result<InquiryQuestionDto>>;

public sealed class SubmitInquiryQuestionCommandHandler(
    InquiryDbContext dbContext,
    IDealApplicationStatusProvider dealStatusProvider)
    : IRequestHandler<SubmitInquiryQuestionCommand, Result<InquiryQuestionDto>>
{
    public async Task<Result<InquiryQuestionDto>> Handle(
        SubmitInquiryQuestionCommand request,
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

        var participants = await dealStatusProvider
            .GetParticipantsAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.DealNotFound", "Deal not found."));
        }

        if (participants.TenantUserId != request.CallerUserId)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the deal's tenant can submit inquiry questions."));
        }

        var session = await dbContext.Sessions
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.DealId == request.DealId
                && s.Status == InquirySessionStatus.Open, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.NotFound", "No open inquiry session found for this deal."));
        }

        var question = session.AddQuestion(
            request.Category,
            request.PredefinedQuestionId,
            request.CustomQuestionText,
            request.OpenQuestionText,
            request.CallerUserId,
            InquiryQuestionAuthorRole.Tenant,
            participants.LandlordUserId);
        dbContext.Entry(question).State = EntityState.Added;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryQuestionDto>.Success(InquiryDtoMapper.ToQuestionDto(question));
    }
}
