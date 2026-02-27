namespace Lagedra.Auth.Application.Settings;

public sealed class ExternalAuthSettings
{
    public const string SectionName = "ExternalAuth";

    public GoogleSettings Google { get; init; } = new();
    public AppleSettings Apple { get; init; } = new();
    public MicrosoftSettings Microsoft { get; init; } = new();
}

public sealed class GoogleSettings
{
    public string ClientId { get; init; } = string.Empty;
}

public sealed class AppleSettings
{
    public string ClientId { get; init; } = string.Empty;
    public string TeamId { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
}

public sealed class MicrosoftSettings
{
    public string ClientId { get; init; } = string.Empty;
    public string TenantId { get; init; } = "common";
}
