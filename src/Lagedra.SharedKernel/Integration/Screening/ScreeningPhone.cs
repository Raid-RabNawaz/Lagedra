namespace Lagedra.SharedKernel.Integration.Screening;

/// <summary>
/// Checkr Tenant accepts real US subscriber numbers only. Non-US, toll-free,
/// and reserved 555-exchange numbers must be omitted — sending them returns 422.
/// </summary>
public static class ScreeningPhone
{
    private static readonly HashSet<string> TollFreeNpa =
    [
        "800", "833", "844", "855", "866", "877", "888"
    ];

    public static string? UsOrNull(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        if (digits.Length != 10)
        {
            return null;
        }

        var npa = digits[..3];
        var nxx = digits[3..6];
        if (TollFreeNpa.Contains(npa) || nxx == "555")
        {
            return null;
        }

        return $"+1{digits}";
    }
}
