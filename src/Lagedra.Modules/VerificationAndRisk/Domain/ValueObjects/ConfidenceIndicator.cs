using Lagedra.SharedKernel.Domain;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Domain.ValueObjects;

public sealed class ConfidenceIndicator : ValueObject
{
    public ConfidenceLevel Level { get; private set; }
    public string Reason { get; private set; }

#pragma warning disable CS8618
    private ConfidenceIndicator() { }
#pragma warning restore CS8618

    public static ConfidenceIndicator Create(ConfidenceLevel level, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new ConfidenceIndicator { Level = level, Reason = reason };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return Reason;
    }
}
