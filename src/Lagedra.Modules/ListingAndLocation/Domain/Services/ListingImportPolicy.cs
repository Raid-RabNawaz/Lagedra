namespace Lagedra.Modules.ListingAndLocation.Domain.Services;

/// <summary>
/// Pure policy logic for the "import from URL" feature. Holds the legal/ToS
/// guardrails (robots.txt evaluation, URL shape rules) and the fetch limits the
/// importer enforces. No I/O happens here so the rules stay deterministic and
/// unit-testable. (The per-user request rate limits are enforced separately by
/// the API gateway middleware in <c>RateLimitingSetup</c>.)
/// </summary>
public static class ListingImportPolicy
{
    /// <summary>The User-Agent the importer presents to remote hosts.</summary>
    public const string UserAgent = "Lagedra-ListingImport/1.0 (+https://lagedra.example)";

    /// <summary>Token used when matching User-agent groups in robots.txt.</summary>
    public const string UserAgentToken = "Lagedra-ListingImport";

    /// <summary>Largest response body we will read, in bytes (5 MB).</summary>
    public const long MaxResponseBytes = 5L * 1024 * 1024;

    /// <summary>How long a single fetch may take before being abandoned.</summary>
    public static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Maximum number of redirects to follow.</summary>
    public const int MaxRedirectDepth = 5;

    /// <summary>
    /// Validates that the supplied string is an absolute http(s) URL and returns
    /// the normalized <see cref="Uri"/>. Returns false for anything else
    /// (relative URLs, other schemes, malformed input).
    /// </summary>
    public static bool TryNormalizeUrl(string? input, out Uri? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        normalized = uri;
        return true;
    }

    /// <summary>
    /// Evaluates a robots.txt body for our user agent and decides whether the
    /// given path may be fetched. Implements a practical subset of the robots
    /// exclusion protocol: it selects the most specific matching user-agent
    /// group (our token beats the wildcard "*"), then applies the longest
    /// matching Allow/Disallow rule with Allow winning ties. A missing or empty
    /// robots.txt allows everything.
    /// </summary>
    public static bool IsPathAllowed(string? robotsTxt, string path)
    {
        if (string.IsNullOrWhiteSpace(robotsTxt))
        {
            return true;
        }

        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        var wildcardRules = new List<(bool Allow, string Pattern)>();
        var specificRules = new List<(bool Allow, string Pattern)>();

        // Tracks which user-agent groups the current directive block applies to.
        var appliesToWildcard = false;
        var appliesToUs = false;
        var sawAgentLine = false;

        foreach (var rawLine in robotsTxt.Split('\n'))
        {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            var field = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (field.Equals("user-agent", StringComparison.OrdinalIgnoreCase))
            {
                // A new run of user-agent lines starts a fresh group.
                if (!sawAgentLine)
                {
                    appliesToWildcard = false;
                    appliesToUs = false;
                }

                sawAgentLine = true;
                if (value == "*")
                {
                    appliesToWildcard = true;
                }
                else if (value.Contains(UserAgentToken, StringComparison.OrdinalIgnoreCase))
                {
                    appliesToUs = true;
                }
            }
            else if (field.Equals("disallow", StringComparison.OrdinalIgnoreCase) ||
                     field.Equals("allow", StringComparison.OrdinalIgnoreCase))
            {
                sawAgentLine = false;
                var allow = field.Equals("allow", StringComparison.OrdinalIgnoreCase);
                if (appliesToUs)
                {
                    specificRules.Add((allow, value));
                }

                if (appliesToWildcard)
                {
                    wildcardRules.Add((allow, value));
                }
            }
            else
            {
                sawAgentLine = false;
            }
        }

        // Prefer rules that explicitly target our agent; otherwise fall back to "*".
        var rules = specificRules.Count > 0 ? specificRules : wildcardRules;
        if (rules.Count == 0)
        {
            return true;
        }

        var bestMatchLength = -1;
        var allowed = true;
        foreach (var (allow, pattern) in rules)
        {
            // An empty Disallow means "allow all" and matches nothing meaningful.
            if (pattern.Length == 0)
            {
                continue;
            }

            if (!PathMatches(path, pattern))
            {
                continue;
            }

            if (pattern.Length > bestMatchLength ||
                (pattern.Length == bestMatchLength && allow))
            {
                bestMatchLength = pattern.Length;
                allowed = allow;
            }
        }

        return allowed;
    }

    private static string StripComment(string line)
    {
        var hash = line.IndexOf('#', StringComparison.Ordinal);
        return hash < 0 ? line : line[..hash];
    }

    private static bool PathMatches(string path, string pattern)
    {
        // Support the common '*' wildcard and '$' end-anchor extensions.
        var anchoredEnd = pattern.EndsWith('$');
        if (anchoredEnd)
        {
            pattern = pattern[..^1];
        }

        var segments = pattern.Split('*');
        var cursor = 0;
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0)
            {
                continue;
            }

            if (i == 0)
            {
                // First segment must match at the start of the path.
                if (!path.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                cursor = segment.Length;
            }
            else
            {
                var idx = path.IndexOf(segment, cursor, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    return false;
                }

                cursor = idx + segment.Length;
            }
        }

        if (anchoredEnd && !pattern.Contains('*', StringComparison.Ordinal))
        {
            return string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}
