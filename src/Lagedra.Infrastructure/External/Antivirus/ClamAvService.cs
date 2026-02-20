using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nClam;

namespace Lagedra.Infrastructure.External.Antivirus;

public sealed partial class ClamAvService(
    IOptions<ClamAvSettings> settings,
    ILogger<ClamAvService> logger)
    : IAntivirusService
{
    private readonly ClamAvSettings _settings = settings.Value;

    public async Task<ScanResult> ScanAsync(Stream fileStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var client = new ClamClient(_settings.Host, _settings.Port)
        {
            MaxStreamSize = 26_214_400 // 25 MB
        };

        try
        {
            var result = await client.SendAndScanFileAsync(fileStream, ct).ConfigureAwait(false);

            return result.Result switch
            {
                ClamScanResults.Clean => Clean(logger),
                ClamScanResults.VirusDetected => Infected(logger, result.InfectedFiles?.FirstOrDefault()?.VirusName),
                _ => ScanError(logger, result.RawResult)
            };
        }
#pragma warning disable CA1031 // intentional: antivirus scan must not crash the host on any exception
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogScanException(logger, ex);
            return new ScanResult(ScanStatus.Error);
        }
    }

    private static ScanResult Clean(ILogger log)
    {
        LogClean(log);
        return new ScanResult(ScanStatus.Clean);
    }

    private static ScanResult Infected(ILogger log, string? threat)
    {
        LogInfected(log, threat ?? "unknown");
        return new ScanResult(ScanStatus.Infected, threat);
    }

    private static ScanResult ScanError(ILogger log, string raw)
    {
        LogScanError(log, raw);
        return new ScanResult(ScanStatus.Error);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "ClamAV scan result: Clean")]
    private static partial void LogClean(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "ClamAV scan result: Infected — threat '{ThreatName}'")]
    private static partial void LogInfected(ILogger logger, string threatName);

    [LoggerMessage(Level = LogLevel.Error, Message = "ClamAV scan returned error: {Raw}")]
    private static partial void LogScanError(ILogger logger, string raw);

    [LoggerMessage(Level = LogLevel.Error, Message = "ClamAV scan threw an exception")]
    private static partial void LogScanException(ILogger logger, Exception ex);
}
