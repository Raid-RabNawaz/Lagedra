using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Screening;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Checkr;

public sealed partial class CheckrTenantScreeningProvider(
    HttpClient httpClient,
    IOptions<CheckrSettings> settings,
    ILogger<CheckrTenantScreeningProvider> logger) : ITenantScreeningProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public string ProviderKey => "Checkr";

    public async Task<ScreeningProviderOrder> CreateOrderAsync(
        ScreeningOrderRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cfg = settings.Value;
        // Checkr: platforms that furnish reports to an end-user MUST send
        // original_requester (the host who receives the report). Omitting it
        // can suspend the account. Send the inline object only — never both
        // original_requester and original_requester_id.
        var requester = CheckrOriginalRequesterPayload.From(request.OriginalRequester);

        using var message = new HttpRequestMessage(HttpMethod.Post, "orders?include=property,applicant,original_requester");
        ApplyAuth(message, cfg);
        message.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey);
        message.Content = JsonContent(new
        {
            order = new
            {
                package = cfg.PackageSlug,
                property = new
                {
                    name = request.Property.Name,
                    street = request.Property.Street,
                    unit = request.Property.Unit,
                    city = request.Property.City,
                    state = RequireUsState(request.Property.State),
                    zipcode = NormalizeUsZip(request.Property.Zipcode)
                },
                applicant = new
                {
                    first_name = request.Applicant.FirstName,
                    last_name = request.Applicant.LastName,
                    email = request.Applicant.Email,
                    phone_number = ScreeningPhone.UsOrNull(request.Applicant.PhoneNumber)
                },
                original_requester = requester
            }
        });

        using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogCreateFailed(logger, (int)response.StatusCode, body);
            throw new InvalidOperationException(FormatCreateError((int)response.StatusCode, body));
        }

        return MapOrderJson(body);
    }

    public async Task<ScreeningProviderOrder?> GetOrderAsync(
        string providerOrderId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderId);
        var cfg = settings.Value;
        using var message = new HttpRequestMessage(HttpMethod.Get, $"orders/{providerOrderId}");
        ApplyAuth(message, cfg);
        using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return MapOrderJson(body);
    }

    public async Task<ScreeningReportView?> GetReportAsync(
        string providerReportId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReportId);
        var cfg = settings.Value;
        using var message = new HttpRequestMessage(HttpMethod.Get, $"reports/{providerReportId}");
        ApplyAuth(message, cfg);
        using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return MapReportJson(body, providerReportId);
    }

    public async Task<ScreeningReportView?> GetOrderReportAsync(
        string providerOrderId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderId);
        var cfg = settings.Value;
        using var message = new HttpRequestMessage(HttpMethod.Get, $"orders/{providerOrderId}/report");
        ApplyAuth(message, cfg);
        using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return MapReportJson(body, providerOrderId);
    }

    public async Task<ScreeningReportPdf?> GetReportPdfAsync(
        string providerReportId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReportId);

        // Checkr renders the PDF on this call and asks for a 60s+ read timeout.
        // Retry once on transient render failures (timeout / 5xx), not on 404.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await FetchReportPdfOnceAsync(providerReportId, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                LogPdfFailed(logger, providerReportId, ex.Message);
                if (attempt == 1)
                {
                    return null;
                }
            }
        }

        return null;
    }

    private async Task<ScreeningReportPdf?> FetchReportPdfOnceAsync(
        string providerReportId,
        CancellationToken ct)
    {
        var cfg = settings.Value;
        using var message = new HttpRequestMessage(HttpMethod.Get, $"reports/{providerReportId}/pdf");
        ApplyAuth(message, cfg);
        using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            LogPdfFailed(logger, providerReportId, $"HTTP {(int)response.StatusCode}");
            response.EnsureSuccessStatusCode();
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        if (!LooksLikePdf(bytes))
        {
            LogPdfFailed(logger, providerReportId, "Response was not a PDF");
            return null;
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"screening-{providerReportId}.pdf";
        return new ScreeningReportPdf(bytes, fileName, "application/pdf");
    }

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length >= 4
        && bytes[0] == (byte)'%'
        && bytes[1] == (byte)'P'
        && bytes[2] == (byte)'D'
        && bytes[3] == (byte)'F';

    public async Task CancelOrderAsync(string providerOrderId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderId);
        var cfg = settings.Value;
        using var message = new HttpRequestMessage(HttpMethod.Post, $"orders/{providerOrderId}/cancel");
        ApplyAuth(message, cfg);
        using var response = await httpClient.SendAsync(message, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode
            && response.StatusCode is not System.Net.HttpStatusCode.NotFound
            and not System.Net.HttpStatusCode.MethodNotAllowed)
        {
            LogCancelFailed(logger, (int)response.StatusCode, providerOrderId);
        }
    }

    public Task<ScreeningWebhookEvent?> HandleWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentNullException.ThrowIfNull(headers);

        // Dashboard "Test" sends an empty POST with no signature. Acknowledge it.
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return Task.FromResult<ScreeningWebhookEvent?>(null);
        }

        if (!headers.TryGetValue("Tenant-Signature", out var signature)
            && !headers.TryGetValue("tenant-signature", out signature))
        {
            LogBadSignature(logger, "missing");
            return Task.FromResult<ScreeningWebhookEvent?>(null);
        }

        if (!VerifySignature(rawBody, signature, settings.Value.WebhookSecret))
        {
            LogBadSignature(logger, "mismatch");
            return Task.FromResult<ScreeningWebhookEvent?>(null);
        }

        return Task.FromResult(ParseWebhook(rawBody));
    }

    internal static ScreeningWebhookEvent? ParseWebhook(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
        var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? string.Empty : string.Empty;
        string? orderId = null;
        string? reportId = null;
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            if (data.TryGetProperty("order_id", out var o) && o.ValueKind == JsonValueKind.String)
            {
                orderId = o.GetString();
            }

            // Checkr: data.id meaning depends on event type. Never treat applicant
            // or product-result ids as an order id.
            if (string.Equals(type, "report.completed", StringComparison.Ordinal))
            {
                reportId = data.TryGetProperty("id", out var rid) ? rid.GetString() : null;
            }
            else if (string.Equals(type, "report.product.completed", StringComparison.Ordinal)
                && data.TryGetProperty("report_id", out var prid))
            {
                reportId = prid.GetString();
            }
        }

        ScreeningOrderStatus? status = type switch
        {
            "order.applicant.visited" or "order.applicant.started" => ScreeningOrderStatus.WaitingForApplicant,
            "order.applicant.completed" => ScreeningOrderStatus.Pending,
            "report.completed" => ScreeningOrderStatus.Completed,
            _ => null
        };

        return new ScreeningWebhookEvent(eventId, type, orderId, reportId, status, OverallResult: null);
    }

    private sealed record CheckrOriginalRequesterPayload(
        string ExternalId,
        string Type,
        string Name,
        string Email,
        string? PhoneNumber,
        string? Street,
        string? City,
        string? State,
        string? Zipcode)
    {
        public static CheckrOriginalRequesterPayload From(ScreeningRequester requester)
        {
            ArgumentNullException.ThrowIfNull(requester);

            if (string.IsNullOrWhiteSpace(requester.ExternalId)
                || string.IsNullOrWhiteSpace(requester.Name)
                || string.IsNullOrWhiteSpace(requester.Email))
            {
                throw new InvalidOperationException(
                    "Checkr original_requester requires external_id, name, and email.");
            }

            return new CheckrOriginalRequesterPayload(
                requester.ExternalId.Trim(),
                requester.Type == ScreeningRequesterType.Business ? "business" : "individual",
                requester.Name.Trim(),
                requester.Email.Trim(),
                ScreeningPhone.UsOrNull(requester.PhoneNumber),
                BlankToNull(requester.Street),
                BlankToNull(requester.City),
                UsStateCode.TryNormalize(requester.State, out var requesterState)
                    ? requesterState
                    : null,
                BlankToNull(requester.Zipcode));
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ApplyAuth(HttpRequestMessage message, CheckrSettings cfg)
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.SecretKey);
        message.Headers.TryAddWithoutValidation("Checkr-Tenant-Version", cfg.ApiVersion);
    }

    private static StringContent JsonContent(object payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    private static string FormatCreateError(int statusCode, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array)
            {
                var mapped = errors.EnumerateArray()
                    .Select(e => (
                        Pointer: e.TryGetProperty("source", out var source)
                            && source.ValueKind == JsonValueKind.Object
                            && source.TryGetProperty("pointer", out var pointer)
                            ? pointer.GetString()
                            : null,
                        Detail: e.TryGetProperty("detail", out var d) ? d.GetString() : null))
                    .ToArray();
                return ScreeningProviderError.ToHostMessage(statusCode, mapped);
            }
        }
        catch (JsonException)
        {
            // Fall through to the status-only message.
        }

        return ScreeningProviderError.ToHostMessage(statusCode, []);
    }

    private static string RequireUsState(string state)
    {
        if (UsStateCode.TryNormalize(state, out var code))
        {
            return code;
        }

        throw new InvalidOperationException(
            "This listing's state must be a US state or territory (for example CA, not California written in a form Checkr cannot read).");
    }

    private static string NormalizeUsZip(string zip)
    {
        var digits = new string(zip.Where(char.IsDigit).ToArray());
        return digits.Length >= 9
            ? $"{digits[..5]}-{digits[5..9]}"
            : digits.Length >= 5 ? digits[..5] : zip.Trim();
    }

    private static ScreeningProviderOrder MapOrderJson(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
        Uri? applyUrl = null;
        if (root.TryGetProperty("application_url", out var urlEl)
            && urlEl.ValueKind == JsonValueKind.String
            && Uri.TryCreate(urlEl.GetString(), UriKind.Absolute, out var parsedUrl))
        {
            applyUrl = parsedUrl;
        }

        var reportId = root.TryGetProperty("report_id", out var rid) ? rid.GetString() : null;
        return new ScreeningProviderOrder(id, MapStatus(status), applyUrl, reportId);
    }

    private static ScreeningReportView MapReportJson(string body, string fallbackId)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var reportId = root.TryGetProperty("id", out var idEl)
            ? idEl.GetString() ?? fallbackId
            : fallbackId;
        var orderId = root.TryGetProperty("order_id", out var oid)
            ? oid.GetString() ?? fallbackId
            : fallbackId;
        var products = MapProducts(root);
        var overall = MapReportResult(root);
        return new ScreeningReportView(reportId, orderId, overall, BuildSummary(overall, products), products);
    }

    private static ScreeningOrderStatus MapStatus(string? status) =>
        status switch
        {
            "waiting_for_applicant" => ScreeningOrderStatus.WaitingForApplicant,
            "pending" => ScreeningOrderStatus.Pending,
            "completed" => ScreeningOrderStatus.Completed,
            "canceled" or "cancelled" => ScreeningOrderStatus.Canceled,
            _ => ScreeningOrderStatus.Pending
        };

    private static readonly string[] ScoredProducts =
    [
        "criminal_history",
        "credit_report",
        "eviction_history",
        "sex_offender_registry",
        "global_watchlist",
        "identity_verification",
        "income_verification"
    ];

    private static readonly (string Key, string Label)[] ProductCatalog =
    [
        ("criminal_history", "Criminal history"),
        ("credit_report", "Credit report"),
        ("eviction_history", "Eviction history"),
        ("sex_offender_registry", "Sex offender registry"),
        ("global_watchlist", "Global watchlist"),
        ("identity_verification", "Identity verification"),
        ("income_verification", "Income verification")
    ];

    private static List<ScreeningReportProduct> MapProducts(JsonElement root)
    {
        var products = new List<ScreeningReportProduct>();
        foreach (var (key, label) in ProductCatalog)
        {
            if (!root.TryGetProperty(key, out var product) || product.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? status = null;
            if (product.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.String)
            {
                status = statusEl.GetString();
            }

            products.Add(new ScreeningReportProduct(key, label, status, ProductDetail(key, product)));
        }

        return products;
    }

    private static string? ProductDetail(string key, JsonElement product)
    {
        if (key == "credit_report")
        {
            var parts = new List<string>();
            if (product.TryGetProperty("credit_file_status", out var file) && file.ValueKind == JsonValueKind.String)
            {
                var fileStatus = file.GetString();
                if (!string.IsNullOrWhiteSpace(fileStatus))
                {
                    parts.Add(fileStatus switch
                    {
                        "available" => "Credit file available",
                        "frozen" => "Credit file frozen",
                        "not_found" => "Credit file not found",
                        "thin_file" => "Thin credit file",
                        _ => fileStatus
                    });
                }
            }

            if (product.TryGetProperty("credit_score", out var score) && score.TryGetInt32(out var points))
            {
                parts.Add($"Score {points}");
            }

            if (product.TryGetProperty("credit_summary", out var summary) && summary.ValueKind == JsonValueKind.Object)
            {
                if (summary.TryGetProperty("collections_count", out var collections) && collections.TryGetInt32(out var c) && c > 0)
                {
                    parts.Add($"{c} collection{(c == 1 ? "" : "s")}");
                }

                if (summary.TryGetProperty("bankruptcies_count", out var banks) && banks.TryGetInt32(out var b) && b > 0)
                {
                    parts.Add($"{b} bankruptc{(b == 1 ? "y" : "ies")}");
                }
            }

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        if (key == "income_verification"
            && product.TryGetProperty("verification_method", out var method)
            && method.ValueKind == JsonValueKind.String)
        {
            return method.GetString() switch
            {
                "document" => "Verified from documents",
                "payroll" => "Verified from payroll",
                var other => string.IsNullOrWhiteSpace(other) ? null : other
            };
        }

        if (product.TryGetProperty("records", out var records) && records.ValueKind == JsonValueKind.Array)
        {
            var count = records.GetArrayLength();
            return count == 0 ? "No records" : $"{count} record{(count == 1 ? "" : "s")}";
        }

        return null;
    }

    private static string BuildSummary(
        BackgroundCheckResult overall,
        List<ScreeningReportProduct> products)
    {
        if (products.Count == 0)
        {
            return overall == BackgroundCheckResult.Pass
                ? "The screening provider returned a clear result."
                : "The screening provider flagged this report for review.";
        }

        var parts = products.Select(p =>
        {
            var status = string.Equals(p.Status, "clear", StringComparison.OrdinalIgnoreCase)
                ? "clear"
                : string.Equals(p.Status, "consider", StringComparison.OrdinalIgnoreCase)
                    ? "review"
                    : p.Status ?? "pending";
            return $"{p.Label}: {status}";
        });
        return string.Join(" · ", parts);
    }

    internal static BackgroundCheckResult MapReportResult(JsonElement root)
    {
        var sawProduct = false;
        var consider = false;
        foreach (var name in ScoredProducts)
        {
            if (!root.TryGetProperty(name, out var product) || product.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!product.TryGetProperty("status", out var statusEl))
            {
                continue;
            }

            var status = statusEl.GetString();
            if (string.IsNullOrWhiteSpace(status))
            {
                continue;
            }

            sawProduct = true;
            if (string.Equals(status, "consider", StringComparison.OrdinalIgnoreCase))
            {
                consider = true;
            }
        }

        if (!sawProduct)
        {
            return BackgroundCheckResult.Review;
        }

        return consider ? BackgroundCheckResult.Review : BackgroundCheckResult.Pass;
    }

    internal static bool VerifySignature(string rawBody, string header, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        string? timestamp = null;
        string? v1 = null;
        foreach (var part in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0)
            {
                continue;
            }

            var key = part[..eq];
            var value = part[(eq + 1)..];
            if (key == "t")
            {
                timestamp = value;
            }
            else if (key == "v1")
            {
                v1 = value;
            }
        }

        if (timestamp is null || v1 is null)
        {
            return false;
        }

        var payload = timestamp + "." + rawBody;
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(v1.ToUpperInvariant()));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Checkr create order failed: {StatusCode} {Body}")]
    private static partial void LogCreateFailed(ILogger logger, int statusCode, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Checkr webhook signature {Reason}")]
    private static partial void LogBadSignature(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Checkr cancel order failed: {StatusCode} {OrderId}")]
    private static partial void LogCancelFailed(ILogger logger, int statusCode, string orderId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Checkr report PDF failed for {ReportId}: {Reason}")]
    private static partial void LogPdfFailed(ILogger logger, string reportId, string reason);
}
