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

    public async Task ProcessAsync(DbContext dbContext, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        LogProcessingBatch(logger, messages.Count);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(dbContext, message, ct).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task ProcessMessageAsync(DbContext dbContext, OutboxMessage message, CancellationToken ct)
    {
        try
        {
            var type = Type.GetType(message.Type);
            if (type is null)
            {
                LogUnknownMessageType(logger, message.Id, message.Type);
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = $"Unknown type: {message.Type}";
                return;
            }

            var domainEvent = JsonSerializer.Deserialize(message.Content, type) as IDomainEvent;
            if (domainEvent is null)
            {
                LogDeserializationFailed(logger, message.Id, message.Type);
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = "Deserialization returned null.";
                return;
            }

            await using var scope = serviceProvider.CreateAsyncScope();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            await eventBus.Publish(domainEvent, ct).ConfigureAwait(false);

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

            if (message.RetryCount >= 5)
            {
                message.ProcessedAt = DateTime.UtcNow;
                LogMessagePoisoned(logger, message.Id, message.Type, ex);
            }
            else
            {
                LogMessageFailed(logger, message.Id, message.Type, message.RetryCount, ex);
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing outbox batch of {Count} messages")]
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
