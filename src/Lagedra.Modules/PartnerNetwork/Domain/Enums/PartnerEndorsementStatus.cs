namespace Lagedra.Modules.PartnerNetwork.Domain.Enums;

/// <summary>
/// Lifecycle of a <see cref="Aggregates.PartnerEndorsement"/>.
///
/// Transitions:
/// <list type="bullet">
///   <item><see cref="Requested"/> → <see cref="Approved"/> | <see cref="Revoked"/></item>
///   <item><see cref="Approved"/>  → <see cref="Revoked"/> | <see cref="Expired"/></item>
///   <item><see cref="Revoked"/>   → terminal</item>
///   <item><see cref="Expired"/>   → terminal</item>
/// </list>
/// </summary>
public enum PartnerEndorsementStatus
{
    Requested,
    Approved,
    Revoked,
    Expired
}
