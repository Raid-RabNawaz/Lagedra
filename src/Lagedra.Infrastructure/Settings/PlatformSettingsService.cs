using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Infrastructure.Settings;

public sealed class PlatformSettingsService(
    PlatformSettingsDbContext dbContext,
    ICacheService cache)
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

        await cache.RemoveAsync($"platform_setting:{key}", ct).ConfigureAwait(false);
    }

    private async Task<string?> GetValueAsync(string key, CancellationToken ct)
    {
        var cacheKey = $"platform_setting:{key}";

        var cached = await cache.GetAsync<string>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var setting = await dbContext.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct)
            .ConfigureAwait(false);

        var value = setting?.Value;

        if (value is not null)
        {
            await cache.SetAsync(cacheKey, value, CacheDuration, ct).ConfigureAwait(false);
        }

        return value;
    }
}
