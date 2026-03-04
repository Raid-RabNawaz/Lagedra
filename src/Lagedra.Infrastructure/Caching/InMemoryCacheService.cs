using Lagedra.SharedKernel.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Lagedra.Infrastructure.Caching;

public sealed class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var value = memoryCache.TryGetValue(key, out T? cached) ? cached : default;
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        memoryCache.Set(key, value, expiration ?? DefaultExpiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (memoryCache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory(ct).ConfigureAwait(false);
        memoryCache.Set(key, value, expiration ?? DefaultExpiration);
        return value;
    }
}
