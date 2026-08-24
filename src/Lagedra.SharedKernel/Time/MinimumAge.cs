namespace Lagedra.SharedKernel.Time;

/// <summary>
/// Age-gate rule shared by every place that accepts a date of birth
/// (profile, manual KYC, provider KYC). Lagedra users sign leases, so they
/// must be legal adults.
/// </summary>
public static class MinimumAge
{
    public const int AdultYears = 18;

    /// <summary>
    /// True when someone born on <paramref name="dateOfBirth"/> has reached
    /// their <paramref name="years"/>th birthday as of <paramref name="utcNow"/>.
    /// Feb 29 birthdays count as reached on Mar 1 in non-leap years (the
    /// conservative reading).
    /// </summary>
    public static bool IsAtLeast(int years, DateOnly dateOfBirth, DateTime utcNow) =>
        dateOfBirth <= DateOnly.FromDateTime(utcNow.Date).AddYears(-years);

    public static bool IsAtLeast(int years, DateTime dateOfBirth, DateTime utcNow) =>
        IsAtLeast(years, DateOnly.FromDateTime(dateOfBirth.Date), utcNow);
}
