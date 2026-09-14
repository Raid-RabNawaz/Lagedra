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

        var parsed = ParseCreateResult(body);
        LogCreated(logger, parsed.VerificationId, parsed.Status);
        return parsed;
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

    /// <summary>
    /// Official create success is nested:
    /// <c>{ "metadata": {...}, "verification": { "verificationId", "status", "flaggedReason?" } }</c>.
    /// Flat top-level fields are accepted as a fallback.
    /// </summary>
    private static TruviVerificationResult ParseCreateResult(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new TruviScreenAndProtectException("Truvi create returned an empty body.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new TruviScreenAndProtectException(
                $"Truvi create response was not JSON: {Truncate(body)}",
                ex);
        }

        using (document)
        {
            var root = document.RootElement;
            var verification = root.TryGetProperty("verification", out var nested)
                ? nested
                : root;
            var verificationId = ReadString(verification, "verificationId")
                ?? ReadString(root, "verificationId");
            var status = ReadString(verification, "status")
                ?? ReadString(root, "status");
            var flaggedReason = ReadString(verification, "flaggedReason")
                ?? ReadString(root, "flaggedReason");

            if (string.IsNullOrWhiteSpace(verificationId) || string.IsNullOrWhiteSpace(status))
            {
                throw new TruviScreenAndProtectException(
                    $"Truvi create response was missing verificationId or status. Body: {Truncate(body)}");
            }

            return new TruviVerificationResult(verificationId, status, flaggedReason);
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static string Truncate(string value)
        => value.Length <= 400 ? value : value[..400];

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
