using System.Security.Claims;
using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Endpoints;

public static class ScreeningEndpoints
{
    public static IEndpointRouteBuilder MapScreeningEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/applications/{applicationId:guid}/screening")
            .WithTags("Screening")
            .RequireAuthorization();

        group.MapPost("/checkout", Checkout);
        group.MapGet("/", GetStatus);
        group.MapGet("/report", GetReport);
        group.MapGet("/report/pdf", GetReportPdf);

        return app;
    }

    private static async Task<IResult> Checkout(
        Guid applicationId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateScreeningCheckoutCommand(applicationId, GetUserId(user), IsAdmin(user)),
            ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetStatus(
        Guid applicationId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetScreeningStatusQuery(applicationId, GetUserId(user), IsAdmin(user)),
            ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetReport(
        Guid applicationId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetScreeningReportQuery(applicationId, GetUserId(user), IsAdmin(user)),
            ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToErrorResult(result.Error);
    }

    private static async Task<IResult> GetReportPdf(
        Guid applicationId,
        ClaimsPrincipal user,
        IMediator mediator,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetScreeningReportPdfQuery(applicationId, GetUserId(user), IsAdmin(user)),
            ct).ConfigureAwait(true);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        http.Response.Headers.CacheControl = "private, no-store";
        return Results.File(
            result.Value.Content as byte[] ?? [.. result.Value.Content],
            result.Value.ContentType,
            result.Value.FileName);
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("PlatformAdmin");

    private static IResult ToErrorResult(SharedKernel.Results.Error error)
    {
        var payload = new { error = error.Code, detail = error.Description };
        return error.Code switch
        {
            "Application.Forbidden" => Results.Json(payload, statusCode: StatusCodes.Status403Forbidden),
            "Application.NotFound" => Results.NotFound(payload),
            _ => Results.BadRequest(payload),
        };
    }
}
