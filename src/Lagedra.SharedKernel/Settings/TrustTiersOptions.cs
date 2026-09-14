namespace Lagedra.SharedKernel.Settings;

/// <summary>
/// Numeric thresholds for <see cref="Integration.TenantVerificationTier"/>
/// assignment. Override in configuration without a code change.
/// </summary>
public sealed class TrustTiersOptions
{
    public const string SectionName = "TrustTiers";

    /// <summary>How many qualifying factors are required to reach Trusted.</summary>
    public int TrustedMinFactors { get; set; } = 2;

    public int TrustedMinStays { get; set; } = 2;
    public int TrustedMinReviews { get; set; } = 2;
    public double TrustedMinRating { get; set; } = 4.0;

    public int EliteMinStays { get; set; } = 5;
    public int EliteMinReviews { get; set; } = 4;
    public double EliteMinRating { get; set; } = 4.5;

    /// <summary>
    /// A screening Pass older than this many days does not count as a factor.
    /// </summary>
    public int ScreeningFactorTtlDays { get; set; } = 365;
}
