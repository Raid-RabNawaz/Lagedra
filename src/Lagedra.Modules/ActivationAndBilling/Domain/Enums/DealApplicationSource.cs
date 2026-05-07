namespace Lagedra.Modules.ActivationAndBilling.Domain.Enums;

/// <summary>
/// How a <see cref="Aggregates.DealApplication"/> entered the system.
/// Used by reporting + Truth Surface canonical content to attribute
/// partner-driven bookings without overloading <c>IsPartnerReferred</c>
/// (which is used for both referral-link redemptions and direct reservations).
/// </summary>
public enum DealApplicationSource
{
    /// <summary>The tenant submitted the application themselves via the marketplace.</summary>
    TenantSelfApply,

    /// <summary>A verified partner created the application on behalf of a guest / employee / client.</summary>
    PartnerDirectReservation
}
