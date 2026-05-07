using System.Security.Claims;
using Lagedra.SharedKernel.Integration;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Presentation.Authorization;

/// <summary>
/// Centralised participant authorization for Truth Surface endpoints.
///
/// A snapshot may only be read or confirmed by:
///   - the deal's landlord
///   - the deal's tenant
///   - users in role 'PlatformAdmin' or 'Arbitrator' (for arbitration / support)
///
/// All other authenticated callers receive 403 Forbidden, even though the
/// outer policy only requires <c>RequireAuthorization()</c>.
/// </summary>
internal static class TruthSurfaceAccess
{
    private static readonly string[] AdminRoles = ["PlatformAdmin", "Arbitrator"];

    public static async Task<TruthSurfaceAccessResult> AuthorizeBySnapshotAsync(
        Guid snapshotId,
        ClaimsPrincipal user,
        TruthSurfaceDbContext dbContext,
        IDealApplicationStatusProvider dealStatusProvider,
        CancellationToken ct)
    {
        var dealId = await dbContext.Snapshots
            .AsNoTracking()
            .Where(s => s.Id == snapshotId)
            .Select(s => (Guid?)s.DealId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (dealId is null)
        {
            return TruthSurfaceAccessResult.NotFound();
        }

        return await AuthorizeByDealAsync(dealId.Value, user, dealStatusProvider, ct)
            .ConfigureAwait(false);
    }

    public static async Task<TruthSurfaceAccessResult> AuthorizeByDealAsync(
        Guid dealId,
        ClaimsPrincipal user,
        IDealApplicationStatusProvider dealStatusProvider,
        CancellationToken ct)
    {
        var participants = await dealStatusProvider
            .GetParticipantsAsync(dealId, ct)
            .ConfigureAwait(false);

        if (participants is null)
        {
            return TruthSurfaceAccessResult.NotFound();
        }

        if (HasAdminRole(user))
        {
            return TruthSurfaceAccessResult.Allowed(participants, isAdmin: true);
        }

        var userId = TryGetUserId(user);
        if (userId is null)
        {
            return TruthSurfaceAccessResult.Forbidden();
        }

        var isLandlord = participants.LandlordUserId == userId.Value;
        var isTenant = participants.TenantUserId == userId.Value;

        if (!isLandlord && !isTenant)
        {
            return TruthSurfaceAccessResult.Forbidden();
        }

        return TruthSurfaceAccessResult.Allowed(participants, isAdmin: false, isLandlord: isLandlord, isTenant: isTenant);
    }

    public static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static bool HasAdminRole(ClaimsPrincipal user) =>
        AdminRoles.Any(r => user.IsInRole(r));
}

internal sealed record TruthSurfaceAccessResult(
    TruthSurfaceAccessOutcome Outcome,
    DealParticipantsDto? Participants = null,
    bool IsAdmin = false,
    bool IsLandlord = false,
    bool IsTenant = false)
{
    public static TruthSurfaceAccessResult NotFound() =>
        new(TruthSurfaceAccessOutcome.NotFound);

    public static TruthSurfaceAccessResult Forbidden() =>
        new(TruthSurfaceAccessOutcome.Forbidden);

    public static TruthSurfaceAccessResult Allowed(
        DealParticipantsDto participants,
        bool isAdmin,
        bool isLandlord = false,
        bool isTenant = false) =>
        new(TruthSurfaceAccessOutcome.Allowed, participants, isAdmin, isLandlord, isTenant);
}

internal enum TruthSurfaceAccessOutcome
{
    Allowed,
    Forbidden,
    NotFound
}
