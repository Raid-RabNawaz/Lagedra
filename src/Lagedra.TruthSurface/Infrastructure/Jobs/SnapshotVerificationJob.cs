using Lagedra.SharedKernel.Security;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Crypto;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.TruthSurface.Infrastructure.Jobs;

/// <summary>
/// Weekly job that re-computes SHA-256 hashes for all confirmed snapshots
/// and flags any mismatches. This is a tamper-detection safety net.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class SnapshotVerificationJob(
    TruthSurfaceDbContext dbContext,
    ICryptographicSigner signer,
    ILogger<SnapshotVerificationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshots = await dbContext.Snapshots
            .AsNoTracking()
            .Include(s => s.Proof)
            .Where(s => s.Status == TruthSurfaceStatus.Confirmed && s.Proof != null)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var mismatches = 0;

        foreach (var snapshot in snapshots)
        {
            if (snapshot.CanonicalContent is null || snapshot.Proof is null)
            {
                continue;
            }

            var recomputedHash = CanonicalHasher.ComputeHash(snapshot.CanonicalContent);
            var hashValid = string.Equals(recomputedHash, snapshot.Proof.Hash, StringComparison.Ordinal);
            var sigValid = signer.Verify(
                System.Text.Encoding.UTF8.GetBytes(snapshot.Proof.Hash),
                snapshot.Proof.Signature);

            if (!hashValid || !sigValid)
            {
                mismatches++;
                LogTamperDetected(logger, snapshot.Id, snapshot.DealId, hashValid, sigValid);
            }
        }

        LogVerificationComplete(logger, snapshots.Count, mismatches);
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "TAMPER DETECTED: Snapshot {SnapshotId} (Deal {DealId}) — hash valid: {HashValid}, signature valid: {SigValid}")]
    private static partial void LogTamperDetected(ILogger logger, Guid snapshotId, Guid dealId, bool hashValid, bool sigValid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Snapshot verification complete: {Total} checked, {Mismatches} mismatches")]
    private static partial void LogVerificationComplete(ILogger logger, int total, int mismatches);
}
