using Lagedra.Modules.ContentManagement.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ContentManagement.Presentation.Endpoints;

public static class BlogEndpoints
{
    public static IEndpointRouteBuilder MapBlogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/blog")
            .WithTags("Blog");

        group.MapGet("/", GetPublishedPosts);
        group.MapGet("/{slug}", GetBySlug);
        group.MapGet("/sitemap", GetSitemap);

        return app;
    }

    private static async Task<IResult> GetPublishedPosts(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? tag,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetPublishedBlogPostsQuery(page, pageSize, tag), ct)
            .ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetBySlug(
        [FromRoute] string slug,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetBlogPostBySlugQuery(slug), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }

    private static async Task<IResult> GetSitemap(
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetSitemapEntriesQuery(), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
