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
}
