using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.Ai;

/// <summary>
/// Minimal <see cref="IChatClient"/> over the OpenAI-compatible
/// <c>/chat/completions</c> endpoint. We implement this directly instead of
/// taking a vendor SDK dependency so the module relies only on the stable
/// <c>Microsoft.Extensions.AI</c> abstractions and works against any
/// OpenAI-compatible provider. The structured-output helpers
/// (<c>GetResponseAsync&lt;T&gt;</c>) build on top of this client.
/// </summary>
public sealed class OpenAiCompatibleChatClient : IChatClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly ListingImportAiSettings _settings;
    private readonly ChatClientMetadata _metadata;

    public OpenAiCompatibleChatClient(HttpClient httpClient, IOptions<ListingImportAiSettings> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _settings = options.Value;
        _metadata = new ChatClientMetadata("openai-compatible", _httpClient.BaseAddress, _settings.Model);
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var wantsJson = options?.ResponseFormat is ChatResponseFormatJson;
        var payload = new ChatCompletionRequest
        {
            Model = options?.ModelId ?? _settings.Model,
            Temperature = options?.Temperature ?? 0f,
            Messages = messages
                .Select(m => new ChatCompletionMessage { Role = m.Role.Value, Content = m.Text })
                .ToList(),
            ResponseFormat = wantsJson ? new ChatCompletionResponseFormat { Type = "json_object" } : null,
        };

        using var response = await _httpClient
            .PostAsJsonAsync("chat/completions", payload, SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, ExtractContent(body)));
    }

    private static string ExtractContent(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(ChatClientMetadata))
        {
            return _metadata;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        // HttpClient lifetime is owned by IHttpClientFactory.
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public IReadOnlyList<ChatCompletionMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("response_format")]
        public ChatCompletionResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class ChatCompletionResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
    }

    private sealed class ChatCompletionMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
