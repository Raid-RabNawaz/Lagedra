namespace Lagedra.SharedKernel.Integration.Screening;

/// <summary>
/// Vendor-neutral tenant screening (criminal + credit). Checkr is the first
/// adapter; swapping vendors is a new implementation plus config.
/// </summary>
public interface ITenantScreeningProvider
{
    string ProviderKey { get; }

    Task<ScreeningProviderOrder> CreateOrderAsync(
        ScreeningOrderRequest request,
        CancellationToken ct = default);

    Task<ScreeningProviderOrder?> GetOrderAsync(
        string providerOrderId,
        CancellationToken ct = default);

    Task<ScreeningReportView?> GetReportAsync(
        string providerReportId,
        CancellationToken ct = default);

    /// <summary>
    /// Poll the report for an order. Returns null while the provider still has none.
    /// </summary>
    Task<ScreeningReportView?> GetOrderReportAsync(
        string providerOrderId,
        CancellationToken ct = default);

    Task<ScreeningWebhookEvent?> HandleWebhookAsync(
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default);

    Task<ScreeningReportPdf?> GetReportPdfAsync(
        string providerReportId,
        CancellationToken ct = default);

    /// <summary>
    /// Best-effort cancel. Providers that have no cancel API should no-op.
    /// </summary>
    Task CancelOrderAsync(string providerOrderId, CancellationToken ct = default);
}

public enum ScreeningPackage
{
    CriminalAndCredit = 0
}

public enum ScreeningOrderStatus
{
    PendingPayment,
    Paid,
    WaitingForApplicant,
    Pending,
    Completed,
    Canceled,
    Failed
}

public sealed record ScreeningPropertyAddress(
    string Street,
    string City,
    string State,
    string Zipcode,
    string? Name = null,
    string? Unit = null);

public sealed record ScreeningApplicant(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber = null);

public enum ScreeningRequesterType
{
    Individual = 0,
    Business = 1
}

/// <summary>
/// End user the report is furnished to. Platforms must send this on every
/// bureau order — Checkr may suspend accounts that omit it.
/// </summary>
public sealed record ScreeningRequester(
    string ExternalId,
    string Name,
    string Email,
    string? PhoneNumber = null,
    string? Street = null,
    string? City = null,
    string? State = null,
    string? Zipcode = null,
    ScreeningRequesterType Type = ScreeningRequesterType.Individual);

public sealed record ScreeningOrderRequest(
    ScreeningPackage Package,
    ScreeningPropertyAddress Property,
    ScreeningApplicant Applicant,
    ScreeningRequester OriginalRequester,
    string IdempotencyKey);

public sealed record ScreeningProviderOrder(
    string ProviderOrderId,
    ScreeningOrderStatus Status,
    Uri? ApplicationUrl,
    string? ProviderReportId);

/// <summary>
/// Paying-host view of a report. Adapters map vendor payloads into this shape.
/// Do not persist the raw vendor JSON.
/// </summary>
public sealed record ScreeningReportProduct(
    string Key,
    string Label,
    string? Status,
    string? Detail);

public sealed record ScreeningReportView(
    string ProviderReportId,
    string ProviderOrderId,
    BackgroundCheckResult OverallResult,
    string? Summary,
    IReadOnlyList<ScreeningReportProduct> Products);

public sealed record ScreeningReportPdf(
    IReadOnlyList<byte> Content,
    string FileName,
    string ContentType);

public sealed record ScreeningWebhookEvent(
    string EventId,
    string EventType,
    string? ProviderOrderId,
    string? ProviderReportId,
    ScreeningOrderStatus? OrderStatus,
    BackgroundCheckResult? OverallResult,
    Uri? ApplicationUrl = null);
