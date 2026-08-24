using System.Net;

namespace Lagedra.Modules.ListingAndLocation.Domain.Services;

/// <summary>
/// Guardrails for fetching listing photos from third-party URLs (XML feed
/// import). Pure functions so the http(s)-only / no-private-IP rules stay
/// unit-testable without spinning up an HTTP stack.
/// </summary>
public static class ListingMediaImportPolicy
{
    public const string HttpClientName = "ListingMediaImport";

    /// <summary>Kept in sync with the create-listing URL importer and the web client.</summary>
    public const int MaxPhotos = 20;

    public const long MaxImageBytes = 15L * 1024 * 1024;

    public static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(25);

    public const int FetchConcurrency = 3;

    public const int MaxRedirects = 3;

    /// <summary>
    /// Accepts an absolute http(s) URL whose host is not loopback / private /
    /// link-local. Hostname-based SSRF (DNS rebinding) is not fully solvable
    /// here; we block the obvious literal-IP cases.
    /// </summary>
    public static bool TryNormalizePublicHttpUrl(string? input, out Uri? normalized)
    {
        normalized = null;
        if (!ListingImportPolicy.TryNormalizeUrl(input, out var uri) || uri is null)
        {
            return false;
        }

        if (uri.IsLoopback)
        {
            return false;
        }

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(host, out var ip) && IsNonPublic(ip))
        {
            return false;
        }

        normalized = uri;
        return true;
    }

    /// <summary>
    /// Resolves a MIME type from the response Content-Type, falling back to
    /// the URL's file extension. Returns null when we cannot confirm an image.
    /// </summary>
    public static string? ResolveImageMime(string? contentType, Uri source)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var mime = contentType.Split(';', 2)[0].Trim();
            if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                && !mime.Equals("image/*", StringComparison.OrdinalIgnoreCase))
            {
                return mime;
            }
        }

        var ext = Path.GetExtension(source.AbsolutePath);
        if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return "image/jpeg";
        }

        if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            return "image/gif";
        }

        if (ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return "image/webp";
        }

        if (ext.Equals(".heic", StringComparison.OrdinalIgnoreCase))
        {
            return "image/heic";
        }

        if (ext.Equals(".heif", StringComparison.OrdinalIgnoreCase))
        {
            return "image/heif";
        }

        return null;
    }

    public static string FileNameFromUrl(Uri source, int index)
    {
        var last = Path.GetFileName(source.AbsolutePath);
        if (!string.IsNullOrWhiteSpace(last) && last.Contains('.', StringComparison.Ordinal))
        {
            return SanitizeFileName(last);
        }

        return $"imported-photo-{index + 1}.jpg";
    }

    public static string SanitizeFileName(string fileName)
    {
        var cleaned = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(cleaned.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
    }

    private static bool IsNonPublic(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)
            || ip.Equals(IPAddress.Any)
            || ip.Equals(IPAddress.IPv6Any)
            || ip.IsIPv6LinkLocal
            || ip.IsIPv6SiteLocal
            || ip.IsIPv6UniqueLocal)
        {
            return true;
        }

        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = ip.GetAddressBytes();
        return bytes[0] == 10
            || bytes[0] == 127
            || (bytes[0] == 169 && bytes[1] == 254)
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168);
    }
}
