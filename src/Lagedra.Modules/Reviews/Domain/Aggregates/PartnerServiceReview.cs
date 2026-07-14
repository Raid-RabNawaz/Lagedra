using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.Reviews.Domain.Aggregates;

/// <summary>
/// Endorsed member rates a partner organization's services. Visible immediately.
/// </summary>
public sealed class PartnerServiceReview : AggregateRoot<Guid>
{
    public const int MinCommentLength = 40;
    public const int MinCriticalCommentLength = 80;
    public const int MinWordCount = 6;
    public const int MaxCommentLength = 2000;

    public Guid OrganizationId { get; private set; }
    public Guid EndorsementId { get; private set; }
    public Guid ReviewerUserId { get; private set; }

    public int OverallRating { get; private set; }
    public int Responsiveness { get; private set; }
    public int Reliability { get; private set; }
    public int SupportQuality { get; private set; }

    public string PublicComment { get; private set; } = string.Empty;
    public DateTime SubmittedAt { get; private set; }

    private PartnerServiceReview() { }

    public static PartnerServiceReview Submit(
        Guid organizationId,
        Guid endorsementId,
        Guid reviewerUserId,
        int overallRating,
        int responsiveness,
        int reliability,
        int supportQuality,
        string publicComment,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ValidateRating(overallRating, nameof(overallRating));
        ValidateRating(responsiveness, nameof(responsiveness));
        ValidateRating(reliability, nameof(reliability));
        ValidateRating(supportQuality, nameof(supportQuality));
        ArgumentException.ThrowIfNullOrWhiteSpace(publicComment);

        var trimmed = publicComment.Trim();
        var minLength = overallRating <= 2 ? MinCriticalCommentLength : MinCommentLength;
        if (trimmed.Length < minLength || trimmed.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                overallRating <= 2
                    ? $"Critical ratings need a specific written explanation of at least {MinCriticalCommentLength} characters."
                    : $"Public comment must be between {MinCommentLength} and {MaxCommentLength} characters.",
                nameof(publicComment));
        }

        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < MinWordCount)
        {
            throw new ArgumentException(
                $"Please write a subjective review with at least {MinWordCount} words.",
                nameof(publicComment));
        }

        var letterCount = trimmed.Count(char.IsLetter);
        if (letterCount < MinCommentLength / 2)
        {
            throw new ArgumentException(
                "Public comment must be a readable description of your experience.",
                nameof(publicComment));
        }

        var upperLetters = trimmed.Count(c => char.IsLetter(c) && char.IsUpper(c));
        if (letterCount >= 20 && upperLetters >= letterCount * 0.8)
        {
            throw new ArgumentException(
                "Please write your review in normal sentence case — all-caps remarks are not accepted.",
                nameof(publicComment));
        }

        var now = clock.UtcNow;
        var review = new PartnerServiceReview
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EndorsementId = endorsementId,
            ReviewerUserId = reviewerUserId,
            OverallRating = overallRating,
            Responsiveness = responsiveness,
            Reliability = reliability,
            SupportQuality = supportQuality,
            PublicComment = trimmed,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        review.AddDomainEvent(new PartnerServiceReviewSubmittedEvent(
            review.Id, organizationId, reviewerUserId, overallRating, now));

        return review;
    }

    private static void ValidateRating(int rating, string paramName)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rating must be between 1 and 5.");
        }
    }
}
