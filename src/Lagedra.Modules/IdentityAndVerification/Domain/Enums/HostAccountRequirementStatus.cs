namespace Lagedra.Modules.IdentityAndVerification.Domain.Enums;

/// <summary>
/// Normalized state of a Stripe Connect onboarding requirement group (tax forms
/// such as W-9/W-8, or the external bank account). Derived from the connected
/// account's <c>requirements</c> + capability flags during status sync.
/// </summary>
public enum HostAccountRequirementStatus
{
    /// <summary>No information submitted yet (onboarding not started/incomplete).</summary>
    Unknown,

    /// <summary>Submitted and awaiting Stripe verification.</summary>
    Pending,

    /// <summary>Verified by Stripe; no outstanding requirements.</summary>
    Verified,

    /// <summary>Past-due or disabled — the host must resolve before payouts continue.</summary>
    Restricted
}
