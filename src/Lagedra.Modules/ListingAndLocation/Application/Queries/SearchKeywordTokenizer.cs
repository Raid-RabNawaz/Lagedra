namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

/// <summary>
/// Splits a marketplace search string into AND-tokens so "Austin, TX"
/// matches a listing in Austin rather than requiring that exact phrase
/// in the title.
/// </summary>
public static class SearchKeywordTokenizer
{
    private static readonly char[] Separators = [',', ';', '/', '|'];

    public static IReadOnlyList<string> Tokenize(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        var tokens = keyword
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(part => part.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(token => token.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tokens.Count > 0 ? tokens : [keyword.Trim()];
    }

    /// <summary>
    /// US ZIP (5 or ZIP+4) or Canadian postal code, compacted for comparison.
    /// Returns null when the text is not a postal code.
    /// </summary>
    public static string? NormalizePostalCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (compact.Length is 5 or 9 && compact.All(char.IsDigit))
        {
            return compact[..5];
        }

        if (compact.Length == 6 && IsCanadianPostal(compact))
        {
            return compact;
        }

        return null;
    }

    private static bool IsCanadianPostal(string compact) =>
        char.IsLetter(compact[0]) && char.IsDigit(compact[1]) &&
        char.IsLetter(compact[2]) && char.IsDigit(compact[3]) &&
        char.IsLetter(compact[4]) && char.IsDigit(compact[5]);
}
