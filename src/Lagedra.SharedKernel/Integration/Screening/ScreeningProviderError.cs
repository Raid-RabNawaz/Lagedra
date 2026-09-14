namespace Lagedra.SharedKernel.Integration.Screening;

/// <summary>
/// Turns a screening-provider validation error into copy a host can act on.
/// </summary>
public static class ScreeningProviderError
{
    public static string ToHostMessage(
        int statusCode,
        IReadOnlyList<(string? Pointer, string? Detail)> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var parts = errors
            .Select(MapOne)
            .Where(static m => !string.IsNullOrWhiteSpace(m))
            .Select(static m => m!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return parts.Length > 0
            ? string.Join(" ", parts)
            : $"The screening provider could not start this check ({statusCode}).";
    }

    private static string? MapOne((string? Pointer, string? Detail) error)
    {
        var pointer = error.Pointer ?? string.Empty;
        var detail = (error.Detail ?? string.Empty).Trim();
        var haystack = $"{pointer} {detail}";

        if (Contains(haystack, "phone"))
        {
            return AppendAdvice(
                detail,
                "Checkr only accepts a real US mobile — not an 800 or 555 example number.");
        }

        if (Contains(pointer, "property.state") || Contains(haystack, "state must be a valid"))
        {
            return AppendAdvice(
                detail,
                "Set the listing state to a two-letter US code (for example CA).");
        }

        if (Contains(pointer, "property.zip") || Contains(haystack, "zip"))
        {
            return AppendAdvice(detail, "Update the listing ZIP code and try again.");
        }

        if (Contains(pointer, "property") || Contains(haystack, "address"))
        {
            return AppendAdvice(detail, "Update the listing street address and try again.");
        }

        if (Contains(haystack, "payment method"))
        {
            return AppendAdvice(detail, "Contact support — Checkr cannot bill this order yet.");
        }

        return string.IsNullOrWhiteSpace(detail) ? null : detail;
    }

    private static string AppendAdvice(string detail, string advice)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return advice;
        }

        var ended = detail.EndsWith('.') || detail.EndsWith('!') ? detail : detail + ".";
        return $"{ended} {advice}";
    }

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
