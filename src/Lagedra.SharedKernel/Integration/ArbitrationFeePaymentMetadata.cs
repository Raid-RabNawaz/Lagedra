namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Stripe PaymentIntent metadata contract for arbitration filing-fee charges.
/// Shared so the Arbitration module (which creates the PaymentIntent) and the
/// ActivationAndBilling webhook (which reacts to it succeeding) agree on the keys.
/// </summary>
public static class ArbitrationFeePaymentMetadata
{
    public const string PurposeKey = "purpose";
    public const string PurposeValue = "arbitration_filing_fee";
    public const string CaseIdKey = "arbitrationCaseId";
    public const string DealIdKey = "dealId";
    public const string FiledByUserIdKey = "filedByUserId";
}
