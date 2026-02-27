using Lagedra.Modules.ContentManagement.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ContentManagement.Presentation.Endpoints;

public static class SeoPageEndpoints
{
    public static IEndpointRouteBuilder MapSeoPageEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/pages")
            .WithTags("SeoPages");

        group.MapGet("/{slug}", GetBySlug);

        return app;
    }

    private static async Task<IResult> GetBySlug(
        [FromRoute] string slug,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetSeoPageBySlugQuery(slug), ct).ConfigureAwait(true);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error.Code, detail = result.Error.Description });
    }
}
