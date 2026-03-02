using Lagedra.Compliance.Domain;

namespace Lagedra.Compliance.Presentation.Contracts;

public sealed record RecordViolationRequest(
    Guid DealId,
    Guid TargetUserId,
    ViolationCategory Category,
    string Description,
    string? EvidenceReference);
