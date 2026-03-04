using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Events;

public sealed record AccountRestrictionAppliedEvent(
    Guid RestrictionId,
    Guid UserId,
    RestrictionLevel Level,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
