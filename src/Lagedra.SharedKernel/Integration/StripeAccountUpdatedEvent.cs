using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration;

public sealed record StripeAccountUpdatedEvent(
    string StripeAccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
