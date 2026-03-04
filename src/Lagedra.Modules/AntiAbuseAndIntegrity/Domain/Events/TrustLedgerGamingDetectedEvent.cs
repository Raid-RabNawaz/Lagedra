using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Events;

public sealed record TrustLedgerGamingDetectedEvent(
    Guid AbuseCaseId,
    Guid SubjectUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
