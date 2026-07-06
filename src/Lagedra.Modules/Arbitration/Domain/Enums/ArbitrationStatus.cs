namespace Lagedra.Modules.Arbitration.Domain.Enums;

public enum ArbitrationStatus
{
    /// <summary>
    /// The case has been opened but the filing fee has not been paid yet. The
    /// case is inert in this state — no evidence, assignment, or review may
    /// happen until the filer pays and the case transitions to
    /// <see cref="Filed"/>. Cases with a zero filing fee skip this state.
    /// </summary>
    PendingPayment,
    Filed,
    EvidencePending,
    EvidenceComplete,
    UnderReview,
    Decided,
    Appealed,
    Closed
}
