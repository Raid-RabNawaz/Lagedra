using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Entities;

public sealed class EvidenceSlot : Entity<Guid>
{
    public Guid CaseId { get; private set; }
    public string SlotType { get; private set; } = string.Empty;
    public Guid SubmittedBy { get; private set; }
    public string FileReference { get; private set; } = string.Empty;
    public DateTime SubmittedAt { get; private set; }

    private EvidenceSlot() { }

    internal EvidenceSlot(Guid caseId, string slotType, Guid submittedBy, string fileReference, DateTime submittedAt)
        : base(Guid.NewGuid())
    {
        CaseId = caseId;
        SlotType = slotType;
        SubmittedBy = submittedBy;
        FileReference = fileReference;
        SubmittedAt = submittedAt;
    }
}
