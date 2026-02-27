using Lagedra.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lagedra.Infrastructure.Settings;

public sealed class PlatformSettingsService(
    PlatformSettingsDbContext dbContext,
    IMemoryCache cache)
    : IPlatformSettingsService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<long> GetLongAsync(string key, long defaultValue, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct).ConfigureAwait(false);
        return value is not null && long.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct).ConfigureAwait(false);
        return value is not null && bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        return await GetValueAsync(key, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PlatformSetting>> GetAllAsync(CancellationToken ct = default)
    {
        return await dbContext.PlatformSettings
            .AsNoTracking()
            .OrderBy(s => s.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task SetAsync(
        string key, string value, string? description,
        Guid? updatedByUserId, CancellationToken ct = default)
    {
        var setting = await dbContext.PlatformSettings
            .FirstOrDefaultAsync(s => s.Key == key, ct)
            .ConfigureAwait(false);

        if (setting is null)
        {
            setting = PlatformSetting.Create(key, value, description);
            dbContext.PlatformSettings.Add(setting);
        }
        else
        {
            setting.Update(value, updatedByUserId);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        cache.Remove($"platform_setting:{key}");
    }

    private async Task<string?> GetValueAsync(string key, CancellationToken ct)
    {
        var cacheKey = $"platform_setting:{key}";

        if (cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        var setting = await dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct)
            .ConfigureAwait(false);

        var value = setting?.Value;

        cache.Set(cacheKey, value, CacheDuration);

        return value;
    }
}
