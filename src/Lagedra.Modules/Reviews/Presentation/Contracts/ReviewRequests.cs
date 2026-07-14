namespace Lagedra.Modules.Reviews.Presentation.Contracts;

public sealed record SubmitStayReviewRequest(
    int OverallRating,
    string PublicComment,
    string? PrivateFeedback,
    int? Cleanliness,
    int? Accuracy,
    int? Communication,
    int? Location,
    int? CheckIn,
    int? Value,
    int? RespectHouseRules);

public sealed record SubmitPartnerServiceReviewRequest(
    int OverallRating,
    int Responsiveness,
    int Reliability,
    int SupportQuality,
    string PublicComment);
