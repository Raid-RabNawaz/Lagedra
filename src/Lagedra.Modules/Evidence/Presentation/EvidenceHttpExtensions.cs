using System.Security.Claims;
using Lagedra.Modules.Evidence.Application.Services;
using Microsoft.AspNetCore.Http;

namespace Lagedra.Modules.Evidence.Presentation;

internal static class EvidenceHttpExtensions
{
    internal static EvidenceCallerContext GetCallerContext(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found.");

        return new EvidenceCallerContext(
            Guid.Parse(claim.Value),
            httpContext.User.IsInRole("PlatformAdmin"),
            httpContext.User.IsInRole("Arbitrator"));
    }
}
