namespace Lagedra.Infrastructure.Eventing;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalSeconds { get; init; } = 10;
    public int MaxRetries { get; init; } = 5;
}
