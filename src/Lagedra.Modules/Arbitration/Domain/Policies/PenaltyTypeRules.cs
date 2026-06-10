using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Domain.Policies;

public static class PenaltyTypeRules
{
    private static readonly HashSet<PenaltyType> AmountRequiredTypes =
    [
        PenaltyType.Monetary,
        PenaltyType.DepositWithhold,
        PenaltyType.ProtocolFee,
        PenaltyType.RentCredit,
        PenaltyType.LateFee,
        PenaltyType.DamageRestitution,
        PenaltyType.InsuranceRecovery,
        PenaltyType.CleaningFee,
        PenaltyType.UtilitiesRecovery
    ];

    public static bool RequiresAmount(PenaltyType type) => AmountRequiredTypes.Contains(type);

    public static string GetLabel(PenaltyType type) => type switch
    {
        PenaltyType.Monetary => "Monetary payment",
        PenaltyType.DepositWithhold => "Deposit withhold",
        PenaltyType.TrustLedgerMark => "Trust ledger mark",
        PenaltyType.AccountWarning => "Account warning",
        PenaltyType.ProtocolFee => "Protocol fee",
        PenaltyType.RentCredit => "Rent credit (owed to party)",
        PenaltyType.LateFee => "Late fee",
        PenaltyType.DamageRestitution => "Damage restitution",
        PenaltyType.InsuranceRecovery => "Insurance recovery",
        PenaltyType.AccountRestriction => "Account restriction",
        PenaltyType.PlatformBan => "Platform ban",
        PenaltyType.CorrectiveAction => "Mandatory corrective action",
        PenaltyType.LeaseTermination => "Lease termination notice",
        PenaltyType.CleaningFee => "Cleaning fee",
        PenaltyType.UtilitiesRecovery => "Utilities recovery",
        PenaltyType.Custom => "Custom",
        _ => type.ToString()
    };
}
