using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure.Middleware;

public static class RateLimitingSetup
{
    public const string DisputeCapPolicy = "DisputeCap";

    /// <summary>
    /// Per-user cap for the "import listing from URL" endpoint. Enforces both a
    /// rolling-hour and a rolling-day limit (see <see cref="ListingImportPerHour"/>
    /// and <see cref="ListingImportPerDay"/>).
    /// </summary>
    public const string ListingImportPolicy = "ListingImport";

    private const int ListingImportPerHour = 5;
    private const int ListingImportPerDay = 30;
    private const string ListingImportPath = "/v1/listings/import-from-url";

    public static IServiceCollection AddLagedraRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(DisputeCapPolicy, httpContext =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"dispute:{userId}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromDays(30),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(ListingImportPolicy, httpContext =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

                return RateLimitPartition.Get(
                    partitionKey: $"listing-import:{userId}",
                    factory: _ => new CompositeRateLimiter(
                        new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = ListingImportPerHour,
                            Window = TimeSpan.FromHours(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }),
                        new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = ListingImportPerDay,
                            Window = TimeSpan.FromDays(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        })));
            });

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";

                var detail = context.HttpContext.Request.Path.StartsWithSegments(
                        ListingImportPath, StringComparison.OrdinalIgnoreCase)
                    ? "Import limit reached. You can import up to 5 listings per hour and 30 per day. Please try again later."
                    : "Monthly dispute limit reached. Please try again next month.";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail
                }, ct).ConfigureAwait(false);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseLagedraRateLimiting(this IApplicationBuilder app) =>
        app.UseRateLimiter();
}
