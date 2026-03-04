namespace Lagedra.Modules.ActivationAndBilling.Domain.Policies;

/// <summary>
/// Configurable protocol fee charged monthly per active deal to the host.
/// Bound from appsettings.json section "ProtocolFee".
/// </summary>
public sealed class ProtocolFeeSettings
{
    public const string SectionName = "ProtocolFee";

    public long MonthlyFeeCentsPerActiveDeal { get; set; } = 7900;

    public long PilotDiscountCents { get; set; } = 3900;

    public bool IsPilotActive { get; set; }

    public long EffectiveMonthlyFeeCents =>
        IsPilotActive
            ? MonthlyFeeCentsPerActiveDeal - PilotDiscountCents
            : MonthlyFeeCentsPerActiveDeal;
}
