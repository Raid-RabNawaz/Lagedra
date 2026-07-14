namespace Lagedra.Compliance.Domain;

public enum TrustLedgerEntryType
{
    DealCompleted,
    ViolationRecorded,
    ViolationDismissed,
    ArbitrationRuling,
    InsuranceClaim,
    PaymentDefault,
    EarlyTermination,
    PositiveReview,
    /// <summary>Soft reputation signal from a published low stay rating (≤2). Not a violation.</summary>
    ReviewConcern,
    IdentityVerified
}
