using System;
using FluentAssertions;
using Lagedra.Modules.Reviews.Domain.Aggregates;
using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.Reviews.Domain;

public class StayReviewTests
{
    private sealed class MutableClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static readonly Guid Deal = Guid.NewGuid();
    private static readonly Guid Listing = Guid.NewGuid();
    private static readonly Guid Guest = Guid.NewGuid();
    private static readonly Guid Host = Guid.NewGuid();

    private const string GuestComment =
        "The apartment matched the listing photos and check-in was smooth. Communication with the host was clear throughout.";

    private const string HostComment =
        "The guest was respectful of house rules, kept the place tidy, and communicated promptly about arrival times.";

    private const string CriticalComment =
        "The unit was not cleaned on arrival and several listed amenities were missing. The host was slow to respond when we raised these issues during the first week.";

    private static StayReviewCategories GuestCats => new()
    {
        Cleanliness = 5,
        Accuracy = 4,
        Communication = 5,
        Location = 4,
        CheckIn = 5,
        Value = 4
    };

    private static StayReviewCategories HostCats => new()
    {
        Cleanliness = 5,
        Communication = 4,
        RespectHouseRules = 5
    };

    [Fact]
    public void Guest_to_host_submit_requires_guest_categories()
    {
        var clock = new MutableClock();
        var act = () => StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            5, GuestComment, null,
            new StayReviewCategories { Cleanliness = 5 },
            clock);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_rejects_short_comments()
    {
        var clock = new MutableClock();
        var act = () => StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            5, "Too short.", null, GuestCats, clock);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Submit_rejects_all_caps_rage_comments()
    {
        var clock = new MutableClock();
        var act = () => StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            1, "THIS PLACE WAS AWFUL AND I HATE EVERYTHING ABOUT IT NEVER COMING BACK AGAIN EVER",
            null, GuestCats, clock);

        act.Should().Throw<ArgumentException>().WithMessage("*sentence case*");
    }

    [Fact]
    public void Critical_rating_requires_longer_comment()
    {
        var clock = new MutableClock();
        // Valid for a 3–5 star review (≥40 chars) but too short for a critical rating.
        const string mediumComment =
            "The stay was fine overall but a few amenities were missing on arrival day.";
        mediumComment.Length.Should().BeLessThan(StayReview.MinCriticalCommentLength);

        var act = () => StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            2, mediumComment, null, GuestCats, clock);

        act.Should().Throw<ArgumentException>().WithMessage("*Critical*");
    }

    [Fact]
    public void Partial_window_stays_unpublished_until_both_or_expiry()
    {
        var clock = new MutableClock();
        var window = StayReviewWindow.Open(Deal, Listing, Host, Guest, 14, clock);

        window.MarkGuestSubmitted(clock);
        window.ShouldPublish(clock).Should().BeFalse();

        window.MarkHostSubmitted(clock);
        window.ShouldPublish(clock).Should().BeTrue();
    }

    [Fact]
    public void Window_publishes_on_expiry_even_if_one_sided()
    {
        var clock = new MutableClock();
        var window = StayReviewWindow.Open(Deal, Listing, Host, Guest, 14, clock);
        window.MarkGuestSubmitted(clock);

        clock.UtcNow = clock.UtcNow.AddDays(15);
        window.ShouldPublish(clock).Should().BeTrue();
    }

    [Fact]
    public void Publish_emits_PositiveReview_when_overall_at_least_4()
    {
        var clock = new MutableClock();
        var review = StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            5, GuestComment, null, GuestCats, clock);
        review.ClearDomainEvents();

        review.Publish(clock);

        review.Status.Should().Be(StayReviewStatus.Published);
        review.DomainEvents.OfType<PositiveReviewEarnedEvent>()
            .Should().ContainSingle(e => e.RevieweeUserId == Host && e.OverallRating == 5);
        review.DomainEvents.OfType<StayReviewPublishedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Publish_emits_ReviewConcern_when_overall_at_most_2()
    {
        var clock = new MutableClock();
        var review = StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            2, CriticalComment, null, GuestCats, clock);
        review.ClearDomainEvents();

        review.Publish(clock);

        review.DomainEvents.OfType<ReviewConcernRaisedEvent>()
            .Should().ContainSingle(e => e.RevieweeUserId == Host && e.OverallRating == 2);
        review.DomainEvents.OfType<PositiveReviewEarnedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Publish_skips_PositiveReview_when_overall_below_4()
    {
        var clock = new MutableClock();
        var review = StayReview.Submit(
            Deal, Listing, Guest, Host, StayReviewDirection.GuestToHost,
            3, "Okay stay overall but noisy nights from the street made sleep difficult.", null, GuestCats, clock);
        review.ClearDomainEvents();

        review.Publish(clock);

        review.DomainEvents.OfType<PositiveReviewEarnedEvent>().Should().BeEmpty();
        review.DomainEvents.OfType<ReviewConcernRaisedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Host_to_guest_submit_works_with_host_categories()
    {
        var clock = new MutableClock();
        var review = StayReview.Submit(
            Deal, Listing, Host, Guest, StayReviewDirection.HostToGuest,
            5, HostComment, null, HostCats, clock);

        review.Direction.Should().Be(StayReviewDirection.HostToGuest);
        review.RespectHouseRules.Should().Be(5);
        review.Status.Should().Be(StayReviewStatus.Submitted);
    }

    [Fact]
    public void Partner_service_review_validates_ratings()
    {
        var clock = new MutableClock();
        var act = () => PartnerServiceReview.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guest,
            6, 5, 5, 5, "Great partner support team that helped with booking logistics.", clock);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Partner_service_review_submits_successfully()
    {
        var clock = new MutableClock();
        var org = Guid.NewGuid();
        var review = PartnerServiceReview.Submit(
            org, Guid.NewGuid(), Guest,
            5, 5, 4, 5, "Helpful and reliable partner who coordinated housing for our members well.", clock);

        review.OrganizationId.Should().Be(org);
        review.OverallRating.Should().Be(5);
    }

    [Fact]
    public void Reminder_is_due_after_interval_until_submitted()
    {
        var clock = new MutableClock();
        var window = StayReviewWindow.Open(Deal, Listing, Host, Guest, 14, clock);

        window.ReminderDue(clock, reminderIntervalDays: 3).Should().BeFalse();

        clock.UtcNow = clock.UtcNow.AddDays(3);
        window.ReminderDue(clock, reminderIntervalDays: 3).Should().BeTrue();

        window.MarkReminderSent(clock);
        window.ReminderDue(clock, reminderIntervalDays: 3).Should().BeFalse();

        clock.UtcNow = clock.UtcNow.AddDays(3);
        window.ReminderDue(clock, reminderIntervalDays: 3).Should().BeTrue();
    }

    [Fact]
    public void Reminder_skips_party_that_already_submitted()
    {
        var clock = new MutableClock();
        var window = StayReviewWindow.Open(Deal, Listing, Host, Guest, 14, clock);
        window.MarkGuestSubmitted(clock);

        window.NeedsGuestReminder().Should().BeFalse();
        window.NeedsHostReminder().Should().BeTrue();
    }

    [Fact]
    public void Reminder_not_due_once_published()
    {
        var clock = new MutableClock();
        var window = StayReviewWindow.Open(Deal, Listing, Host, Guest, 14, clock);
        window.MarkGuestSubmitted(clock);
        window.MarkHostSubmitted(clock);
        window.MarkPublished(clock);

        clock.UtcNow = clock.UtcNow.AddDays(10);
        window.ReminderDue(clock, reminderIntervalDays: 3).Should().BeFalse();
    }
}
