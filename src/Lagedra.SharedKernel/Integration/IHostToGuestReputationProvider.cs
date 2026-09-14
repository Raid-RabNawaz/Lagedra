namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Host → guest review aggregate for trust-tier factors. Implemented by Reviews.
/// </summary>
public interface IHostToGuestReputationProvider
{
    Task<HostToGuestReputationDto> GetAsync(Guid tenantUserId, CancellationToken ct = default);
}

public sealed record HostToGuestReputationDto(int ReviewCount, double AverageOverall);
