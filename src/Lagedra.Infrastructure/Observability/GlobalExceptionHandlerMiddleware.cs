using System.Net;
using System.Text.Json;
using FluentValidation;
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
        catch (ValidationException vex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetailsResponse
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "One or more validation errors occurred.",
                Status = (int)HttpStatusCode.BadRequest,
                Errors = errors
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problem, JsonOptions)).ConfigureAwait(false);
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

internal sealed class ValidationProblemDetailsResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public IDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
