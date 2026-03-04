using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record BookingCancelledEvent(
    Guid DealId,
    Guid ListingId,
    Guid CancelledByUserId,
    string Reason,
    bool IsAutoCancel,
    long RefundAmountCents,
    long InsuranceRefundCents) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
