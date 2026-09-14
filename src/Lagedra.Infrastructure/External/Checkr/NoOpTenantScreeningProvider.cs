using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Screening;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.External.Checkr;

/// <summary>
/// Dev stand-in that never calls a bureau. Completes immediately with Pass
/// so local apply/host flows can be exercised without Checkr keys.
/// </summary>
public sealed partial class NoOpTenantScreeningProvider(ILogger<NoOpTenantScreeningProvider> logger)
    : ITenantScreeningProvider
{
    public string ProviderKey => "NoOp";

    public Task<ScreeningProviderOrder> CreateOrderAsync(
        ScreeningOrderRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        LogCreated(logger, request.Applicant.Email);

        var id = $"noop_{Guid.NewGuid():N}";
        return Task.FromResult(new ScreeningProviderOrder(
            id,
            ScreeningOrderStatus.Completed,
            ApplicationUrl: null,
            ProviderReportId: $"noop_rp_{Guid.NewGuid():N}"));
    }

    public Task<ScreeningProviderOrder?> GetOrderAsync(
        string providerOrderId,
        CancellationToken ct = default)
    {
        return Task.FromResult<ScreeningProviderOrder?>(new ScreeningProviderOrder(
            providerOrderId,
            ScreeningOrderStatus.Completed,
            null,
            providerOrderId));
    }

    public Task<ScreeningReportView?> GetOrderReportAsync(
        string providerOrderId,
        CancellationToken ct = default) =>
        GetReportAsync(providerOrderId, ct);

    public Task<ScreeningReportView?> GetReportAsync(
        string providerReportId,
        CancellationToken ct = default)
    {
        return Task.FromResult<ScreeningReportView?>(new ScreeningReportView(
            providerReportId,
            providerReportId,
            BackgroundCheckResult.Pass,
            "Development provider — no bureau report.",
            []));
    }

    public Task<ScreeningWebhookEvent?> HandleWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        return Task.FromResult<ScreeningWebhookEvent?>(null);
    }

    public Task<ScreeningReportPdf?> GetReportPdfAsync(
        string providerReportId,
        CancellationToken ct = default)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(
            "Development provider — no bureau PDF.");
        return Task.FromResult<ScreeningReportPdf?>(
            new ScreeningReportPdf(bytes, $"screening-{providerReportId}.txt", "text/plain"));
    }

    public Task CancelOrderAsync(string providerOrderId, CancellationToken ct = default) =>
        Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "NoOp screening provider created an auto-completed order for {Email}")]
    private static partial void LogCreated(ILogger logger, string email);
}
