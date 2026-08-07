using System;
using System.Collections.Generic;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Infrastructure.Security;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// The OAuth <c>state</c> parameter is what stops a stranger from finishing an
/// authorization against someone else's Lagedra account, so it is worth pinning
/// that a forged, stale, or foreign-key state is refused. Uses the real
/// encryption service rather than a stub — its authenticated encryption is the
/// property under test.
/// </summary>
public sealed class OwnerRezOAuthFlowTests
{
    private sealed class MutableClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    private static readonly Guid Host = Guid.NewGuid();

    private static IConfiguration Config(string encryptionKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = encryptionKey,
                ["App:BaseUrl"] = "https://api.lagedra.com/",
            })
            .Build();

    private static string NewKey() => Convert.ToBase64String(new byte[32]);

    private static OwnerRezOAuthFlow CreateFlow(
        MutableClock clock,
        string? encryptionKey = null,
        OwnerRezChannelSettings? settings = null)
    {
        var configuration = Config(encryptionKey ?? NewKey());
        return new OwnerRezOAuthFlow(
            configuration,
            new EncryptionService(configuration),
            Options.Create(settings ?? new OwnerRezChannelSettings
            {
                ClientId = "c_lagedra",
                ClientSecret = "s_shhh",
            }),
            clock);
    }

    [Fact]
    public void RedirectUri_PointsAtTheApiCallback()
    {
        CreateFlow(new MutableClock()).RedirectUri.Should().Be(
            new Uri("https://api.lagedra.com/v1/channels/ownerrez/oauth/callback"));
    }

    [Fact]
    public void State_RoundTripsTheHostItWasMintedFor()
    {
        var flow = CreateFlow(new MutableClock());

        flow.TryReadState(flow.CreateState(Host)).Should().Be(Host);
    }

    [Fact]
    public void State_IsDifferentEveryTime()
    {
        var flow = CreateFlow(new MutableClock());

        flow.CreateState(Host).Should().NotBe(flow.CreateState(Host));
    }

    [Fact]
    public void TryReadState_TamperedState_IsRefused()
    {
        var flow = CreateFlow(new MutableClock());
        var state = flow.CreateState(Host);

        var tampered = state[..^4] + (state.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA");

        flow.TryReadState(tampered).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!")]
    [InlineData(null)]
    public void TryReadState_Garbage_IsRefused(string? state)
    {
        CreateFlow(new MutableClock()).TryReadState(state).Should().BeNull();
    }

    [Fact]
    public void TryReadState_StateFromAnotherKey_IsRefused()
    {
        var clock = new MutableClock();
        var state = CreateFlow(clock, Convert.ToBase64String(new byte[32] { 7, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })).CreateState(Host);

        CreateFlow(clock).TryReadState(state).Should().BeNull();
    }

    [Fact]
    public void TryReadState_ExpiredState_IsRefused()
    {
        var clock = new MutableClock();
        var flow = CreateFlow(clock, settings: new OwnerRezChannelSettings
        {
            ClientId = "c_lagedra",
            ClientSecret = "s_shhh",
            AuthorizationStateLifetimeMinutes = 10,
        });
        var state = flow.CreateState(Host);

        clock.UtcNow = clock.UtcNow.AddMinutes(9);
        flow.TryReadState(state).Should().Be(Host);

        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        flow.TryReadState(state).Should().BeNull();
    }

    [Fact]
    public void IsConfigured_IsFalseWithoutClientCredentials()
    {
        CreateFlow(new MutableClock(), settings: new OwnerRezChannelSettings())
            .IsConfigured.Should().BeFalse();

        CreateFlow(new MutableClock()).IsConfigured.Should().BeTrue();
    }
}
