using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;

public sealed class LeaseTerms : ValueObject
{
    public int RentDueDayOfMonth { get; private set; } = 1;
    public long NsfFirstFeeCents { get; private set; } = 2500;
    public long NsfSubsequentFeeCents { get; private set; } = 3500;
    public decimal LateFeePercent { get; private set; } = 5m;
    public int LateFeeGraceDays { get; private set; } = 3;
    public string? UtilitiesResponsibility { get; private set; }
    public bool YardMaintenanceByTenant { get; private set; }
    public bool Furnished { get; private set; }
    public string? IncludedAppliancesNotes { get; private set; }
    public int KeyCount { get; private set; } = 1;
    public int MailboxKeyCount { get; private set; }
    public long KeyReplacementFeeCents { get; private set; } = 20000;
    public long LockoutFeeCents { get; private set; } = 20000;
    public int ParkingSpaceCount { get; private set; }
    public string? ParkingDescription { get; private set; }
    public bool ParkingIncludedInRent { get; private set; } = true;
    public int MaxGuestConsecutiveDays { get; private set; } = 7;
    public long RentersInsuranceMinLiabilityCents { get; private set; } = 100_000_00;
    public int EarlyTerminationFeeMonths { get; private set; } = 2;
    public bool BuiltBefore1978 { get; private set; }
    public string? LeadPaintKnowledge { get; private set; }
    public bool RentCapJustCauseExempt { get; private set; }
    public string? PaymentMethods { get; private set; }

    private LeaseTerms() { }

    public static LeaseTerms CreateDefault() => new();

    public static LeaseTerms Create(
        int rentDueDayOfMonth = 1,
        long nsfFirstFeeCents = 2500,
        long nsfSubsequentFeeCents = 3500,
        decimal lateFeePercent = 5m,
        int lateFeeGraceDays = 3,
        string? utilitiesResponsibility = null,
        bool yardMaintenanceByTenant = false,
        bool furnished = false,
        string? includedAppliancesNotes = null,
        int keyCount = 1,
        int mailboxKeyCount = 0,
        long keyReplacementFeeCents = 20000,
        long lockoutFeeCents = 20000,
        int parkingSpaceCount = 0,
        string? parkingDescription = null,
        bool parkingIncludedInRent = true,
        int maxGuestConsecutiveDays = 7,
        long rentersInsuranceMinLiabilityCents = 100_000_00,
        int earlyTerminationFeeMonths = 2,
        bool builtBefore1978 = false,
        string? leadPaintKnowledge = null,
        bool rentCapJustCauseExempt = false,
        string? paymentMethods = null)
    {
        if (rentDueDayOfMonth is < 1 or > 28)
        {
            throw new ArgumentOutOfRangeException(nameof(rentDueDayOfMonth), "Rent due day must be 1–28.");
        }

        return new LeaseTerms
        {
            RentDueDayOfMonth = rentDueDayOfMonth,
            NsfFirstFeeCents = nsfFirstFeeCents,
            NsfSubsequentFeeCents = nsfSubsequentFeeCents,
            LateFeePercent = lateFeePercent,
            LateFeeGraceDays = lateFeeGraceDays,
            UtilitiesResponsibility = Truncate(utilitiesResponsibility, 500),
            YardMaintenanceByTenant = yardMaintenanceByTenant,
            Furnished = furnished,
            IncludedAppliancesNotes = Truncate(includedAppliancesNotes, 500),
            KeyCount = Math.Max(0, keyCount),
            MailboxKeyCount = Math.Max(0, mailboxKeyCount),
            KeyReplacementFeeCents = keyReplacementFeeCents,
            LockoutFeeCents = lockoutFeeCents,
            ParkingSpaceCount = Math.Max(0, parkingSpaceCount),
            ParkingDescription = Truncate(parkingDescription, 300),
            ParkingIncludedInRent = parkingIncludedInRent,
            MaxGuestConsecutiveDays = Math.Max(0, maxGuestConsecutiveDays),
            RentersInsuranceMinLiabilityCents = rentersInsuranceMinLiabilityCents,
            EarlyTerminationFeeMonths = Math.Max(0, earlyTerminationFeeMonths),
            BuiltBefore1978 = builtBefore1978,
            LeadPaintKnowledge = Truncate(leadPaintKnowledge, 1000),
            RentCapJustCauseExempt = rentCapJustCauseExempt,
            PaymentMethods = Truncate(paymentMethods, 500)
        };
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length > max ? value[..max] : value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RentDueDayOfMonth;
        yield return NsfFirstFeeCents;
        yield return NsfSubsequentFeeCents;
        yield return LateFeePercent;
        yield return LateFeeGraceDays;
        yield return UtilitiesResponsibility;
        yield return YardMaintenanceByTenant;
        yield return Furnished;
        yield return IncludedAppliancesNotes;
        yield return KeyCount;
        yield return MailboxKeyCount;
        yield return KeyReplacementFeeCents;
        yield return LockoutFeeCents;
        yield return ParkingSpaceCount;
        yield return ParkingDescription;
        yield return ParkingIncludedInRent;
        yield return MaxGuestConsecutiveDays;
        yield return RentersInsuranceMinLiabilityCents;
        yield return EarlyTerminationFeeMonths;
        yield return BuiltBefore1978;
        yield return LeadPaintKnowledge;
        yield return RentCapJustCauseExempt;
        yield return PaymentMethods;
    }
}
