namespace Lagedra.Infrastructure.External.Checkr;

public sealed class CheckrSettings
{
    public const string SectionName = "Checkr";

    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PackageSlug { get; set; } = "essential";
    public string ApiVersion { get; set; } = "2026-04-29";
    public Uri BaseUrl { get; set; } = new("https://tenant.checkr.com/api/");

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey)
        && !SecretKey.Contains("REPLACE", StringComparison.OrdinalIgnoreCase);
}
