using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Observability;

public sealed partial class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

#pragma warning disable CA1031 // intentional: global exception handler must catch all
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue("CorrelationId", out var cid)
                ? cid?.ToString() ?? "unknown"
                : "unknown";

            LogUnhandledException(
                logger,
                context.Request.Method,
                context.Request.Path,
                correlationId,
                ex);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetailsResponse
            {
                Type = "https://lagedra.com/errors/internal-server-error",
                Title = "An unexpected error occurred.",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = environment.IsDevelopment()
                    ? $"{ex.GetType().Name}: {ex.Message}"
                    : $"An internal error occurred. Reference ID: {correlationId}",
                TraceId = correlationId
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problem, JsonOptions)).ConfigureAwait(false);
        }
    }
#pragma warning restore CA1031

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception on {HttpMethod} {RequestPath} [CorrelationId={CorrelationId}]")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string httpMethod,
        string requestPath,
        string correlationId,
        Exception exception);
}

internal sealed class ProblemDetailsResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string TraceId { get; init; } = string.Empty;
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
