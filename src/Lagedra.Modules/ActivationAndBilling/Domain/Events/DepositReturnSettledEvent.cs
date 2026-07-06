using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

/// <summary>
/// Raised when both parties have confirmed the deposit return handshake (host
/// returned the deposit, tenant received it) — the point at which the deal is
/// considered fully completed under the non-custodial model.
/// </summary>
public sealed record DepositReturnSettledEvent(
    Guid DealId,
    DateTime SettledAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
