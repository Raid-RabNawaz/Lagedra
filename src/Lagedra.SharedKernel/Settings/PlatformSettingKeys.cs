namespace Lagedra.SharedKernel.Settings;

public static class PlatformSettingKeys
{
    // Protocol fee
    public const string ProtocolFeeMonthly = "protocol_fee.monthly_cents";
    public const string ProtocolFeePilotDiscount = "protocol_fee.pilot_discount_cents";
    public const string ProtocolFeePilotActive = "protocol_fee.pilot_active";

    // Arbitration fee
    public const string ArbitrationFeeProtocolAdjudication = "arbitration_fee.protocol_adjudication_cents";
    public const string ArbitrationFeeBindingArbitration = "arbitration_fee.binding_arbitration_cents";

    // Tenant payment timing
    public const string PaymentGracePeriodDays = "payment.grace_period_days";
    public const string PaymentReminderAfterDays = "payment.reminder_after_days";
    public const string PaymentAutoCancelAfterDays = "payment.auto_cancel_after_days";

    // Host platform payment enforcement
    public const string HostPlatformPaymentReminderIntervalDays = "host_platform_payment.reminder_interval_days";
    public const string HostPlatformPaymentSuspendAfterDays = "host_platform_payment.suspend_after_days";

    // Cancellation & insurance
    public const string CancellationInsuranceRefundDeadlineDays = "cancellation.insurance_refund_deadline_days";

    // Damage claims
    public const string DamageClaimFilingDeadlineDays = "damage_claim.filing_deadline_days";
}
