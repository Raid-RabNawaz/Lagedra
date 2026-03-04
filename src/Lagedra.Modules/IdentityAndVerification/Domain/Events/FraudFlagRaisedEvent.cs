using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.Events;

public sealed record FraudFlagRaisedEvent(
    Guid FlagId,
    Guid UserId,
    string Reason,
    string Source) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
