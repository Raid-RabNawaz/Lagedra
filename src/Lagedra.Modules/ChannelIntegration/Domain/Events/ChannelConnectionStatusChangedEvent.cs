using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ChannelIntegration.Domain.Events;

public sealed record ChannelConnectionStatusChangedEvent(
    Guid ConnectionId,
    string Status) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
