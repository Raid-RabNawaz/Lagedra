using System.Security.Claims;
using Lagedra.SharedKernel.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Lagedra.Infrastructure.Settings;

public static class PlatformSettingsEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/admin/settings")
            .WithTags("Platform Settings")
            .RequireAuthorization("RequirePlatformAdmin");

        group.MapGet("/", GetAll);
        group.MapPut("/{key}", UpdateSetting);

        return app;
    }

    private static async Task<IResult> GetAll(
        IPlatformSettingsService settings,
        CancellationToken ct)
    {
        var all = await settings.GetAllAsync(ct).ConfigureAwait(true);

        var dtos = all.Select(s => new PlatformSettingDto(
            s.Key, s.Value, s.Description, s.UpdatedAt, s.UpdatedByUserId)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> UpdateSetting(
        [FromRoute] string key,
        [FromBody] UpdateSettingRequest request,
        ClaimsPrincipal principal,
        IPlatformSettingsService settings,
        CancellationToken ct)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Guid? adminId = userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var id) ? id : null;

        await settings.SetAsync(key, request.Value, request.Description, adminId, ct).ConfigureAwait(true);

        return Results.Ok(new { message = $"Setting '{key}' updated." });
    }
}

public sealed record PlatformSettingDto(
    string Key,
    string Value,
    string? Description,
    DateTime UpdatedAt,
    Guid? UpdatedByUserId);

public sealed record UpdateSettingRequest(
    string Value,
    string? Description);
