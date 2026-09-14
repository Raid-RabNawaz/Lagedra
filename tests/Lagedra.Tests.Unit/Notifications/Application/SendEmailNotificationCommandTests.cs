using System;
using System.Collections.Generic;
using FluentAssertions;
using Lagedra.Modules.Notifications.Application.Commands;
using Xunit;

namespace Lagedra.Tests.Unit.Notifications.Application;

public class SendEmailNotificationCommandTests
{
    [Fact]
    public void TryGetDealId_reads_camelCase_payload()
    {
        var dealId = Guid.NewGuid();

        SendEmailNotificationCommandHandler.TryGetDealId(
                new Dictionary<string, string> { ["dealId"] = dealId.ToString() },
                out var parsed)
            .Should().BeTrue();

        parsed.Should().Be(dealId);
    }

    [Fact]
    public void TryGetDealId_reads_pascal_or_related_entity_keys()
    {
        var dealId = Guid.NewGuid();

        SendEmailNotificationCommandHandler.TryGetDealId(
                new Dictionary<string, string> { ["DealId"] = dealId.ToString() },
                out var fromPascal)
            .Should().BeTrue();
        fromPascal.Should().Be(dealId);

        SendEmailNotificationCommandHandler.TryGetDealId(
                new Dictionary<string, string> { ["relatedEntityId"] = dealId.ToString() },
                out var fromRelated)
            .Should().BeTrue();
        fromRelated.Should().Be(dealId);
    }

    [Fact]
    public void TryGetDealId_returns_false_when_missing()
    {
        SendEmailNotificationCommandHandler.TryGetDealId(
                new Dictionary<string, string> { ["listingTitle"] = "Loft" },
                out var parsed)
            .Should().BeFalse();

        parsed.Should().Be(Guid.Empty);
    }
}
