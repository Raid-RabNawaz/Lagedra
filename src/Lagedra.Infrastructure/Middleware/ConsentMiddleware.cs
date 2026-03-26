using System.Security.Claims;
using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Middleware;

public sealed partial class ConsentMiddleware(
    RequestDelegate next,
    ILogger<ConsentMiddleware> logger,
    IHostEnvironment environment,
    IConfiguration configuration)
{
    private static readonly TimeSpan ConsentCacheTtl = TimeSpan.FromMinutes(10);

    private static readonly string[] ExemptPrefixes =
    [
        "/health", "/swagger", "/hubs",
        "/v1/auth", "/v1/webhook",
        "/v1/blog", "/v1/seo",
        "/v1/listings/search", "/v1/listings/definitions",
        "/v1/privacy/consent", "/v1/privacy/consents",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var enforced = configuration.GetValue("ConsentEnforcement:Enabled", defaultValue: !environment.IsDevelopment());
        if (!enforced)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsExemptPath(path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var cache = context.RequestServices.GetRequiredService<ICacheService>();
        var cacheKey = $"user:consent:{userId}";

        var hasConsents = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var checker = context.RequestServices.GetRequiredService<IConsentChecker>();
                return await checker.HasRequiredConsentsAsync(userId, ct).ConfigureAwait(false);
            },
            ConsentCacheTtl,
            context.RequestAborted).ConfigureAwait(false);

        if (!hasConsents)
        {
            LogMissingConsent(logger, userId, path);
            context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7725#section-3",
                title = "Consent Required",
                status = 451,
                detail = "KYC and Data Processing consent must be granted before using this endpoint."
            }, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsExemptPath(string path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Missing required consents for user {UserId} on {Path}")]
    private static partial void LogMissingConsent(ILogger logger, Guid userId, string path);
}

public static class ConsentMiddlewareExtensions
{
    public static IApplicationBuilder UseConsentEnforcement(this IApplicationBuilder app) =>
        app.UseMiddleware<ConsentMiddleware>();
}
