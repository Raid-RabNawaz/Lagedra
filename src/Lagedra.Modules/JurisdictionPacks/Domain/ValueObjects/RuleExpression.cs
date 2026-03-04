using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.ValueObjects;

public sealed class RuleExpression : ValueObject
{
    public string Expression { get; }

    private RuleExpression(string expression)
    {
        Expression = expression;
    }

    public static RuleExpression Create(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        return new RuleExpression(expression);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Expression;
    }

    public override string ToString() => Expression;
}
