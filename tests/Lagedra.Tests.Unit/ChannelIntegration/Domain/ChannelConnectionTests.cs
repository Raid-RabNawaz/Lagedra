using System;
using FluentAssertions;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Domain;

public class ChannelConnectionTests
{
    private sealed class MutableClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static readonly Guid Host = Guid.NewGuid();

    private static ChannelConnection Connect(IClock clock) =>
        ChannelConnection.Create(
            Host,
            providerKey: "hostaway",
            externalAccountId: "12345",
            displayName: "Hostaway",
            username: "12345",
            encryptedSecret: "enc:secret",
            clock);

    [Fact]
    public void Revoke_destroys_credentials_and_stops_syncing()
    {
        var clock = new MutableClock();
        var connection = Connect(clock);
        connection.Activate(clock);
        connection.RecordContentSync(clock);

        connection.Revoke(clock);

        connection.Status.Should().Be(ChannelConnectionStatus.Revoked);
        connection.EncryptedSecret.Should().BeNull();
        connection.Username.Should().BeNull();

        // The account id and import history survive: they are what lets a
        // reconnect recognise the same account and re-link its listings.
        connection.ExternalAccountId.Should().Be("12345");
        connection.LastContentSyncAt.Should().NotBeNull();
    }

    [Fact]
    public void Revoke_is_idempotent()
    {
        var clock = new MutableClock();
        var connection = Connect(clock);
        connection.Revoke(clock);

        var revokedAt = connection.UpdatedAt;
        clock.UtcNow = clock.UtcNow.AddHours(1);
        connection.Revoke(clock);

        connection.Status.Should().Be(ChannelConnectionStatus.Revoked);
        connection.UpdatedAt.Should().Be(revokedAt);
    }

    [Fact]
    public void A_revoked_connection_cannot_be_activated()
    {
        var clock = new MutableClock();
        var connection = Connect(clock);
        connection.Revoke(clock);

        var activate = () => connection.Activate(clock);

        activate.Should().Throw<InvalidOperationException>();
        connection.Status.Should().Be(ChannelConnectionStatus.Revoked);
    }

    [Fact]
    public void Disabling_a_revoked_connection_is_a_no_op()
    {
        var clock = new MutableClock();
        var connection = Connect(clock);
        connection.Revoke(clock);

        connection.Disable(clock);

        connection.Status.Should().Be(ChannelConnectionStatus.Revoked);
    }

    [Fact]
    public void Relink_restores_a_revoked_connection_with_new_credentials()
    {
        var clock = new MutableClock();
        var connection = Connect(clock);
        connection.Activate(clock);
        connection.RecordContentSync(clock);
        connection.RecordBookingSync(clock);
        connection.Revoke(clock);

        clock.UtcNow = clock.UtcNow.AddDays(1);
        connection.Relink("67890", "Hostaway (new account)", "67890", "enc:rotated", clock);

        connection.Status.Should().Be(ChannelConnectionStatus.PendingActivation);
        connection.ExternalAccountId.Should().Be("67890");
        connection.DisplayName.Should().Be("Hostaway (new account)");
        connection.Username.Should().Be("67890");
        connection.EncryptedSecret.Should().Be("enc:rotated");

        // Nothing has been synced through the new credentials yet.
        connection.LastContentSyncAt.Should().BeNull();
        connection.LastBookingSyncAt.Should().BeNull();

        connection.Activate(clock);
        connection.Status.Should().Be(ChannelConnectionStatus.Active);
    }

    [Fact]
    public void Relink_rejects_a_connection_that_is_still_live()
    {
        var clock = new MutableClock();
        var connection = Connect(clock);
        connection.Activate(clock);

        var relink = () => connection.Relink("67890", "Hostaway", null, "enc:rotated", clock);

        relink.Should().Throw<InvalidOperationException>();
        connection.ExternalAccountId.Should().Be("12345");
        connection.EncryptedSecret.Should().Be("enc:secret");
    }
}
