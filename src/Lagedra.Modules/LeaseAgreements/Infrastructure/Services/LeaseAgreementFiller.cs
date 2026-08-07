using System.Globalization;
using System.Text.RegularExpressions;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public sealed partial class LeaseAgreementFiller(
    IDealApplicationStatusProvider dealProvider,
    IListingProvider listingProvider,
    ILeasePartyProfileProvider partyProfileProvider,
    ILeaseAgreementTemplateProvider templateProvider,
    IClock clock) : ILeaseAgreementFiller
{
    public async Task<FilledLeaseAgreement> FillForDealAsync(Guid dealId, CancellationToken ct = default)
    {
        var deal = await dealProvider.GetDealDetailsAsync(dealId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Deal '{dealId}' not found.");

        var listing = await listingProvider.GetListingDetailsAsync(deal.ListingId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Listing '{deal.ListingId}' not found.");

        var jurisdictionCode = string.IsNullOrWhiteSpace(listing.JurisdictionCode)
            ? "US-CA"
            : listing.JurisdictionCode!;

        var template = await templateProvider.GetActiveTemplateAsync(jurisdictionCode, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No published lease template for jurisdiction '{jurisdictionCode}'.");

        var host = await partyProfileProvider.GetAsync(deal.LandlordUserId, ct).ConfigureAwait(false);
        var tenant = await partyProfileProvider.GetAsync(deal.TenantUserId, ct).ConfigureAwait(false);
        var terms = listing.LeaseTerms;
        var address = listing.PreciseAddress;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["lease.effectiveDate"] = FormatDate(clock.UtcNow.Date),
            ["host.fullName"] = host?.FullName ?? string.Empty,
            ["host.phone"] = host?.Phone ?? string.Empty,
            ["host.email"] = host?.Email ?? string.Empty,
            ["host.mailingAddress"] = host?.MailingAddress ?? string.Empty,
            ["host.noticeAddress"] = host?.NoticeAddress ?? host?.MailingAddress ?? string.Empty,
            ["broker.name"] = host?.BrokerName ?? string.Empty,
            ["broker.dreLicense"] = host?.BrokerDreLicense ?? string.Empty,
            ["broker.scopeNotes"] = host?.BrokerScopeNotes ?? string.Empty,
            ["tenant.fullName"] = tenant?.FullName ?? string.Empty,
            ["tenant.phone"] = tenant?.Phone ?? string.Empty,
            ["tenant.email"] = tenant?.Email ?? string.Empty,
            ["tenant.mailingAddress"] = tenant?.MailingAddress
                ?? FormatFullAddress(address),
            ["tenant.additionalOccupants"] = string.Empty,
            ["listing.propertyTypeLabel"] = HumanizePropertyType(listing.PropertyType),
            // Treat blank owned-entity rows the same as a missing address so the
            // required-placeholder check fails with a clear message instead of
            // embedding ", ,  , " into a signed lease.
            ["listing.fullAddress"] = FormatFullAddress(address),
            ["listing.paymentMethods"] = terms?.PaymentMethods ?? string.Empty,
            ["listing.rentDueDay"] = FormatDueDay(terms?.RentDueDayOfMonth ?? 1),
            ["listing.nsfFirstFee"] = FormatMoney(terms?.NsfFirstFeeCents ?? 2500),
            ["listing.nsfSubsequentFee"] = FormatMoney(terms?.NsfSubsequentFeeCents ?? 3500),
            ["listing.lateFeePercent"] = $"{(terms?.LateFeePercent ?? 5m).ToString("0.##", CultureInfo.InvariantCulture)}%",
            ["listing.lateFeeGraceDays"] = (terms?.LateFeeGraceDays ?? 3).ToString(CultureInfo.InvariantCulture),
            ["listing.utilitiesResponsibility"] = terms?.UtilitiesResponsibility ?? "Tenant pays all utilities",
            ["listing.yardMaintenanceByTenant"] = YesNo(terms?.YardMaintenanceByTenant ?? false),
            ["listing.furnished"] = terms?.Furnished == true ? "furnished" : "unfurnished",
            ["listing.includedAppliances"] = terms?.IncludedAppliancesNotes
                ?? string.Join(", ", listing.AmenityNames ?? Array.Empty<string>()),
            ["listing.keyCount"] = (terms?.KeyCount ?? 1).ToString(CultureInfo.InvariantCulture),
            ["listing.mailboxKeyCount"] = (terms?.MailboxKeyCount ?? 0).ToString(CultureInfo.InvariantCulture),
            ["listing.keyReplacementFee"] = FormatMoney(terms?.KeyReplacementFeeCents ?? 20000),
            ["listing.lockoutFee"] = FormatMoney(terms?.LockoutFeeCents ?? 20000),
            ["listing.parkingSpaces"] = (terms?.ParkingSpaceCount ?? 0).ToString(CultureInfo.InvariantCulture),
            ["listing.parkingDescription"] = terms?.ParkingDescription ?? string.Empty,
            ["listing.maxGuests"] = (listing.HouseRules?.MaxGuests ?? deal.GuestCount)
                .ToString(CultureInfo.InvariantCulture),
            ["listing.maxGuestConsecutiveDays"] = (terms?.MaxGuestConsecutiveDays ?? 7)
                .ToString(CultureInfo.InvariantCulture),
            ["listing.petsAllowed"] = YesNo(listing.HouseRules?.PetsAllowed ?? false),
            ["listing.petsNotes"] = listing.HouseRules?.PetsNotes ?? string.Empty,
            ["listing.smokingAllowed"] = YesNo(listing.HouseRules?.SmokingAllowed ?? false),
            ["listing.rentersInsuranceMinLiability"] = FormatMoney(terms?.RentersInsuranceMinLiabilityCents ?? 100_000_00),
            ["listing.earlyTerminationFeeMonths"] = (terms?.EarlyTerminationFeeMonths ?? 2)
                .ToString(CultureInfo.InvariantCulture),
            ["listing.leadPaintKnowledge"] = terms?.LeadPaintKnowledge
                ?? "The Landlord has no records or reports pertaining to lead-based paint and/or lead-based paint hazards in the Leased Property.",
            ["listing.builtBefore1978"] = YesNo(terms?.BuiltBefore1978 ?? false),
            ["listing.rentCapJustCauseExempt"] = YesNo(terms?.RentCapJustCauseExempt ?? false),
            ["deal.startDate"] = FormatDate(deal.RequestedCheckIn.ToDateTime(TimeOnly.MinValue)),
            ["deal.endDate"] = FormatDate(deal.RequestedCheckOut.ToDateTime(TimeOnly.MinValue)),
            ["deal.termMonths"] = FormatTermMonths(deal.RequestedCheckIn, deal.RequestedCheckOut),
            ["deal.monthlyRent"] = FormatMoney(deal.FirstMonthRentCents ?? listing.MonthlyRentCents),
            ["deal.securityDeposit"] = FormatMoney(deal.DepositAmountCents ?? 0),
            ["deal.guestCount"] = deal.GuestCount.ToString(CultureInfo.InvariantCulture),
        };

        var usedKeys = ExtractPlaceholders(template.BodyHtml).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingRequired = LeasePlaceholderCatalog.RequiredKeys
            .Where(k => usedKeys.Contains(k) && string.IsNullOrWhiteSpace(values.GetValueOrDefault(k)))
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var filledHtml = ReplacePlaceholders(template.BodyHtml, values);

        return new FilledLeaseAgreement(
            template.TemplateId,
            template.ActiveVersionId,
            template.VersionNumber,
            template.JurisdictionCode,
            template.Title,
            filledHtml,
            values,
            missingRequired);
    }

    private static IEnumerable<string> ExtractPlaceholders(string bodyHtml) =>
        PlaceholderRegex().Matches(bodyHtml).Select(m => m.Groups[1].Value.Trim());

    private static string ReplacePlaceholders(string bodyHtml, Dictionary<string, string> values) =>
        PlaceholderRegex().Replace(bodyHtml, m =>
        {
            var key = m.Groups[1].Value.Trim();
            return values.TryGetValue(key, out var value)
                ? System.Net.WebUtility.HtmlEncode(value)
                : m.Value;
        });

    private static string FormatMoney(long cents) =>
        (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"));

    private static string FormatDate(DateTime date) =>
        date.ToString("MMMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));

    private static string FormatDueDay(int day) => day switch
    {
        1 => "first",
        2 => "second",
        3 => "third",
        _ => day.ToString(CultureInfo.InvariantCulture)
    };

    private static string FormatTermMonths(DateOnly start, DateOnly end)
    {
        var months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
        if (end.Day < start.Day) months--;
        if (months < 1) months = 1;
        return months switch
        {
            1 => "one (1)",
            2 => "two (2)",
            3 => "three (3)",
            6 => "six (6)",
            12 => "twelve (12)",
            _ => $"{months} ({months})"
        };
    }

    private static string HumanizePropertyType(string? propertyType) =>
        string.IsNullOrWhiteSpace(propertyType)
            ? "residence"
            : propertyType.ToUpperInvariant() switch
            {
                "HOUSE" => "single-family home",
                "APARTMENT" => "apartment",
                "CONDO" => "condominium",
                "TOWNHOUSE" => "townhouse",
                "STUDIO" => "studio",
                _ => propertyType
            };

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string FormatFullAddress(ListingAddressDto? address)
    {
        if (address is null
            || string.IsNullOrWhiteSpace(address.Street)
            || string.IsNullOrWhiteSpace(address.City)
            || string.IsNullOrWhiteSpace(address.State)
            || string.IsNullOrWhiteSpace(address.ZipCode))
        {
            return string.Empty;
        }

        var country = string.IsNullOrWhiteSpace(address.Country) ? "US" : address.Country.Trim();
        return $"{address.Street.Trim()}, {address.City.Trim()}, {address.State.Trim()} {address.ZipCode.Trim()}, {country}";
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_.]+)\s*\}\}")]
    private static partial Regex PlaceholderRegex();
}
