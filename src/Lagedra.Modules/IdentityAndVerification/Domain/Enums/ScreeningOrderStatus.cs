namespace Lagedra.Modules.IdentityAndVerification.Domain.Enums;

/// <summary>
/// Persistence twin of <see cref="Lagedra.SharedKernel.Integration.Screening.ScreeningOrderStatus"/>.
/// Kept in the module so EF conversions stay local.
/// </summary>
public enum ScreeningOrderStatus
{
    PendingPayment,
    Paid,
    WaitingForApplicant,
    Pending,
    Completed,
    Canceled,
    Failed
}
