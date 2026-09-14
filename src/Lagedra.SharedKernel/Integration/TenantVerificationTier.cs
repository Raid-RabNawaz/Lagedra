namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// The tenant's Lagedra trust standing. Drives the predetermined deposit
/// charged on a reservation request. Higher tiers represent more platform
/// history (and optionally a recent screening Pass as one factor) and
/// therefore a lower deposit.
/// </summary>
public enum TenantVerificationTier
{
    /// <summary>Identity verification is not complete.</summary>
    Unverified = 0,

    /// <summary>Government ID complete. Screening is not required.</summary>
    Verified = 1,

    /// <summary>
    /// Identity complete, clean compliance gates, and at least two
    /// qualifying factors (stays, host reviews, and/or a recent screening Pass).
    /// </summary>
    Trusted = 2,

    /// <summary>
    /// Trusted-level history plus an active partner endorsement.
    /// Screening never grants this tier by itself.
    /// </summary>
    Elite = 3,

    /// <summary>
    /// Suspended/banned or platform-ban. Cannot apply. Screening never sets this.
    /// </summary>
    Blocklisted = 4
}
