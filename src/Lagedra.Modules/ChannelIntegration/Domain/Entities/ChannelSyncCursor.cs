using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.ChannelIntegration.Domain.Entities;

/// <summary>
/// High-water mark for incremental pulls from a channel (e.g. booking updates),
/// scoped per connection + cursor kind so each pull only fetches what changed
/// since the last successful run.
/// </summary>
public sealed class ChannelSyncCursor : Entity<Guid>
{
    public Guid ConnectionId { get; private set; }

    /// <summary>Logical stream this cursor tracks, e.g. "booking-updates".</summary>
    public string CursorKind { get; private set; } = string.Empty;

    public DateTime LastChangedAtUtc { get; private set; }

    private ChannelSyncCursor() { }

    public static ChannelSyncCursor Create(
        Guid connectionId,
        string cursorKind,
        DateTime lastChangedAtUtc,
        IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cursorKind);
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new ChannelSyncCursor
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            CursorKind = cursorKind.Trim(),
            LastChangedAtUtc = lastChangedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Advance(DateTime lastChangedAtUtc, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (lastChangedAtUtc <= LastChangedAtUtc)
        {
            return;
        }

        LastChangedAtUtc = lastChangedAtUtc;
        UpdatedAt = clock.UtcNow;
    }
}
