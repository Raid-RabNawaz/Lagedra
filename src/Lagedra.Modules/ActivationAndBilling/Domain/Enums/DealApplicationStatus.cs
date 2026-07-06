namespace Lagedra.Modules.ActivationAndBilling.Domain.Enums;

/// <summary>
/// Booking-request lifecycle. The stored values are persisted as strings so
/// new members can be appended without renumbering existing rows.
/// </summary>
public enum DealApplicationStatus
{
    /// <summary>Submitted, awaiting host decision (a.k.a. PendingHostApproval).</summary>
    Pending,

    /// <summary>Host accepted; a deal id exists and the booking proceeds to seal+charge.</summary>
    Approved,

    /// <summary>Host declined (RejectedByHost).</summary>
    Rejected,

    /// <summary>Tenant cancelled (CancelledByTenant) or auto-cancelled.</summary>
    Cancelled,

    /// <summary>Pending request lapsed before the host decided.</summary>
    Expired,

    /// <summary>
    /// Host accepted and the Truth Surface sealed, but the off-session charge
    /// failed. The booking is NOT active; the tenant can update their card to
    /// retry against the same sealed snapshot.
    /// </summary>
    PaymentFailed
}
