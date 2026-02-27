using System.Security.Claims;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.Notifications.Presentation.Endpoints;

public static class InAppNotificationEndpoints
{
    public static IEndpointRouteBuilder MapInAppNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/notifications")
            .WithTags("InAppNotifications")
            .RequireAuthorization();

        group.MapGet("/unread", GetUnread);
        group.MapGet("/unread/count", GetUnreadCount);
        group.MapPost("/{notificationId:guid}/read", MarkRead);
        group.MapPost("/read-all", MarkAllRead);

        return app;
    }

    private static async Task<IResult> GetUnread(
        ClaimsPrincipal user,
        IMediator mediator,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new GetUnreadNotificationsQuery(userId, limit), ct).ConfigureAwait(false);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetUnreadCount(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new GetUnreadCountQuery(userId), ct).ConfigureAwait(false);
        return Results.Ok(new { count = result.Value });
    }

    private static async Task<IResult> MarkRead(
        [FromRoute] Guid notificationId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        var result = await mediator.Send(new MarkNotificationReadCommand(notificationId, userId), ct).ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok()
            : Results.NotFound(new { error = result.Error.Code });
    }

    private static async Task<IResult> MarkAllRead(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        await mediator.Send(new MarkAllNotificationsReadCommand(userId), ct).ConfigureAwait(false);
        return Results.Ok(new { message = "All notifications marked as read." });
    }

    private static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
