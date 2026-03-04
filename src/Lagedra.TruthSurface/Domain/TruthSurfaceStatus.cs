namespace Lagedra.TruthSurface.Domain;

public enum TruthSurfaceStatus
{
    Draft,
    PendingBothConfirmations,
    PendingLandlordConfirmation,
    PendingTenantConfirmation,
    Confirmed,
    Superseded
}
