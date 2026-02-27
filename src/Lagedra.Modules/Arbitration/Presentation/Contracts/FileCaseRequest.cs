using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Presentation.Contracts;

public sealed record FileCaseRequest(
    Guid DealId,
    ArbitrationTier Tier,
    ArbitrationCategory Category);
