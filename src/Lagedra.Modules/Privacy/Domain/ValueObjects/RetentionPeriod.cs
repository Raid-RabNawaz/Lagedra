using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.ValueObjects;

public sealed class RetentionPeriod : ValueObject
{
    public static readonly int CoreRecordYears = 7;
    public static readonly int InactiveProfileYears = 2;
    public static readonly int CancelledPreActivationDays = 30;

    public int Value { get; }

    public RetentionPeriod(int value)
    {
        Value = value;
    }

    private RetentionPeriod() { }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
