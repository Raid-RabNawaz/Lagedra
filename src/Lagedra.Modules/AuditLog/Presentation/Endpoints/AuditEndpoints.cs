using Lagedra.Modules.AuditLog.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.AuditLog.Presentation.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/audit")
            .WithTags("Audit")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/", SearchEvents);

        return app;
    }

    private static async Task<IResult> SearchEvents(
        IMediator mediator,
        Guid? userId,
        string? eventType,
        string? entityType,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchAuditEventsQuery(userId, eventType, entityType, startDate, endDate, page, pageSize),
            ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: 500, detail: result.Error.Description);
    }
}
