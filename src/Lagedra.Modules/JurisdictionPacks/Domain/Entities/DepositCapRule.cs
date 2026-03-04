using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Entities;

public sealed class DepositCapRule : Entity<Guid>
{
    public Guid VersionId { get; private set; }
    public string JurisdictionCode { get; private set; } = string.Empty;

    /// <summary>
    /// Maximum deposit as a multiplier of monthly rent (e.g. 1.0 = 1x monthly rent).
    /// </summary>
    public decimal MaxMultiplier { get; private set; }

    /// <summary>
    /// Optional condition under which a different cap applies (e.g. "small-landlord", "furnished").
    /// </summary>
    public string? ExceptionCondition { get; private set; }

    /// <summary>
    /// Multiplier to use when the exception condition is met.
    /// </summary>
    public decimal? ExceptionMultiplier { get; private set; }

    /// <summary>
    /// Legal citation (e.g. "CA Civil Code §1950.5").
    /// </summary>
    public string LegalReference { get; private set; } = string.Empty;

    private DepositCapRule() { }

    internal static DepositCapRule Create(
        Guid versionId,
        string jurisdictionCode,
        decimal maxMultiplier,
        string legalReference,
        string? exceptionCondition = null,
        decimal? exceptionMultiplier = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalReference);

        if (maxMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMultiplier), "Max multiplier must be positive.");
        }

        return new DepositCapRule
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            JurisdictionCode = jurisdictionCode.ToUpperInvariant(),
            MaxMultiplier = maxMultiplier,
            ExceptionCondition = exceptionCondition,
            ExceptionMultiplier = exceptionMultiplier,
            LegalReference = legalReference
        };
    }
}
