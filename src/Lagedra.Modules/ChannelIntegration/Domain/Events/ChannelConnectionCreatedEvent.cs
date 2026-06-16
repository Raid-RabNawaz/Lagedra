using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ChannelIntegration.Domain.Events;

public sealed record ChannelConnectionCreatedEvent(
    Guid ConnectionId,
    Guid HostUserId,
    string ProviderKey) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
