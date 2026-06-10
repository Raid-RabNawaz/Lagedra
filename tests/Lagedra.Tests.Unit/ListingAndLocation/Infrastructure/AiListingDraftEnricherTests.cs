using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;
using Lagedra.SharedKernel.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Infrastructure;

public sealed class AiListingDraftEnricherTests
{
    private static readonly Uri SourceUrl = new("https://owner-site.test/my-place");

    private static ImportedListingDraftDto BaseDraft() => new(
        Title: "Existing title",
        Description: null,
        PropertyType: null,
        Bedrooms: null,
        Bathrooms: null,
        SquareFootage: null,
        MaxGuests: null,
        CheckInTime: null,
        CheckOutTime: null,
        MonthlyRentCents: null,
        NightlyRateCents: null,
        Currency: null,
        ApproxAddress: null,
        AmenityHints: new[] { "WiFi" },
        Photos: null,
        SourceUrl: SourceUrl.ToString(),
        SourceHost: "owner-site.test");

    private static AiListingDraftEnricher CreateEnricher(IChatClient? chatClient, bool flagEnabled)
    {
        var clients = chatClient is null ? Array.Empty<IChatClient>() : new[] { chatClient };
        return new AiListingDraftEnricher(
            clients,
            new StubFeatureFlags(flagEnabled),
            Options.Create(new ListingImportAiSettings()),
            NullLogger<AiListingDraftEnricher>.Instance);
    }

    [Fact]
    public async Task EnrichAsync_NoChatClient_ReturnsDraftUnchanged()
    {
        var enricher = CreateEnricher(chatClient: null, flagEnabled: true);
        var draft = BaseDraft();

        var result = await enricher.EnrichAsync(draft, "<html><body>stuff</body></html>", SourceUrl);

        result.Should().BeSameAs(draft);
    }

    [Fact]
    public async Task EnrichAsync_FlagDisabled_DoesNotCallModel()
    {
        var client = new FakeChatClient("{\"description\":\"should be ignored\"}");
        var enricher = CreateEnricher(client, flagEnabled: false);
        var draft = BaseDraft();

        var result = await enricher.EnrichAsync(draft, "<html><body>stuff</body></html>", SourceUrl);

        result.Should().BeSameAs(draft);
        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task EnrichAsync_FillsGapsButNeverOverwritesExistingValues()
    {
        const string modelJson =
            "{\"title\":\"AI title\",\"description\":\"A cosy place\",\"bedrooms\":2," +
            "\"bathrooms\":1.5,\"maxGuests\":4,\"currency\":\"USD\",\"amenityHints\":[\"WiFi\",\"Pool\"]}";
        var client = new FakeChatClient(modelJson);
        var enricher = CreateEnricher(client, flagEnabled: true);
        var draft = BaseDraft();

        var result = await enricher.EnrichAsync(draft, "<html><body>A cosy place</body></html>", SourceUrl);

        result.Title.Should().Be("Existing title"); // not overwritten
        result.Description.Should().Be("A cosy place"); // gap filled
        result.Bedrooms.Should().Be(2);
        result.Bathrooms.Should().Be(1.5m);
        result.MaxGuests.Should().Be(4);
        result.Currency.Should().Be("USD");
        result.AmenityHints.Should().BeEquivalentTo(new[] { "WiFi", "Pool" }); // unioned, deduped
        result.SourceHost.Should().Be("owner-site.test"); // provenance preserved
        client.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task EnrichAsync_ModelFailure_ReturnsOriginalDraft()
    {
        var client = new ThrowingChatClient();
        var enricher = CreateEnricher(client, flagEnabled: true);
        var draft = BaseDraft();

        var result = await enricher.EnrichAsync(draft, "<html><body>stuff</body></html>", SourceUrl);

        result.Should().BeSameAs(draft);
    }

    private sealed class StubFeatureFlags(bool enabled) : IFeatureFlags
    {
        public bool BookingFlowV2Enabled => false;

        public bool IsEnabled(string flagName, bool defaultValue = false) => enabled;
    }

    private sealed class FakeChatClient(string json) : IChatClient
    {
        public int CallCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, json)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("model unavailable");

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
