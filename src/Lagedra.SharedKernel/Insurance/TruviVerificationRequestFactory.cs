using System.Globalization;
using System.Text.RegularExpressions;

namespace Lagedra.SharedKernel.Insurance;

/// <summary>
/// Builds Screen &amp; Protect create/modify/cancel bodies. Pure mapping — no HTTP.
/// </summary>
public static partial class TruviVerificationRequestFactory
{
    public const string DirectWebChannel = "Direct web";
    public const string CompleteProtection = "Complete Protection";
    public const int DefaultExtendedAmount = 50_000;

    /// <summary>
    /// Deductible amounts Truvi accepts. Pick the largest value that does not
    /// exceed the stay's security deposit so coverage starts when the deposit
    /// is exhausted (a $2,000 deposit maps to 1000, not 5000).
    /// </summary>
    private static readonly int[] AllowedStartingLevels = [250, 500, 1000, 5000, 10_000];

    public static string FormatTimestamp(DateTime utcNow)
    {
        var utc = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : utcNow.ToUniversalTime();
        return utc.ToString("yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture);
    }

    public static string EchoTokenForDeal(Guid dealId) => dealId.ToString("D");

    public static string ReservationIdForDeal(Guid dealId) => dealId.ToString("D");

    public static string RescreenReservationId(Guid dealId, DateTime utcNow)
    {
        var utc = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : utcNow.ToUniversalTime();
        return $"{dealId:D}-r{utc:yyyyMMddHHmmss}";
    }

    public static int? StartingLevelForDepositCents(long? depositAmountCents)
    {
        if (depositAmountCents is null or < 250_00)
        {
            return null;
        }

        var dollars = depositAmountCents.Value / 100;
        for (var i = AllowedStartingLevels.Length - 1; i >= 0; i--)
        {
            if (AllowedStartingLevels[i] <= dollars)
            {
                return AllowedStartingLevels[i];
            }
        }

        return null;
    }

    public static bool TryResolveCompany(
        string? companyName,
        string? fullName,
        string? email,
        out string name,
        out string resolvedEmail,
        out string? error)
    {
        name = string.Empty;
        resolvedEmail = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Host or property-manager email is required for company identity.";
            return false;
        }

        var resolvedName = !string.IsNullOrWhiteSpace(companyName)
            ? companyName.Trim()
            : fullName?.Trim();
        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            error = "Host or property-manager name is required for company identity.";
            return false;
        }

        name = resolvedName;
        resolvedEmail = email.Trim();
        return true;
    }

    public static bool IsExcludedListingType(string? propertyType)
    {
        if (string.IsNullOrWhiteSpace(propertyType))
        {
            return false;
        }

        return propertyType.Equals("Event", StringComparison.OrdinalIgnoreCase)
            || propertyType.Equals("EventSpace", StringComparison.OrdinalIgnoreCase)
            || propertyType.Equals("Communal", StringComparison.OrdinalIgnoreCase)
            || propertyType.Equals("CommunalSpace", StringComparison.OrdinalIgnoreCase);
    }

    public static string StripDigits(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return DigitRegex().Replace(value, string.Empty).Trim();
    }

    public static string? ToCountryIso(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        var trimmed = country.Trim();
        if (Iso3ByAlias.TryGetValue(trimmed, out var mapped))
        {
            return mapped;
        }

        if (trimmed.Length == 3 && trimmed.All(char.IsLetter))
        {
            return trimmed.ToUpperInvariant();
        }

        if (trimmed.Length == 2 && trimmed.All(char.IsLetter)
            && Iso3ByAlias.TryGetValue(trimmed.ToUpperInvariant(), out var fromAlpha2))
        {
            return fromAlpha2;
        }

        return null;
    }

    public static bool TryCreate(
        Guid dealId,
        DateTime utcNow,
        string companyName,
        string companyEmail,
        int extendedAmount,
        string? street,
        string? city,
        string? postcode,
        string? country,
        bool petsAllowed,
        int guestCount,
        int bedrooms,
        decimal bathrooms,
        DateOnly checkIn,
        DateOnly checkOut,
        string? firstName,
        string? lastName,
        string? fullName,
        string? email,
        string? phone,
        out TruviCreateVerificationRequest? request,
        out string? error)
        => TryCreate(
            dealId,
            utcNow,
            companyName,
            companyEmail,
            extendedAmount,
            street,
            city,
            postcode,
            country,
            petsAllowed,
            guestCount,
            bedrooms,
            bathrooms,
            checkIn,
            checkOut,
            firstName,
            lastName,
            fullName,
            email,
            phone,
            depositAmountCents: null,
            reservationId: null,
            out request,
            out error);

    public static bool TryCreate(
        Guid dealId,
        DateTime utcNow,
        string companyName,
        string companyEmail,
        int extendedAmount,
        string? street,
        string? city,
        string? postcode,
        string? country,
        bool petsAllowed,
        int guestCount,
        int bedrooms,
        decimal bathrooms,
        DateOnly checkIn,
        DateOnly checkOut,
        string? firstName,
        string? lastName,
        string? fullName,
        string? email,
        string? phone,
        long? depositAmountCents,
        string? reservationId,
        out TruviCreateVerificationRequest? request,
        out string? error)
    {
        request = null;
        error = null;

        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(postcode) || string.IsNullOrWhiteSpace(country))
        {
            error = "Listing precise address is incomplete.";
            return false;
        }

        var countryIso = ToCountryIso(country);
        if (countryIso is null)
        {
            error = $"Listing country '{country}' is not a usable ISO alpha-3 code.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Guest email is required for Direct web screening.";
            return false;
        }

        var (resolvedFirst, resolvedLast) = ResolveNames(firstName, lastName, fullName);
        if (string.IsNullOrWhiteSpace(resolvedFirst) || string.IsNullOrWhiteSpace(resolvedLast))
        {
            error = "Guest first and last name are required.";
            return false;
        }

        var metadata = new TruviVerificationMetadata(
            FormatTimestamp(utcNow),
            EchoTokenForDeal(dealId));

        request = new TruviCreateVerificationRequest(
            metadata,
            new TruviCompany(companyName, companyEmail),
            new TruviListing(
                new TruviListingAddress(street.Trim(), city.Trim(), countryIso, postcode.Trim()),
                petsAllowed,
                guestCount > 0 ? guestCount : null,
                bathrooms > 0 ? (int)decimal.Round(bathrooms, MidpointRounding.AwayFromZero) : null,
                bedrooms > 0 ? bedrooms : null),
            new TruviReservation(
                string.IsNullOrWhiteSpace(reservationId) ? ReservationIdForDeal(dealId) : reservationId,
                checkIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                checkOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DirectWebChannel,
                DateOnly.FromDateTime(utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime())
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new TruviGuest(resolvedFirst, resolvedLast, email.Trim(),
                string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()),
            new TruviProtection(
                CompleteProtection,
                extendedAmount,
                HasPetProtection: false,
                StartingLevel: StartingLevelForDepositCents(depositAmountCents)));

        return true;
    }

    public static TruviModifyVerificationRequest Modify(
        Guid dealId,
        DateTime utcNow,
        string verificationId,
        string reservationId,
        DateOnly checkIn,
        DateOnly checkOut,
        bool petsAllowed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);

        return new TruviModifyVerificationRequest(
            new TruviVerificationMetadata(FormatTimestamp(utcNow), EchoTokenForDeal(dealId)),
            new TruviCancelVerificationRef(verificationId),
            new TruviModifyReservation(
                reservationId,
                checkIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                checkOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new TruviModifyListing(petsAllowed));
    }

    public static TruviCancelVerificationRequest Cancel(
        Guid dealId,
        DateTime utcNow,
        string verificationId,
        string? reservationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationId);

        return new TruviCancelVerificationRequest(
            new TruviVerificationMetadata(FormatTimestamp(utcNow), EchoTokenForDeal(dealId)),
            new TruviCancelVerificationRef(verificationId),
            new TruviCancelReservationRef(
                string.IsNullOrWhiteSpace(reservationId)
                    ? ReservationIdForDeal(dealId)
                    : reservationId));
    }

    public static (string First, string Last) ResolveNames(
        string? firstName,
        string? lastName,
        string? fullName)
    {
        var first = StripDigits(firstName ?? string.Empty);
        var last = StripDigits(lastName ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
        {
            return (first, last);
        }

        var cleanedFull = StripDigits(fullName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(cleanedFull))
        {
            return (first, last);
        }

        var parts = cleanedFull.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return (string.IsNullOrWhiteSpace(first) ? parts[0] : first, last);
        }

        return (
            string.IsNullOrWhiteSpace(first) ? parts[0] : first,
            string.IsNullOrWhiteSpace(last) ? string.Join(' ', parts.Skip(1)) : last);
    }

    private static readonly Dictionary<string, string> Iso3ByAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = "USA",
        ["USA"] = "USA",
        ["United States"] = "USA",
        ["United States of America"] = "USA",
        ["GB"] = "GBR",
        ["UK"] = "GBR",
        ["GBR"] = "GBR",
        ["United Kingdom"] = "GBR",
        ["CA"] = "CAN",
        ["CAN"] = "CAN",
        ["Canada"] = "CAN",
        ["MX"] = "MEX",
        ["MEX"] = "MEX",
        ["Mexico"] = "MEX",
        ["AU"] = "AUS",
        ["AUS"] = "AUS",
        ["Australia"] = "AUS",
    };

    [GeneratedRegex(@"\d", RegexOptions.CultureInvariant)]
    private static partial Regex DigitRegex();
}
