using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised once per penalized party when an arbitration decision is issued.
/// Subscribed by Compliance to record an ArbitrationRuling trust ledger entry
/// against the party the ruling went against.
/// </summary>
public sealed record ArbitrationRulingIssuedEvent(
    Guid CaseId,
    Guid DealId,
    Guid PartyUserId,
    string PenaltySummary,
    DateTime DecidedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
