namespace Lagedra.Infrastructure.External.Antivirus;

public enum ScanStatus { Clean, Infected, Error }

public sealed record ScanResult(ScanStatus Status, string? ThreatName = null);

public interface IAntivirusService
{
    Task<ScanResult> ScanAsync(Stream fileStream, CancellationToken ct = default);
}
