using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

/// <summary>
/// Raised when the host accepted a request and the Truth Surface sealed, but
/// the off-session charge failed. The booking is held in
/// <c>DealApplicationStatus.PaymentFailed</c>; the tenant is asked to update
/// their card and retry, and the host is told the booking did not activate.
/// </summary>
public sealed record BookingPaymentFailedEvent(
    Guid ApplicationId,
    Guid DealId,
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
