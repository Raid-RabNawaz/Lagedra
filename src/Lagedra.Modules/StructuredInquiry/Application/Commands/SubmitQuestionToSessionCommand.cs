using Lagedra.Modules.StructuredInquiry.Application.DTOs;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

/// <summary>
/// Submit a question on an inquiry session. Allowed for the tenant or
/// staff of the attached partner organization.
/// </summary>
public sealed record SubmitQuestionToSessionCommand(
    Guid SessionId,
    Guid CallerUserId,
    InquiryCategory Category,
    Guid? PredefinedQuestionId,
    string? CustomQuestionText = null,
    string? OpenQuestionText = null) : IRequest<Result<InquiryQuestionDto>>;

public sealed class SubmitQuestionToSessionCommandHandler(
    InquiryDbContext dbContext,
    IListingProvider listingProvider,
    IPartnerMembershipProvider membershipProvider)
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

        if (session.Status != InquirySessionStatus.Open)
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.NotOpen",
                    $"Cannot add a question to a session in status '{session.Status}'."));
        }

        InquiryQuestionAuthorRole role;
        if (session.TenantUserId == request.CallerUserId)
        {
            role = InquiryQuestionAuthorRole.Tenant;
        }
        else if (session.PartnerOrganizationId is { } partnerOrgId)
        {
            var callerOrgId = await membershipProvider
                .GetPartnerOrganizationIdAsync(request.CallerUserId, cancellationToken)
                .ConfigureAwait(false);

            if (callerOrgId != partnerOrgId)
            {
                return Result<InquiryQuestionDto>.Failure(
                    new Error("Inquiry.Forbidden",
                        "Only the tenant or the attached partner can submit questions."));
            }

            role = InquiryQuestionAuthorRole.Partner;
        }
        else
        {
            return Result<InquiryQuestionDto>.Failure(
                new Error("Inquiry.Forbidden",
                    "Only the inquiring tenant can submit questions to this thread."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(session.ListingId, cancellationToken)
            .ConfigureAwait(false);

        var question = session.AddQuestion(
            request.Category,
            request.PredefinedQuestionId,
            request.CustomQuestionText,
            request.OpenQuestionText,
            request.CallerUserId,
            role,
            listing?.LandlordUserId);

        dbContext.Entry(question).State = EntityState.Added;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<InquiryQuestionDto>.Success(InquiryDtoMapper.ToQuestionDto(question));
    }
}
