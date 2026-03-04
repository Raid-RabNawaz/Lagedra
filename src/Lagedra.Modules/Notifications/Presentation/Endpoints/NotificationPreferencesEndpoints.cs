using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Application.Queries;
using Lagedra.Modules.Notifications.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Notifications.Presentation.Endpoints;

public static class NotificationPreferencesEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/preferences/{userId:guid}", GetUserPreferences);
        group.MapPut("/preferences/{userId:guid}", UpdateUserPreferences);
        group.MapGet("/history/{userId:guid}", GetNotificationHistory);

        return app;
    }

    private static async Task<IResult> GetUserPreferences(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserPreferencesQuery(userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpdateUserPreferences(
        [FromRoute] Guid userId,
        [FromBody] UpdatePreferencesRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateUserPreferencesCommand(userId, request.EventOptIns), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetNotificationHistory(
        [FromRoute] Guid userId,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ListNotificationHistoryQuery(userId), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
