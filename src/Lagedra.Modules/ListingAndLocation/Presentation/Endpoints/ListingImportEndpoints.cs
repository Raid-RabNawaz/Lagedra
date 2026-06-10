using System.Security.Claims;
using Lagedra.Infrastructure.Middleware;
using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Presentation.Contracts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Endpoints;

/// <summary>
/// Endpoints for the opt-in "import from URL" pre-fill step of the create
/// listing flow. Kept in a dedicated file so the existing listing endpoints are
/// not touched. The route is purely a transform (URL -> draft DTO) and persists
/// nothing.
/// </summary>
public static class ListingImportEndpoints
{
    public static IEndpointRouteBuilder MapListingImportEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/listings")
            .WithTags("Listings");

        group.MapPost("/import-from-url", ImportFromUrl)
            .RequireAuthorization("RequireMember")
            .RequireRateLimiting(RateLimitingSetup.ListingImportPolicy);

        return app;
    }

    private static async Task<IResult> ImportFromUrl(
        [FromBody] ImportListingFromUrlRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = GetUserId(httpContext);
        var result = await mediator.Send(
            new ImportListingFromUrlCommand(userId, request.Url, request.HostAttestation),
            cancellationToken).ConfigureAwait(true);

        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        var payload = new { error = result.Error.Code, detail = result.Error.Description };
        return result.Error.Code switch
        {
            "Import.AttestationRequired" => Results.BadRequest(payload),
            "Import.InvalidUrl" => Results.BadRequest(payload),
            "Import.RobotsBlocked" => Results.BadRequest(payload),
            _ => Results.BadRequest(payload),
        };
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");
        return Guid.Parse(claim.Value);
    }
}
