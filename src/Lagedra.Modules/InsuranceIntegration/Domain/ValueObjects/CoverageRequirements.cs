using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.InsuranceIntegration.Domain.ValueObjects;

public sealed class CoverageRequirements : ValueObject
{
    public string MinimumCoverageType { get; }
    public long MinimumAmountCents { get; }

    public CoverageRequirements(string minimumCoverageType, long minimumAmountCents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(minimumCoverageType);
        MinimumCoverageType = minimumCoverageType;
        MinimumAmountCents = minimumAmountCents;
    }

#pragma warning disable CS8618
    private CoverageRequirements() { }
#pragma warning restore CS8618

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinimumCoverageType;
        yield return MinimumAmountCents;
    }
}
