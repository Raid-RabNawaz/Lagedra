using System.IO;
using System.Text;
using Lagedra.Infrastructure.Caching;
using Lagedra.SharedKernel.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Lagedra.Infrastructure.Middleware;

/// <summary>
/// Idempotency middleware. When request includes Idempotency-Key header,
/// caches the response (24h TTL) and returns cached response for duplicate keys.
/// Applies to POST, PUT, PATCH methods only.
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next, ICacheService cacheService)
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly HashSet<string> ApplicableMethods = ["POST", "PUT", "PATCH"];

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!ApplicableMethods.Contains(context.Request.Method))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var key = keyValues.ToString()!.Trim();
        var cacheKey = CacheKeys.Idempotency($"{context.Request.Method}:{context.Request.Path}:{key}");

        var cached = await cacheService.GetAsync<CachedResponse>(cacheKey, context.RequestAborted).ConfigureAwait(false);
        if (cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            foreach (var (name, value) in cached.Headers)
            {
                context.Response.Headers[name] = value;
            }

            await context.Response.WriteAsync(cached.Body, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var originalBodyStream = context.Response.Body;
        await using var bufferStream = new MemoryStream();
        context.Response.Body = bufferStream;

        try
        {
            await next(context).ConfigureAwait(false);

            bufferStream.Position = 0;
            string body;
            using (var reader = new StreamReader(bufferStream))
            {
                body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
            }

            var headers = context.Response.Headers
                .Where(h => !string.Equals(h.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            var cachedResponse = new CachedResponse(
                context.Response.StatusCode,
                headers,
                body);

            await cacheService.SetAsync(cacheKey, cachedResponse, CacheTtl, context.RequestAborted).ConfigureAwait(false);

            bufferStream.Position = 0;
            await bufferStream.CopyToAsync(originalBodyStream, context.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private sealed record CachedResponse(int StatusCode, IReadOnlyDictionary<string, string> Headers, string Body);
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app) =>
        app.UseMiddleware<IdempotencyMiddleware>();
}
