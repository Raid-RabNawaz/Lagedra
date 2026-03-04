using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;

public sealed class HouseRules : ValueObject
{
    public TimeOnly CheckInTime { get; private set; }
    public TimeOnly CheckOutTime { get; private set; }
    public int MaxGuests { get; private set; }
    public bool PetsAllowed { get; private set; }
    public string? PetsNotes { get; private set; }
    public bool SmokingAllowed { get; private set; }
    public bool PartiesAllowed { get; private set; }
    public TimeOnly? QuietHoursStart { get; private set; }
    public TimeOnly? QuietHoursEnd { get; private set; }
    public string? LeavingInstructions { get; private set; }
    public string? AdditionalRules { get; private set; }

    private HouseRules() { }

    public static HouseRules Create(
        TimeOnly checkInTime,
        TimeOnly checkOutTime,
        int maxGuests,
        bool petsAllowed,
        string? petsNotes,
        bool smokingAllowed,
        bool partiesAllowed,
        TimeOnly? quietHoursStart,
        TimeOnly? quietHoursEnd,
        string? leavingInstructions,
        string? additionalRules)
    {
        if (maxGuests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxGuests), "Max guests must be positive.");
        }

        return new HouseRules
        {
            CheckInTime = checkInTime,
            CheckOutTime = checkOutTime,
            MaxGuests = maxGuests,
            PetsAllowed = petsAllowed,
            PetsNotes = petsNotes?.Length > 500 ? petsNotes[..500] : petsNotes,
            SmokingAllowed = smokingAllowed,
            PartiesAllowed = partiesAllowed,
            QuietHoursStart = quietHoursStart,
            QuietHoursEnd = quietHoursEnd,
            LeavingInstructions = leavingInstructions?.Length > 2000 ? leavingInstructions[..2000] : leavingInstructions,
            AdditionalRules = additionalRules?.Length > 2000 ? additionalRules[..2000] : additionalRules
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CheckInTime;
        yield return CheckOutTime;
        yield return MaxGuests;
        yield return PetsAllowed;
        yield return PetsNotes;
        yield return SmokingAllowed;
        yield return PartiesAllowed;
        yield return QuietHoursStart;
        yield return QuietHoursEnd;
        yield return LeavingInstructions;
        yield return AdditionalRules;
    }
}
