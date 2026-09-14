namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Stripe PaymentIntent metadata for host-paid tenant screening.
/// </summary>
public static class ScreeningPaymentMetadata
{
    public const string PurposeKey = "purpose";
    public const string PurposeValue = "tenant_screening";
    public const string OrderIdKey = "screeningOrderId";
    public const string ApplicationIdKey = "applicationId";
    public const string HostUserIdKey = "hostUserId";
}
