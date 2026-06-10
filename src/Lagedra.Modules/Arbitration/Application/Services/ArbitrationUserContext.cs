namespace Lagedra.Modules.Arbitration.Application.Services;

public sealed record ArbitrationUserContext(
    Guid UserId,
    bool IsPlatformAdmin,
    bool IsArbitrator);
