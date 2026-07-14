using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.Reviews.Domain.Aggregates;

public sealed class StayReview : AggregateRoot<Guid>
{
    /// <summary>Minimum length for a fair, experience-based public review.</summary>
    public const int MinCommentLength = 40;

    /// <summary>Critical ratings need more specific written context.</summary>
    public const int MinCriticalCommentLength = 80;

    public const int MinWordCount = 6;
    public const int MaxCommentLength = 2000;
    public const int MaxPrivateFeedbackLength = 2000;

    public Guid DealId { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid ReviewerUserId { get; private set; }
    public Guid RevieweeUserId { get; private set; }
    public StayReviewDirection Direction { get; private set; }
    public StayReviewStatus Status { get; private set; }

    public int OverallRating { get; private set; }

    // Guest → Host categories
    public int? Cleanliness { get; private set; }
    public int? Accuracy { get; private set; }
    public int? Communication { get; private set; }
    public int? Location { get; private set; }
    public int? CheckIn { get; private set; }
    public int? Value { get; private set; }

    // Host → Guest categories (Cleanliness + Communication reused; RespectHouseRules)
    public int? RespectHouseRules { get; private set; }

    public string PublicComment { get; private set; } = string.Empty;
    public string? PrivateFeedbackToPlatform { get; private set; }

    public DateTime SubmittedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    private StayReview() { }

    public static StayReview Submit(
        Guid dealId,
        Guid listingId,
        Guid reviewerUserId,
        Guid revieweeUserId,
        StayReviewDirection direction,
        int overallRating,
        string publicComment,
        string? privateFeedback,
        StayReviewCategories categories,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(categories);
        ValidateRating(overallRating, nameof(overallRating));
        ValidateComment(publicComment, overallRating);

        categories.ValidateFor(direction);

        var now = clock.UtcNow;
        var review = new StayReview
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            ListingId = listingId,
            ReviewerUserId = reviewerUserId,
            RevieweeUserId = revieweeUserId,
            Direction = direction,
            Status = StayReviewStatus.Submitted,
            OverallRating = overallRating,
            Cleanliness = categories.Cleanliness,
            Accuracy = categories.Accuracy,
            Communication = categories.Communication,
            Location = categories.Location,
            CheckIn = categories.CheckIn,
            Value = categories.Value,
            RespectHouseRules = categories.RespectHouseRules,
            PublicComment = publicComment.Trim(),
            PrivateFeedbackToPlatform = string.IsNullOrWhiteSpace(privateFeedback)
                ? null
                : privateFeedback.Trim(),
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (review.PrivateFeedbackToPlatform is { Length: > MaxPrivateFeedbackLength })
        {
            throw new ArgumentException(
                $"Private feedback must be at most {MaxPrivateFeedbackLength} characters.",
                nameof(privateFeedback));
        }

        review.AddDomainEvent(new StayReviewSubmittedEvent(
            review.Id, dealId, direction, reviewerUserId, revieweeUserId, now));

        return review;
    }

    public void Publish(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (Status == StayReviewStatus.Published)
        {
            return;
        }

        Status = StayReviewStatus.Published;
        PublishedAt = clock.UtcNow;
        UpdatedAt = PublishedAt.Value;

        AddDomainEvent(new StayReviewPublishedEvent(
            Id, DealId, ListingId, Direction, ReviewerUserId, RevieweeUserId, OverallRating, PublishedAt.Value));

        if (OverallRating >= 4)
        {
            AddDomainEvent(new PositiveReviewEarnedEvent(DealId, RevieweeUserId, OverallRating));
        }
        else if (OverallRating <= 2)
        {
            AddDomainEvent(new ReviewConcernRaisedEvent(DealId, RevieweeUserId, OverallRating));
        }
    }

    private static void ValidateRating(int rating, string paramName)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(paramName, "Rating must be between 1 and 5.");
        }
    }

    private static void ValidateComment(string comment, int overallRating)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comment);
        var trimmed = comment.Trim();
        var minLength = overallRating <= 2 ? MinCriticalCommentLength : MinCommentLength;

        if (trimmed.Length < minLength)
        {
            throw new ArgumentException(
                overallRating <= 2
                    ? $"Critical ratings need a specific written explanation of at least {MinCriticalCommentLength} characters."
                    : $"Public comment must be at least {MinCommentLength} characters describing your experience.",
                nameof(comment));
        }

        if (trimmed.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                $"Public comment must be at most {MaxCommentLength} characters.",
                nameof(comment));
        }

        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < MinWordCount)
        {
            throw new ArgumentException(
                $"Please write a subjective review with at least {MinWordCount} words about the stay.",
                nameof(comment));
        }

        // Reject keyboard-mash / all-caps rage dumps that aren't real feedback.
        var letterCount = trimmed.Count(char.IsLetter);
        if (letterCount < MinCommentLength / 2)
        {
            throw new ArgumentException(
                "Public comment must be a readable description of your experience.",
                nameof(comment));
        }

        var upperLetters = trimmed.Count(c => char.IsLetter(c) && char.IsUpper(c));
        if (letterCount >= 20 && upperLetters >= letterCount * 0.8)
        {
            throw new ArgumentException(
                "Please write your review in normal sentence case — all-caps remarks are not accepted.",
                nameof(comment));
        }
    }
}

/// <summary>Category ratings payload validated per review direction.</summary>
public sealed class StayReviewCategories
{
    public int? Cleanliness { get; init; }
    public int? Accuracy { get; init; }
    public int? Communication { get; init; }
    public int? Location { get; init; }
    public int? CheckIn { get; init; }
    public int? Value { get; init; }
    public int? RespectHouseRules { get; init; }

    public void ValidateFor(StayReviewDirection direction)
    {
        if (direction == StayReviewDirection.GuestToHost)
        {
            Require(Cleanliness, nameof(Cleanliness));
            Require(Accuracy, nameof(Accuracy));
            Require(Communication, nameof(Communication));
            Require(Location, nameof(Location));
            Require(CheckIn, nameof(CheckIn));
            Require(Value, nameof(Value));
        }
        else
        {
            Require(Cleanliness, nameof(Cleanliness));
            Require(Communication, nameof(Communication));
            Require(RespectHouseRules, nameof(RespectHouseRules));
        }
    }

    private static void Require(int? rating, string name)
    {
        if (rating is null or < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be a rating between 1 and 5.");
        }
    }
}
