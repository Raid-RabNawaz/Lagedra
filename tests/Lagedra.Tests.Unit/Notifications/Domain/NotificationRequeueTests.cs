using System;
using FluentAssertions;
using Lagedra.Modules.Notifications.Domain.Aggregates;
using Lagedra.Modules.Notifications.Domain.Enums;
using Xunit;

namespace Lagedra.Tests.Unit.Notifications.Domain;

public class NotificationRequeueTests
{
    [Fact]
    public void RequeueAfterMissingTemplate_resets_a_template_not_found_failure()
    {
        var notification = Notification.Queue(
            Guid.NewGuid(),
            "guest@example.com",
            NotificationChannel.Email,
            "application_approved",
            new());
        notification.MarkFailed("Template 'application_approved' not found for Email channel.");

        notification.RequeueAfterMissingTemplate();

        notification.Status.Should().Be(NotificationStatus.Queued);
        notification.AttemptCount.Should().Be(0);
        notification.LastError.Should().BeNull();
    }

    [Fact]
    public void RequeueAfterMissingTemplate_rejects_unrelated_failures()
    {
        var notification = Notification.Queue(
            Guid.NewGuid(),
            "guest@example.com",
            NotificationChannel.Email,
            "deal_activated",
            new());
        notification.MarkFailed("SendGrid timed out");

        var act = notification.RequeueAfterMissingTemplate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing template*");
    }
}
