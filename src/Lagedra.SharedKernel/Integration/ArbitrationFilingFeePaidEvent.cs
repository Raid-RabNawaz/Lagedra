using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Raised by the Stripe webhook (ActivationAndBilling) when an arbitration
/// filing-fee PaymentIntent succeeds, so the Arbitration module can activate the
/// case without the two modules referencing each other directly.
/// </summary>
public sealed record ArbitrationFilingFeePaidEvent(
    Guid CaseId,
    string PaymentIntentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
