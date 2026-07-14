using Lagedra.Modules.Reviews.Application.DTOs;
using Lagedra.Modules.Reviews.Domain.Aggregates;
using Lagedra.Modules.Reviews.Domain.Enums;

namespace Lagedra.Modules.Reviews.Application;

internal static class ReviewMapper
{
    public static StayReviewDto ToDto(StayReview r, bool includePrivate = false) =>
        new(
            r.Id,
            r.DealId,
            r.ListingId,
            r.ReviewerUserId,
            r.RevieweeUserId,
            r.Direction,
            r.Status,
            r.OverallRating,
            r.Cleanliness,
            r.Accuracy,
            r.Communication,
            r.Location,
            r.CheckIn,
            r.Value,
            r.RespectHouseRules,
            r.PublicComment,
            r.SubmittedAt,
            r.PublishedAt);

    public static PartnerServiceReviewDto ToDto(PartnerServiceReview r) =>
        new(
            r.Id,
            r.OrganizationId,
            r.ReviewerUserId,
            r.OverallRating,
            r.Responsiveness,
            r.Reliability,
            r.SupportQuality,
            r.PublicComment,
            r.SubmittedAt);
}
