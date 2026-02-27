using Lagedra.Modules.Evidence.Domain.Enums;

namespace Lagedra.Modules.Evidence.Presentation.Contracts;

public sealed record SubmitManifestRequest(
    Guid DealId,
    ManifestType ManifestType);
