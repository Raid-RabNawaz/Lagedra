using System.Text.RegularExpressions;

namespace Lagedra.Modules.LeaseAgreements.Application.Services;

/// <summary>
/// Lightweight <c>{{#if key}}</c> / <c>{{#unless key}}</c> blocks for lease
/// HTML. Nested blocks are not supported — keep conditions flat.
/// </summary>
public static partial class LeaseTemplateConditionals
{
    public static string Apply(string html, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(values);

        var current = html;
        for (var i = 0; i < 8; i++)
        {
            var next = IfBlockRegex().Replace(current, m =>
                IsTruthy(values, m.Groups[1].Value) ? m.Groups[2].Value : string.Empty);
            next = UnlessBlockRegex().Replace(next, m =>
                IsTruthy(values, m.Groups[1].Value) ? string.Empty : m.Groups[2].Value);

            if (string.Equals(next, current, StringComparison.Ordinal))
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    public static bool IsTruthy(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key.Trim(), out var value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim() switch
        {
            "0" or "n" or "N" => false,
            var v when v.Equals("no", StringComparison.OrdinalIgnoreCase) => false,
            var v when v.Equals("false", StringComparison.OrdinalIgnoreCase) => false,
            _ => true
        };
    }

    [GeneratedRegex(@"\{\{#if\s+([a-zA-Z0-9_.]+)\s*\}\}(.*?)\{\{/if\}\}", RegexOptions.Singleline)]
    private static partial Regex IfBlockRegex();

    [GeneratedRegex(@"\{\{#unless\s+([a-zA-Z0-9_.]+)\s*\}\}(.*?)\{\{/unless\}\}", RegexOptions.Singleline)]
    private static partial Regex UnlessBlockRegex();
}
