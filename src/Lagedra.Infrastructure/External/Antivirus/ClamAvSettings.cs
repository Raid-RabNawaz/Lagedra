namespace Lagedra.Infrastructure.External.Antivirus;

public sealed class ClamAvSettings
{
    public const string SectionName = "ClamAV";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 3310;
    public int TimeoutSeconds { get; init; } = 30;
}
