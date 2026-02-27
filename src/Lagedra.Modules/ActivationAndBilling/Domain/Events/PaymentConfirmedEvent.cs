using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record PaymentConfirmedEvent(
    Guid DealId,
    DateTime ConfirmedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
