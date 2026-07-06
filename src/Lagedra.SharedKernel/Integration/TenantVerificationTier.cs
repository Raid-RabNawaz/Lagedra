namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// The tenant's verification standing at the moment a reservation request is
/// submitted. Drives which predetermined deposit amount a listing charges.
/// Higher tiers represent more trust and therefore a lower deposit.
/// </summary>
public enum TenantVerificationTier
{
    /// <summary>
    /// No completed background check and no active partner endorsement.
    /// Pays the full (unverified) deposit.
    /// </summary>
    Unverified = 0,

    /// <summary>
    /// Identity verified AND background check passed. Pays the reduced
    /// background-verified deposit.
    /// </summary>
    BackgroundVerified = 1,

    /// <summary>
    /// Connected with an approved/registered partner via an active
    /// endorsement. Pays the lowest, partner-guaranteed deposit.
    /// </summary>
    PartnerGuaranteed = 2
}
