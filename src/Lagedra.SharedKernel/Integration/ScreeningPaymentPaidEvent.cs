using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Raised when a host-paid screening PaymentIntent succeeds so Identity
/// can place the provider order without Billing knowing the vendor.
/// </summary>
public sealed record ScreeningPaymentPaidEvent(
    Guid ScreeningOrderId,
    string PaymentIntentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
