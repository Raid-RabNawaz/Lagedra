using System.Text.RegularExpressions;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.ValueObjects;

public sealed partial class JurisdictionCode : ValueObject
{
    public string Code { get; }

    private JurisdictionCode(string code)
    {
        Code = code;
    }

    public static JurisdictionCode Create(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!JurisdictionCodePattern().IsMatch(code))
        {
            throw new ArgumentException(
                $"Jurisdiction code '{code}' must follow the format XX-YY-ZZ (e.g. US-CA-LA).",
                nameof(code));
        }

        return new JurisdictionCode(code.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public override string ToString() => Code;

    [GeneratedRegex(@"^[A-Z]{2}-[A-Z]{2}-[A-Z]{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex JurisdictionCodePattern();
}
