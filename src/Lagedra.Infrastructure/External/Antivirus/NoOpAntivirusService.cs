namespace Lagedra.Infrastructure.External.Antivirus;

public sealed class NoOpAntivirusService : IAntivirusService
{
    public Task<ScanResult> ScanAsync(Stream fileStream, CancellationToken ct = default)
        => Task.FromResult(new ScanResult(ScanStatus.Clean));
}
