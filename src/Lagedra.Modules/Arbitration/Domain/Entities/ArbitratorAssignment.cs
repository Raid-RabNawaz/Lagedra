using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Entities;

public sealed class ArbitratorAssignment : Entity<Guid>
{
    public Guid CaseId { get; private set; }
    public Guid ArbitratorUserId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public int ConcurrentCaseCount { get; private set; }

    private ArbitratorAssignment() { }

    internal ArbitratorAssignment(Guid caseId, Guid arbitratorUserId, DateTime assignedAt, int concurrentCaseCount)
        : base(Guid.NewGuid())
    {
        CaseId = caseId;
        ArbitratorUserId = arbitratorUserId;
        AssignedAt = assignedAt;
        ConcurrentCaseCount = concurrentCaseCount;
    }
}
