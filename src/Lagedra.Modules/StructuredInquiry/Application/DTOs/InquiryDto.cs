using Lagedra.Modules.StructuredInquiry.Domain.Enums;

namespace Lagedra.Modules.StructuredInquiry.Application.DTOs;

public sealed record InquiryDto(
    Guid SessionId,
    Guid? DealId,
    Guid ListingId,
    Guid TenantUserId,
    InquirySessionStatus Status,
    DateTime? UnlockedByLandlordAt,
    DateTime? ClosedAt,
    DateTime CreatedAt,
    IReadOnlyList<InquiryQuestionDto> Questions);

public sealed record InquiryQuestionDto(
    Guid QuestionId,
    Guid? PredefinedQuestionId,
    InquiryCategory Category,
    DateTime SubmittedAt,
    InquiryAnswerDto? Answer,
    string? CustomText = null,
    string? OpenQuestionText = null);

public sealed record InquiryAnswerDto(
    Guid AnswerId,
    ResponseType ResponseType,
    string AnswerValue,
    DateTime AnsweredAt);

/// <summary>
/// Phase 17 — lightweight inbox row for the host inquiries page. Joins
/// in just enough listing + tenant context to render a card without
/// fetching the entire thread.
/// </summary>
public sealed record HostInquirySummaryDto(
    Guid SessionId,
    Guid ListingId,
    string? ListingTitle,
    Uri? ListingCoverPhotoUri,
    string? ListingCity,
    Guid TenantUserId,
    string? TenantDisplayName,
    InquirySessionStatus Status,
    Guid? DealId,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    int QuestionCount,
    int UnansweredCount);

/// <summary>
/// Phase 17 — tenant counterpart of <see cref="HostInquirySummaryDto"/>.
/// Powers the "My conversations" page so tenants can revisit every
/// inquiry thread they've started, regardless of whether they've
/// applied yet.
/// </summary>
/// <remarks>
/// <c>UnansweredByHostCount</c> is the number of questions the tenant
/// is still waiting on — the symmetric metric to the host inbox's
/// <c>UnansweredCount</c>.
/// </remarks>
public sealed record TenantInquirySummaryDto(
    Guid SessionId,
    Guid ListingId,
    string? ListingTitle,
    Uri? ListingCoverPhotoUri,
    string? ListingCity,
    Guid LandlordUserId,
    string? LandlordDisplayName,
    InquirySessionStatus Status,
    Guid? DealId,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    int QuestionCount,
    int UnansweredByHostCount);
