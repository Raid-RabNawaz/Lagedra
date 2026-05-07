using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Services;

public sealed class JurisdictionPackProvider(JurisdictionDbContext db) : IJurisdictionPackProvider
{
    public async Task<JurisdictionPackInfo?> GetActivePackAsync(string jurisdictionCode, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionCode);

        var code = jurisdictionCode.Trim().ToUpperInvariant();

        var pack = await db.JurisdictionPacks
            .AsNoTracking()
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.JurisdictionCode.Code == code, ct)
            .ConfigureAwait(false);

        if (pack?.ActiveVersionId is null)
        {
            return null;
        }

        var active = pack.Versions.FirstOrDefault(v => v.Id == pack.ActiveVersionId.Value);
        if (active is null)
        {
            return null;
        }

        return new JurisdictionPackInfo(
            pack.Id,
            pack.JurisdictionCode.Code,
            active.Id,
            active.VersionNumber,
            active.EffectiveDate);
    }
}
