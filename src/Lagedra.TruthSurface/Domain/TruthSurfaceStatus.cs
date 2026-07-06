namespace Lagedra.TruthSurface.Domain;

public enum TruthSurfaceStatus
{
    Draft,
    PendingBothConfirmations,
    PendingLandlordConfirmation,
    PendingTenantConfirmation,
    Confirmed,
    Superseded,

    /// <summary>
    /// Terminal cancel — the sealed agreement is no longer in force (e.g. the
    /// booking was cancelled before activation). Used only for terminal cancel;
    /// a recoverable payment failure keeps the snapshot <see cref="Confirmed"/>
    /// (Locked) so a retry charges against the same signed record.
    /// </summary>
    Voided
}
