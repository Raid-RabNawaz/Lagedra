using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;

public enum ConfidenceLevel
{
    High,
    Medium,
    Low
}

public sealed class ConfidenceIndicator : ValueObject
{
    public ConfidenceLevel Level { get; }
    public string Reason { get; }

    public ConfidenceIndicator(ConfidenceLevel level, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Level = level;
        Reason = reason;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Level;
        yield return Reason;
    }
}
