namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Returns the active jurisdiction pack version for a given jurisdiction code.
/// Implemented by the JurisdictionPacks module, consumed by TruthSurface so the
/// sealed snapshot records the exact pack version that governed the deal terms.
/// </summary>
public interface IJurisdictionPackProvider
{
    Task<JurisdictionPackInfo?> GetActivePackAsync(string jurisdictionCode, CancellationToken ct = default);
}

public sealed record JurisdictionPackInfo(
    Guid PackId,
    string JurisdictionCode,
    Guid ActiveVersionId,
    int VersionNumber,
    DateTime? EffectiveDate);
