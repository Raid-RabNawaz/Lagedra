using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Entities;

public sealed class DeletionRequest : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public DeletionStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? BlockingReason { get; private set; }

    private DeletionRequest() { }

    public static DeletionRequest Create(Guid userId)
    {
        return new DeletionRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = DeletionStatus.Requested,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = DeletionStatus.Blocked;
        BlockingReason = reason;
    }

    public void Complete()
    {
        Status = DeletionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}
