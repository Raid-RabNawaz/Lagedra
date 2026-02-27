using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.InsuranceIntegration.Domain.Events;

public sealed record InsuranceUnknownSlaBreachedEvent(
    Guid DealId,
    Guid PolicyRecordId,
    DateTime UnknownSince) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
