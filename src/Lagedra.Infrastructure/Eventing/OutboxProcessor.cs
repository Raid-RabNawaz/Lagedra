using System.Text.Json;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Eventing;

public sealed partial class OutboxProcessor(
    IServiceProvider serviceProvider,
    ILogger<OutboxProcessor> logger)
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    /// <summary>
    /// Processes pending outbox messages for a single module context.
    /// Each module has its own outbox table in its own schema, so calling this
    /// once per registered IOutboxContext is safe — no cross-module row collisions.
    ///
    /// Messages are claimed one at a time inside their own transaction whose
    /// SELECT takes a <c>FOR UPDATE SKIP LOCKED</c> row lock, then dispatched and
    /// marked processed before the transaction commits. Several dispatchers poll
    /// the same outbox concurrently — the API host's background dispatcher, the
    /// worker's background dispatcher, and the worker's Quartz orchestrator all
    /// funnel through here against the shared database. Without the lock they each
    /// read the same unprocessed rows in the same window and re-handle them, which
    /// is exactly what fired duplicate notifications (e.g. two identical "New
    /// Booking Application" emails/bells for one request). The lock guarantees a
    /// pending row is claimed by exactly one processor and skipped by the rest —
    /// one event, one notification — no matter how many dispatchers or replicas run.
    ///
    /// Claiming per-message (rather than a whole batch under one transaction)
    /// keeps the crash window to a single in-flight row: a process that dies mid
    /// dispatch only re-runs that one message, matching the outbox's existing
    /// at-least-once contract.
    /// </summary>
    public async Task ProcessAsync(IOutboxContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not DbContext dbContext)
        {
            throw new InvalidOperationException(
                "IOutboxContext must be an EF Core DbContext to support transactional claiming.");
        }

        var processed = 0;
        while (processed < BatchSize && !ct.IsCancellationRequested)
        {
            var claimedOne = await ClaimAndProcessOneAsync(context, dbContext, ct).ConfigureAwait(false);
            if (!claimedOne)
            {
                break;
            }

            processed++;
        }

        if (processed > 0)
        {
            LogProcessingBatch(logger, processed);
        }
    }

    /// <summary>
    /// Opens a transaction, claims the oldest unprocessed row with a row-level
    /// lock, dispatches it, and commits. Returns <c>false</c> when there is
    /// nothing left to claim. Because the claim + mark-processed + commit are one
    /// atomic unit, a sibling processor that polls concurrently skips this locked
    /// row and moves on rather than re-dispatching it.
    /// </summary>
    private async Task<bool> ClaimAndProcessOneAsync(
        IOutboxContext context, DbContext dbContext, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(ct).ConfigureAwait(false);

        var message = await ClaimNextAsync(context, dbContext, ct).ConfigureAwait(false);
        if (message is null)
        {
            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return false;
        }

        // ProcessMessageAsync persists the row's processed/retry state, so the
        // commit below finalises the claim together with the dispatch outcome.
        await ProcessMessageAsync(context, message, ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Claims the next pending row with <c>FOR UPDATE SKIP LOCKED</c>. The
    /// statement is fully raw so EF doesn't wrap it in a subquery (which would
    /// strip the locking clause). Schema/table come from the model so every
    /// module's outbox table resolves correctly; the column identifiers are the
    /// entity's PascalCase names, quoted to match Postgres' case-sensitive storage.
    /// </summary>
    private static async Task<OutboxMessage?> ClaimNextAsync(
        IOutboxContext context, DbContext dbContext, CancellationToken ct)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("OutboxMessage is not mapped in this context.");

        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException("OutboxMessage has no mapped table name.");
        var schema = entityType.GetSchema();
        var qualifiedTable = schema is null
            ? QuoteIdentifier(table)
            : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";

        var sql =
            $"SELECT * FROM {qualifiedTable} "
            + "WHERE \"ProcessedAt\" IS NULL "
            + $"AND \"RetryCount\" < {MaxRetries} "
            + "ORDER BY \"OccurredAt\" "
            + "LIMIT 1 "
            + "FOR UPDATE SKIP LOCKED";

        var claimed = await context.OutboxMessages
            .FromSqlRaw(sql)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return claimed.Count > 0 ? claimed[0] : null;
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private async Task ProcessMessageAsync(IOutboxContext context, OutboxMessage message, CancellationToken ct)
    {
        try
        {
            var type = Type.GetType(message.Type);
            if (type is null)
            {
                LogUnknownMessageType(logger, message.Id, message.Type);
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = $"Unknown type: {message.Type}";
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }

            var domainEvent = JsonSerializer.Deserialize(message.Content, type) as IDomainEvent;
            if (domainEvent is null)
            {
                LogDeserializationFailed(logger, message.Id, message.Type);
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = "Deserialization returned null.";
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }

            await using var scope = serviceProvider.CreateAsyncScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

            // Must invoke Publish<TConcreteEvent> via reflection so the
            // generic parameter matches the actual event type (e.g.
            // TruthSurfaceConfirmedEvent) rather than IDomainEvent.
            // Otherwise GetServices<IDomainEventHandler<IDomainEvent>>()
            // resolves zero handlers and events are silently swallowed.
            var publishMethod = typeof(IEventBus)
                .GetMethod(nameof(IEventBus.Publish))!
                .MakeGenericMethod(type);
            await ((Task)publishMethod.Invoke(eventBus, [domainEvent, ct])!).ConfigureAwait(false);

            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;
            LogMessageProcessed(logger, message.Id, message.Type);
        }
#pragma warning disable CA1031 // intentional: outbox must survive any handler exception
        catch (Exception ex)
#pragma warning restore CA1031
        {
            message.RetryCount++;
            message.Error = ex.Message;

            if (message.RetryCount >= MaxRetries)
            {
                message.ProcessedAt = DateTime.UtcNow;
                LogMessagePoisoned(logger, message.Id, message.Type, ex);
            }
            else
            {
                LogMessageFailed(logger, message.Id, message.Type, message.RetryCount, ex);
            }
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatched {Count} outbox message(s) this cycle")]
    private static partial void LogProcessingBatch(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {Id} has unknown type '{Type}' — skipping")]
    private static partial void LogUnknownMessageType(ILogger logger, Guid id, string type);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {Id} of type '{Type}' could not be deserialized — skipping")]
    private static partial void LogDeserializationFailed(ILogger logger, Guid id, string type);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox message {Id} ({Type}) processed successfully")]
    private static partial void LogMessageProcessed(ILogger logger, Guid id, string type);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {Id} ({Type}) failed after {RetryCount} attempts — will retry")]
    private static partial void LogMessageFailed(ILogger logger, Guid id, string type, int retryCount, Exception ex);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Outbox message {Id} ({Type}) exceeded max retries — marked as dead")]
    private static partial void LogMessagePoisoned(ILogger logger, Guid id, string type, Exception ex);
}
