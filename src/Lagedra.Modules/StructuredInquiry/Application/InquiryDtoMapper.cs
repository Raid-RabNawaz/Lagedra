using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Entities;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Application.DTOs;

internal static class InquiryDtoMapper
{
    public static InquiryDto ToDto(
        InquirySession s,
        string? partnerOrganizationName = null,
        Guid? landlordUserId = null) =>
        new(
            s.Id,
            s.DealId,
            s.ListingId,
            s.TenantUserId,
            s.Status,
            s.UnlockedByLandlordAt,
            s.ClosedAt,
            s.CreatedAt,
            s.Questions.Select(ToQuestionDto).ToList(),
            (s.Offers ?? Array.Empty<InquiryOffer>())
                .OrderBy(o => o.ProposedAt)
                .Select(ToOfferDto)
                .ToList(),
            s.AcceptedOffer is { } accepted ? ToOfferDto(accepted) : null,
            s.PartnerOrganizationId,
            partnerOrganizationName,
            landlordUserId);

    public static InquiryQuestionDto ToQuestionDto(InquiryQuestion q) =>
        new(
            q.Id,
            q.PredefinedQuestionId,
            q.Category,
            q.SubmittedAt,
            q.Answer is not null
                ? new InquiryAnswerDto(
                    q.Answer.Id, q.Answer.ResponseType, q.Answer.AnswerValue, q.Answer.AnsweredAt)
                : null,
            q.CustomText,
            q.OpenQuestionText,
            q.SubmittedByUserId,
            q.SubmittedByRole ?? InquiryQuestionAuthorRole.Tenant);

    public static InquiryOfferDto ToOfferDto(InquiryOffer o) =>
        new(
            o.Id,
            o.ProposedByUserId,
            o.ProposedByRole,
            o.RentCents,
            o.DepositCents,
            o.Note,
            o.Status,
            o.ProposedAt,
            o.RespondedAt,
            o.SupersedesOfferId);
}
