namespace Lagedra.SharedKernel.Caching;

/// <summary>
/// Provider-agnostic cache abstraction. All operations are async so the
/// contract works unchanged when the implementation moves from in-memory
/// to a distributed store (Redis, Valkey, etc.).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Cache-aside convenience: returns the cached value or invokes
    /// <paramref name="factory"/>, stores the result, and returns it.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}
