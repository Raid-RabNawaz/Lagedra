namespace Lagedra.Modules.Evidence.Application.Services;

public sealed record EvidenceCallerContext(
    Guid UserId,
    bool IsPlatformAdmin,
    bool IsArbitrator);
