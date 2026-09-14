using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using ScreeningPort = Lagedra.SharedKernel.Integration.Screening;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Entities;

public sealed class ScreeningOrder : Entity<Guid>
{
    public Guid ApplicationId { get; private set; }
    public Guid HostUserId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public Guid ListingId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ExternalOrderId { get; private set; }
    public string? ExternalReportId { get; private set; }
    public Uri? ApplicationUrl { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public ScreeningOrderStatus Status { get; private set; }
    public string? LastFailureReason { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private ScreeningOrder() { }

    public static ScreeningOrder Create(
        Guid applicationId,
        Guid hostUserId,
        Guid tenantUserId,
        Guid listingId,
        string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        var now = DateTime.UtcNow;
        return new ScreeningOrder
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            HostUserId = hostUserId,
            TenantUserId = tenantUserId,
            ListingId = listingId,
            Provider = provider.Trim(),
            Status = ScreeningOrderStatus.PendingPayment,
            CreatedAt = now
        };
    }

    public void RecordPaymentIntent(string paymentIntentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        StripePaymentIntentId = paymentIntentId;
    }

    public void MarkPaid()
    {
        if (Status is not (ScreeningOrderStatus.PendingPayment
            or ScreeningOrderStatus.Paid
            or ScreeningOrderStatus.Failed))
        {
            return;
        }

        Status = ScreeningOrderStatus.Paid;
        PaidAt ??= DateTime.UtcNow;
    }

    public void AttachProviderOrder(ScreeningPort.ScreeningProviderOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        ExternalOrderId = order.ProviderOrderId;
        ExternalReportId = order.ProviderReportId;
        ApplicationUrl = order.ApplicationUrl;
        LastFailureReason = null;
        Status = Map(order.Status);
    }

    public void ApplyProviderUpdate(
        ScreeningOrderStatus status,
        Uri? applicationUrl,
        string? reportId)
    {
        Status = status;
        if (applicationUrl is not null)
        {
            ApplicationUrl = applicationUrl;
        }

        if (!string.IsNullOrWhiteSpace(reportId))
        {
            ExternalReportId = reportId;
        }

        if (status is ScreeningOrderStatus.Completed)
        {
            CompletedAt ??= DateTime.UtcNow;
        }
    }

    public void MarkFailed(string? reason = null)
    {
        Status = ScreeningOrderStatus.Failed;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        var trimmed = reason.Trim();
        LastFailureReason = trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    public void MarkCanceled()
    {
        if (Status is ScreeningOrderStatus.Completed)
        {
            return;
        }

        Status = ScreeningOrderStatus.Canceled;
    }

    private static ScreeningOrderStatus Map(ScreeningPort.ScreeningOrderStatus status) =>
        status switch
        {
            ScreeningPort.ScreeningOrderStatus.WaitingForApplicant => ScreeningOrderStatus.WaitingForApplicant,
            ScreeningPort.ScreeningOrderStatus.Pending => ScreeningOrderStatus.Pending,
            ScreeningPort.ScreeningOrderStatus.Completed => ScreeningOrderStatus.Completed,
            ScreeningPort.ScreeningOrderStatus.Canceled => ScreeningOrderStatus.Canceled,
            ScreeningPort.ScreeningOrderStatus.Failed => ScreeningOrderStatus.Failed,
            ScreeningPort.ScreeningOrderStatus.Paid => ScreeningOrderStatus.Paid,
            _ => ScreeningOrderStatus.PendingPayment
        };
}
