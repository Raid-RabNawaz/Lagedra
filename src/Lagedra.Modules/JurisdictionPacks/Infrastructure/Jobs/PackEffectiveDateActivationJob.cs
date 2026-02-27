using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class PackEffectiveDateActivationJob(
    JurisdictionDbContext dbContext,
    ILogger<PackEffectiveDateActivationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var today = DateTime.UtcNow.Date;

        var pendingVersions = await dbContext.PackVersions
            .Include(v => v.EffectiveDateRules)
            .Where(v => v.Status == PackVersionStatus.Active
                        && v.EffectiveDate.HasValue
                        && v.EffectiveDate.Value.Date <= today)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var activatedCount = 0;

        foreach (var version in pendingVersions)
        {
            var newlyEffective = version.EffectiveDateRules
                .Where(r => r.EffectiveDate.Date <= today)
                .ToList();

            if (newlyEffective.Count > 0)
            {
                activatedCount++;
                LogEffectiveDateRulesActivated(logger, version.Id, version.PackId, newlyEffective.Count);
            }
        }

        LogJobComplete(logger, pendingVersions.Count, activatedCount);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Effective date rules activated for version {VersionId} (Pack {PackId}): {RuleCount} rules now effective")]
    private static partial void LogEffectiveDateRulesActivated(ILogger logger, Guid versionId, Guid packId, int ruleCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pack effective date activation job complete: {VersionsChecked} versions checked, {ActivatedCount} had newly effective rules")]
    private static partial void LogJobComplete(ILogger logger, int versionsChecked, int activatedCount);
}
