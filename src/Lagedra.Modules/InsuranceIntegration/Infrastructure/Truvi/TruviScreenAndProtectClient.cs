using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.SharedKernel.Insurance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;

public sealed partial class TruviScreenAndProtectClient(
    HttpClient httpClient,
    IOptions<TruviScreenAndProtectSettings> settings,
    ILogger<TruviScreenAndProtectClient> logger)
    : ITruviScreenAndProtectClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
    };

    private readonly TruviScreenAndProtectSettings _settings = settings.Value;

    public async Task<TruviVerificationResult> CreateAsync(
        TruviCreateVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Post, "verificationRequests")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        AddSubscriptionKey(message);

        var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response, body);
        }

        var parsed = JsonSerializer.Deserialize<CreateResponse>(body, JsonOptions)
            ?? throw new TruviScreenAndProtectException("Truvi create returned an empty body.");

        if (string.IsNullOrWhiteSpace(parsed.VerificationId) || string.IsNullOrWhiteSpace(parsed.Status))
        {
            throw new TruviScreenAndProtectException("Truvi create response was missing verificationId or status.");
        }

        LogCreated(logger, parsed.VerificationId, parsed.Status);
        return new TruviVerificationResult(parsed.VerificationId, parsed.Status, parsed.FlaggedReason);
    }

    public async Task ModifyAsync(
        TruviModifyVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Put, "verificationRequests")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        AddSubscriptionKey(message);

        var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response, body);
        }

        LogModified(logger, request.Verification.VerificationId);
    }

    public async Task CancelAsync(
        TruviCancelVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var message = new HttpRequestMessage(HttpMethod.Put, "verificationRequests/cancel")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        AddSubscriptionKey(message);

        var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response, body);
        }

        LogCancelled(logger, request.Verification.VerificationId);
    }

    private void AddSubscriptionKey(HttpRequestMessage message)
    {
        if (string.IsNullOrWhiteSpace(_settings.SubscriptionKey))
        {
            throw new TruviScreenAndProtectException("Insurance:Truvi:SubscriptionKey is not configured.");
        }

        message.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _settings.SubscriptionKey);
    }

    private static TruviScreenAndProtectException ToException(HttpResponseMessage response, string body)
    {
        ProblemDetails? problem = null;
        try
        {
            problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
        }
        catch (JsonException)
        {
            // Fall through to the raw-body message.
        }

        var detail = problem?.Detail ?? problem?.Title ?? body;
        return new TruviScreenAndProtectException(
            $"Truvi screening failed ({(int)response.StatusCode}): {detail}")
        {
            Status = problem?.Status ?? (int)response.StatusCode,
            Title = problem?.Title,
            Detail = problem?.Detail ?? body,
        };
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi verification created {VerificationId} with status {Status}")]
    private static partial void LogCreated(ILogger logger, string verificationId, string status);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi verification modified {VerificationId}")]
    private static partial void LogModified(ILogger logger, string verificationId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Truvi verification cancelled {VerificationId}")]
    private static partial void LogCancelled(ILogger logger, string verificationId);

#pragma warning disable CA1812
    private sealed class CreateResponse
    {
        [JsonPropertyName("verificationId")]
        public string VerificationId { get; init; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("flaggedReason")]
        public string? FlaggedReason { get; init; }
    }

    private sealed class ProblemDetails
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("detail")]
        public string? Detail { get; init; }

        [JsonPropertyName("status")]
        public int? Status { get; init; }
    }
#pragma warning restore CA1812
}
