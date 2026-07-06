namespace Lagedra.Modules.ActivationAndBilling.Domain.Enums;

/// <summary>
/// Payment lifecycle for a sealed deal. Persisted as strings; <c>Confirmed</c>
/// is the captured/settled terminal-success state (kept for back-compat with
/// rows written before the new members existed).
/// </summary>
public enum PaymentConfirmationStatus
{
    /// <summary>Created, awaiting capture.</summary>
    Pending,

    /// <summary>Captured/settled successfully (== Captured).</summary>
    Confirmed,

    /// <summary>Tenant disputed the payment.</summary>
    Disputed,

    /// <summary>Dispute resolved against the payment / rejected.</summary>
    Rejected,

    /// <summary>Cancelled.</summary>
    Cancelled,

    /// <summary>Tenant saved a card at request time; no money moved yet.</summary>
    PaymentMethodProvided,

    /// <summary>Off-session capture in progress (Stripe "processing"/"requires_capture").</summary>
    CapturePending,

    /// <summary>Capture failed; tenant must update their card and retry.</summary>
    Failed,

    /// <summary>Captured funds were refunded.</summary>
    Refunded
}
