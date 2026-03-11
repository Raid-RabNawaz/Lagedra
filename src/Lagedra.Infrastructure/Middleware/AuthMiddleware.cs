using System.Security.Claims;
using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Middleware;

public sealed partial class AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
{
    private static readonly TimeSpan ActiveCacheTtl = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.Identity?.IsAuthenticated != true)
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

        var role = context.User.FindFirstValue(ClaimTypes.Role)
                   ?? context.User.FindFirstValue("role")
                   ?? string.Empty;

        context.Items["UserId"] = userId;
        context.Items["UserRole"] = role;

        var cache = context.RequestServices.GetRequiredService<ICacheService>();
        var cacheKey = $"user:active:{userId}";

        var isActive = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var provider = context.RequestServices.GetRequiredService<IUserStatusProvider>();
                return await provider.IsActiveAsync(userId, ct).ConfigureAwait(false);
            },
            ActiveCacheTtl,
            context.RequestAborted).ConfigureAwait(false);

        if (!isActive)
        {
            LogInactiveUser(logger, userId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Forbidden",
                status = 403,
                detail = "Account is deactivated."
            }, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Blocked request from deactivated user {UserId}")]
    private static partial void LogInactiveUser(ILogger logger, Guid userId);
}

public static class AuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthEnforcement(this IApplicationBuilder app) =>
        app.UseMiddleware<AuthMiddleware>();
}
