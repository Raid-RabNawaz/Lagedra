using Lagedra.Modules.Reviews.Domain.Enums;

namespace Lagedra.Modules.Reviews.Application.DTOs;

public sealed record StayReviewDto(
    Guid Id,
    Guid DealId,
    Guid ListingId,
    Guid ReviewerUserId,
    Guid RevieweeUserId,
    StayReviewDirection Direction,
    StayReviewStatus Status,
    int OverallRating,
    int? Cleanliness,
    int? Accuracy,
    int? Communication,
    int? Location,
    int? CheckIn,
    int? Value,
    int? RespectHouseRules,
    string PublicComment,
    DateTime SubmittedAt,
    DateTime? PublishedAt);

public sealed record StayReviewWindowDto(
    Guid DealId,
    Guid ListingId,
    DateTime OpensAt,
    DateTime ClosesAt,
    bool GuestSubmitted,
    bool HostSubmitted,
    bool IsPublished,
    DateTime? PublishedAt,
    bool CanCallerSubmit,
    StayReviewDirection? CallerDirection,
    StayReviewDto? OwnReview,
    StayReviewDto? PeerReview);

public sealed record PartnerServiceReviewDto(
    Guid Id,
    Guid OrganizationId,
    Guid ReviewerUserId,
    int OverallRating,
    int Responsiveness,
    int Reliability,
    int SupportQuality,
    string PublicComment,
    DateTime SubmittedAt);

public sealed record PartnerReputationDto(
    Guid OrganizationId,
    double AverageOverall,
    int ReviewCount,
    double AverageResponsiveness,
    double AverageReliability,
    double AverageSupportQuality,
    bool CallerCanReview,
    bool CallerAlreadyReviewed);
