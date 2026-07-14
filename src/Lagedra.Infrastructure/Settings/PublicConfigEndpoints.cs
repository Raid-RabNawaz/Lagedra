using Lagedra.SharedKernel.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Infrastructure.Settings;

/// <summary>
/// Anonymous, read-only surface for the handful of platform flags the SPA
/// needs before a user has authenticated (e.g. whether to render the
/// pre-launch waitlist flow instead of the full product). Kept deliberately
/// tiny and PII-free so it can be cached at the edge.
/// </summary>
public static class PublicConfigEndpoints
{
    public static IEndpointRouteBuilder MapPublicConfigEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/v1/platform/public-config", GetPublicConfig)
            .AllowAnonymous()
            .WithTags("Platform");

        return app;
    }

    private static async Task<IResult> GetPublicConfig(
        IPlatformSettingsService settings,
        CancellationToken ct)
    {
        var preLaunchEnabled = await settings
            .GetBoolAsync(PlatformSettingKeys.PreLaunchEnabled, defaultValue: false, ct)
            .ConfigureAwait(false);

        return Results.Ok(new PublicConfigDto(preLaunchEnabled));
    }
}

public sealed record PublicConfigDto(bool PreLaunchEnabled);
