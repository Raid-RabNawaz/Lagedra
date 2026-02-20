using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Persona;

public sealed partial class PersonaClient(
    HttpClient httpClient,
    IOptions<PersonaSettings> settings,
    ILogger<PersonaClient> logger)
    : IPersonaClient
{
    private readonly PersonaSettings _settings = settings.Value;

    public async Task<PersonaInquiry> CreateInquiryAsync(Guid userId, string email, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/inquiries");
        AddAuthHeader(request);

        var body = new
        {
            data = new
            {
                type = "inquiry",
                attributes = new
                {
                    inquiry_template_id = _settings.TemplateId,
                    reference_id = userId.ToString(),
                    fields = new { email_address = new { value = email } }
                }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<PersonaApiResponse>(json)
            ?? throw new InvalidOperationException("Persona returned null response.");

        LogInquiryCreated(logger, userId, result.Data.Id);
        return MapInquiry(result.Data);
    }

    public async Task<PersonaInquiry> GetInquiryAsync(string inquiryId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/inquiries/{inquiryId}");
        AddAuthHeader(request);

        var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<PersonaApiResponse>(json)
            ?? throw new InvalidOperationException("Persona returned null response.");

        return MapInquiry(result.Data);
    }

    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(signature);
        _ = ct;
        ValidateWebhookSignature(payload, signature);
        LogWebhookReceived(logger, signature[..Math.Min(8, signature.Length)]);
        return Task.CompletedTask;
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Headers.Add("Persona-Version", "2023-01-05");
    }

    private void ValidateWebhookSignature(string payload, string signature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToUpperInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature.ToUpperInvariant())))
        {
            throw new InvalidOperationException("Persona webhook signature validation failed.");
        }
    }

    private static PersonaInquiry MapInquiry(PersonaApiData data) =>
        new(
            data.Id,
            data.Attributes.Status switch
            {
                "created" => PersonaInquiryStatus.Created,
                "pending" => PersonaInquiryStatus.Pending,
                "completed" => PersonaInquiryStatus.Completed,
                "failed" => PersonaInquiryStatus.Failed,
                "expired" => PersonaInquiryStatus.Expired,
                _ => PersonaInquiryStatus.Created
            },
            data.Attributes.SessionToken,
            data.Attributes.CreatedAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Persona inquiry created for user {UserId}: {InquiryId}")]
    private static partial void LogInquiryCreated(ILogger logger, Guid userId, string inquiryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Persona webhook received (sig prefix: {SigPrefix})")]
    private static partial void LogWebhookReceived(ILogger logger, string sigPrefix);

#pragma warning disable CA1812 // instantiated by System.Text.Json via reflection
    private sealed class PersonaApiResponse
    {
        [JsonPropertyName("data")] public PersonaApiData Data { get; init; } = new();
    }

    private sealed class PersonaApiData
    {
        [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
        [JsonPropertyName("attributes")] public PersonaApiAttributes Attributes { get; init; } = new();
    }

    private sealed class PersonaApiAttributes
    {
        [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
        [JsonPropertyName("session-token")] public string? SessionToken { get; init; }
        [JsonPropertyName("created-at")] public DateTime CreatedAt { get; init; }
    }
#pragma warning restore CA1812
}
