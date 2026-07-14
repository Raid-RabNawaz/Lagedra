using Lagedra.Infrastructure.Caching;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public sealed class LeaseAgreementTemplateProvider(
    LeaseAgreementDbContext db,
    IMemoryCache cache) : ILeaseAgreementTemplateProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public async Task<LeaseAgreementTemplateInfo?> GetActiveTemplateAsync(
        string jurisdictionCode,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionCode);
        var code = jurisdictionCode.ToUpperInvariant();
        var cacheKey = CacheKeys.LeaseAgreementTemplate(code);

        if (cache.TryGetValue(cacheKey, out LeaseAgreementTemplateInfo? cached))
        {
            return cached;
        }

        var template = await db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.JurisdictionCode.Code == code, ct)
            .ConfigureAwait(false);

        if (template?.ActiveVersionId is null)
        {
            return null;
        }

        var version = template.Versions.FirstOrDefault(v => v.Id == template.ActiveVersionId);
        if (version is null)
        {
            return null;
        }

        var info = new LeaseAgreementTemplateInfo(
            template.Id,
            template.JurisdictionCode.Code,
            template.Title,
            version.Id,
            version.VersionNumber,
            version.EffectiveDate,
            version.BodyHtml);

        cache.Set(cacheKey, info, CacheTtl);
        return info;
    }
}
