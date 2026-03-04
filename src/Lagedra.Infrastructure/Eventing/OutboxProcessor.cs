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

    /// <summary>
    /// Processes pending outbox messages for a single module context.
    /// Each module has its own outbox table in its own schema, so calling this
    /// once per registered IOutboxContext is safe — no cross-module row collisions.
    /// </summary>
    public async Task ProcessAsync(IOutboxContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        var messages = await context.OutboxMessages
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
            await ProcessMessageAsync(context, message, ct).ConfigureAwait(false);
        }
    }

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

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
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
