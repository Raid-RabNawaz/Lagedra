using Lagedra.Modules.ContentManagement.Application.Commands;
using Lagedra.Modules.ContentManagement.Application.Queries;
using Lagedra.Modules.ContentManagement.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ContentManagement.Presentation.Endpoints;

public static class AdminBlogEndpoints
{
    public static IEndpointRouteBuilder MapAdminBlogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/admin/blog")
            .WithTags("AdminBlog")
            .RequireAuthorization();

        group.MapPost("/", CreateBlogPost);
        group.MapPut("/{id:guid}", UpdateBlogPost);
        group.MapPost("/{id:guid}/publish", PublishBlogPost);
        group.MapPost("/{id:guid}/archive", ArchiveBlogPost);
        group.MapGet("/", GetAllAdmin);

        var pagesGroup = app.MapGroup("/api/v1/admin/pages")
            .WithTags("AdminPages")
            .RequireAuthorization();

        pagesGroup.MapPut("/{slug}", UpsertSeoPage);

        return app;
    }

    private static async Task<IResult> CreateBlogPost(
        [FromBody] CreateBlogPostRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateBlogPostCommand(
                request.Slug, request.Title, request.Excerpt, request.Content,
                request.AuthorUserId, request.Tags, request.MetaTitle,
                request.MetaDescription, request.OgImageUrl, request.ReadingTimeMinutes), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Created($"/api/v1/blog/{result.Value.Slug}", result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpdateBlogPost(
        [FromRoute] Guid id,
        [FromBody] UpdateBlogPostRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateBlogPostCommand(
                id, request.Title, request.Excerpt, request.Content,
                request.Tags, request.MetaTitle, request.MetaDescription,
                request.OgImageUrl, request.ReadingTimeMinutes), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> PublishBlogPost(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new PublishBlogPostCommand(id), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> ArchiveBlogPost(
        [FromRoute] Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ArchiveBlogPostCommand(id), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok()
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetAllAdmin(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetAllBlogPostsAdminQuery(page, pageSize), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> UpsertSeoPage(
        [FromRoute] string slug,
        [FromBody] UpsertSeoPageCommand command,
        IMediator mediator,
        CancellationToken ct)
    {
        var cmd = command with { Slug = slug };
        var result = await mediator.Send(cmd, ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
