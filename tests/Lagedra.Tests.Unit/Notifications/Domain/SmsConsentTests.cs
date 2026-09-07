using System;
using FluentAssertions;
using Lagedra.Modules.Notifications.Domain.Entities;
using Xunit;

namespace Lagedra.Tests.Unit.Notifications.Domain;

public class SmsConsentTests
{
    [Fact]
    public void Create_normalizes_the_phone_number()
    {
        var consent = SmsConsent.Create("(555) 123-4567");

        consent.PhoneE164.Should().Be("+15551234567");
        consent.OptedIn.Should().BeFalse();
        consent.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_an_invalid_number()
    {
        var act = () => SmsConsent.Create("not-a-phone");

        act.Should().Throw<ArgumentException>().WithParameterName("phoneE164");
    }

    [Fact]
    public void OptIn_records_source_user_and_timestamp()
    {
        var consent = SmsConsent.Create("+15551234567");
        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

        consent.OptIn(SmsConsent.SourceWebForm, now, userId);

        consent.OptedIn.Should().BeTrue();
        consent.OptedInAt.Should().Be(now);
        consent.OptedOutAt.Should().BeNull();
        consent.Source.Should().Be(SmsConsent.SourceWebForm);
        consent.UserId.Should().Be(userId);
    }

    [Fact]
    public void OptOut_clears_opt_in_without_dropping_the_user()
    {
        var consent = SmsConsent.Create("+15551234567");
        var userId = Guid.NewGuid();
        consent.OptIn(SmsConsent.SourceWebForm, DateTime.UtcNow, userId);

        var now = new DateTime(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc);
        consent.OptOut(SmsConsent.SourceKeyword, now);

        consent.OptedIn.Should().BeFalse();
        consent.OptedOutAt.Should().Be(now);
        consent.Source.Should().Be(SmsConsent.SourceKeyword);
        consent.UserId.Should().Be(userId);
    }
}
