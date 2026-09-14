namespace Lagedra.SharedKernel.Settings;

/// <summary>
/// Vendor-neutral screening settings. Vendor secrets live under their own section
/// (e.g. <c>Checkr</c>).
/// </summary>
public sealed class ScreeningOptions
{
    public const string SectionName = "Screening";

    /// <summary><c>Checkr</c> or <c>NoOp</c>.</summary>
    public string Provider { get; set; } = "NoOp";

    /// <summary>Amount charged to the host per screening order, in cents.</summary>
    public long HostFeeCents { get; set; } = 4500;

    /// <summary>
    /// After this many days in <c>WaitingForApplicant</c>, refund the host
    /// and mark the order canceled.
    /// </summary>
    public int IncompleteApplyRefundAfterDays { get; set; } = 7;
}
