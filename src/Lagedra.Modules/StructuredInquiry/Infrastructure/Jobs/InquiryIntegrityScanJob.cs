using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class InquiryIntegrityScanJob(
    InquiryDbContext dbContext,
    ILogger<InquiryIntegrityScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var openSessions = await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Where(s => s.Status == InquirySessionStatus.Open)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var anomalies = 0;

        foreach (var session in openSessions)
        {
            var unanswered = session.Questions.Count(q => q.Answer is null);
            var staleThreshold = DateTime.UtcNow.AddDays(-30);

            if (session.UnlockedByLandlordAt.HasValue && session.UnlockedByLandlordAt.Value < staleThreshold)
            {
                anomalies++;
                LogStaleSession(logger, session.Id, session.DealId, unanswered);
            }
        }

        LogScanComplete(logger, openSessions.Count, anomalies);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stale open inquiry session {SessionId} (Deal {DealId}) — {UnansweredCount} unanswered questions, open > 30 days")]
    private static partial void LogStaleSession(ILogger logger, Guid sessionId, Guid dealId, int unansweredCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Inquiry integrity scan complete: {Total} open sessions checked, {Anomalies} anomalies")]
    private static partial void LogScanComplete(ILogger logger, int total, int anomalies);
}
