namespace Lagedra.Modules.Arbitration.Presentation.Contracts;

public sealed record AttachEvidenceRequest(
    string SlotType,
    Guid SubmittedBy,
    string FileReference);
