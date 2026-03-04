using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Entities;

public sealed class LegalHold : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime AppliedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    private LegalHold() { }

    public static LegalHold Apply(Guid userId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new LegalHold
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Reason = reason,
            AppliedAt = DateTime.UtcNow
        };
    }

    public void Release()
    {
        if (ReleasedAt is not null)
        {
            throw new InvalidOperationException("Legal hold has already been released.");
        }

        ReleasedAt = DateTime.UtcNow;
    }
}
