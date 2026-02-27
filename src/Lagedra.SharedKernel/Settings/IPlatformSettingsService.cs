namespace Lagedra.SharedKernel.Settings;

public interface IPlatformSettingsService
{
    Task<long> GetLongAsync(string key, long defaultValue, CancellationToken ct = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default);
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformSetting>> GetAllAsync(CancellationToken ct = default);
    Task SetAsync(string key, string value, string? description, Guid? updatedByUserId, CancellationToken ct = default);
}
