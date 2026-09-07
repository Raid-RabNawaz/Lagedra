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
    /// <summary>Fill-in rule shown where a preview cannot know the real value.</summary>
    private const string BlankRule = "__________";

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
        var ownerUserId = deal.HomeOwnerUserId ?? listing.HomeOwnerUserId;
        var owner = ownerUserId is Guid ownerId
            ? await partyProfileProvider.GetAsync(ownerId, ct).ConfigureAwait(false)
            : null;
        var includeBroker = listing.IncludeBrokerClause;
        var terms = listing.LeaseTerms;
        var address = listing.PreciseAddress;
        var rentCents = deal.FirstMonthRentCents ?? listing.MonthlyRentCents;
        var latePercent = terms?.LateFeePercent ?? 5m;
        var lateFeeCents = (long)Math.Round(rentCents * latePercent / 100m, MidpointRounding.AwayFromZero);
        var earlyFeeMonths = terms?.EarlyTerminationFeeMonths ?? 2;
        var earlyFeeCents = rentCents * earlyFeeMonths;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["lease.effectiveDate"] = FormatDate(clock.UtcNow.Date),
            ["host.fullName"] = host?.FullName ?? string.Empty,
            ["host.phone"] = host?.Phone ?? string.Empty,
            ["host.email"] = host?.Email ?? string.Empty,
            ["host.mailingAddress"] = host?.MailingAddress ?? string.Empty,
            ["host.noticeAddress"] = host?.NoticeAddress ?? host?.MailingAddress ?? string.Empty,
            ["owner.fullName"] = owner?.FullName ?? string.Empty,
            ["owner.email"] = owner?.Email ?? string.Empty,
            ["owner.phone"] = owner?.Phone ?? string.Empty,
            ["owner.mailingAddress"] = owner?.MailingAddress ?? string.Empty,
            ["owner.consentDate"] = deal.OwnerTenancyConsentAt is DateTime consentedAt
                ? FormatDate(consentedAt)
                : string.Empty,
            ["owner.consentVersion"] = deal.OwnerConsentVersion ?? string.Empty,
            ["owner.consented"] = YesNo(deal.OwnerTenancyConsentGiven),
            ["broker.name"] = includeBroker ? host?.BrokerName ?? string.Empty : string.Empty,
            ["broker.dreLicense"] = includeBroker ? host?.BrokerDreLicense ?? string.Empty : string.Empty,
            ["broker.scopeNotes"] = includeBroker ? host?.BrokerScopeNotes ?? string.Empty : string.Empty,
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
            ["listing.lateFeeAmount"] = FormatMoney(lateFeeCents),
            ["listing.lateFeeGraceDays"] = (terms?.LateFeeGraceDays ?? 3).ToString(CultureInfo.InvariantCulture),
            ["listing.isFurnished"] = YesNo(terms?.Furnished ?? false),
            ["listing.maintenanceContactName"] = !string.IsNullOrWhiteSpace(host?.BrokerName)
                ? host!.BrokerName!
                : host?.FullName ?? string.Empty,
            ["listing.maintenanceContactPhone"] = host?.Phone ?? string.Empty,
            ["listing.maintenanceContactEmail"] = host?.Email ?? string.Empty,
            ["listing.utilitiesResponsibility"] = ExpandUtilities(terms?.UtilitiesResponsibility),
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
            ["listing.earlyTerminationFeeMonths"] = earlyFeeMonths
                .ToString(CultureInfo.InvariantCulture),
            ["listing.earlyTerminationFeeAmount"] = FormatMoney(earlyFeeCents),
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

        var withConditionals = LeaseTemplateConditionals.Apply(template.BodyHtml, values);
        var filledHtml = ReplacePlaceholders(withConditionals, values);

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

    public async Task<FilledLeaseAgreement> FillPreviewForListingAsync(
        Guid listingId,
        CancellationToken ct = default)
    {
        var listing = await listingProvider.GetListingDetailsAsync(listingId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Listing '{listingId}' not found.");

        var jurisdictionCode = string.IsNullOrWhiteSpace(listing.JurisdictionCode)
            ? "US-CA"
            : listing.JurisdictionCode!;

        var template = await templateProvider.GetActiveTemplateAsync(jurisdictionCode, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No published lease template for jurisdiction '{jurisdictionCode}'.");

        var terms = listing.LeaseTerms;
        var rentCents = listing.MonthlyRentCents;
        var depositCents = listing.DefaultDepositCents ?? listing.MaxDepositCents;
        var latePercent = terms?.LateFeePercent ?? 5m;
        var lateFeeCents = (long)Math.Round(rentCents * latePercent / 100m, MidpointRounding.AwayFromZero);
        var earlyFeeMonths = terms?.EarlyTerminationFeeMonths ?? 2;
        var hasOwner = listing.HomeOwnerUserId is not null;
        var includeBroker = listing.IncludeBrokerClause;

        // Everything the listing genuinely knows. Party identities, dates and the
        // street address are added below as fill-in rules.
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["listing.propertyTypeLabel"] = HumanizePropertyType(listing.PropertyType),
            ["listing.fullAddress"] = FormatPreviewAddress(listing.PreciseAddress),
            ["listing.paymentMethods"] = terms?.PaymentMethods ?? string.Empty,
            ["listing.rentDueDay"] = FormatDueDay(terms?.RentDueDayOfMonth ?? 1),
            ["listing.nsfFirstFee"] = FormatMoney(terms?.NsfFirstFeeCents ?? 2500),
            ["listing.nsfSubsequentFee"] = FormatMoney(terms?.NsfSubsequentFeeCents ?? 3500),
            ["listing.lateFeePercent"] = $"{latePercent.ToString("0.##", CultureInfo.InvariantCulture)}%",
            ["listing.lateFeeAmount"] = FormatMoney(lateFeeCents),
            ["listing.lateFeeGraceDays"] = (terms?.LateFeeGraceDays ?? 3).ToString(CultureInfo.InvariantCulture),
            ["listing.isFurnished"] = YesNo(terms?.Furnished ?? false),
            ["listing.utilitiesResponsibility"] = ExpandUtilities(terms?.UtilitiesResponsibility),
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
            ["listing.maxGuests"] = (listing.HouseRules?.MaxGuests ?? 0).ToString(CultureInfo.InvariantCulture),
            ["listing.maxGuestConsecutiveDays"] = (terms?.MaxGuestConsecutiveDays ?? 7)
                .ToString(CultureInfo.InvariantCulture),
            ["listing.petsAllowed"] = YesNo(listing.HouseRules?.PetsAllowed ?? false),
            ["listing.petsNotes"] = listing.HouseRules?.PetsNotes ?? string.Empty,
            ["listing.smokingAllowed"] = YesNo(listing.HouseRules?.SmokingAllowed ?? false),
            ["listing.rentersInsuranceMinLiability"] = FormatMoney(terms?.RentersInsuranceMinLiabilityCents ?? 100_000_00),
            ["listing.earlyTerminationFeeMonths"] = earlyFeeMonths.ToString(CultureInfo.InvariantCulture),
            ["listing.earlyTerminationFeeAmount"] = FormatMoney(rentCents * earlyFeeMonths),
            ["listing.leadPaintKnowledge"] = terms?.LeadPaintKnowledge
                ?? "The Landlord has no records or reports pertaining to lead-based paint and/or lead-based paint hazards in the Leased Property.",
            ["listing.builtBefore1978"] = YesNo(terms?.BuiltBefore1978 ?? false),
            ["listing.rentCapJustCauseExempt"] = YesNo(terms?.RentCapJustCauseExempt ?? false),
            ["deal.monthlyRent"] = FormatMoney(rentCents),
            ["deal.securityDeposit"] = FormatMoney(depositCents),
        };

        // ReplacePlaceholders leaves unknown tokens untouched, so any catalog key
        // we do not fill would render as a literal "{{tenant.fullName}}" in the
        // tenant's PDF. Everything still unset becomes a fill-in rule.
        foreach (var key in LeasePlaceholderCatalog.AllKeys)
        {
            if (!values.ContainsKey(key))
            {
                values[key] = BlankRule;
            }
        }

        // Conditionals are evaluated against a separate view: a blank value is
        // falsy, so using the display values directly would silently delete every
        // clause that depends on a party or date. Anything that is merely unknown
        // at preview time stays visible (with blanks); anything the listing knows
        // to be absent — no home owner, no broker clause — is correctly hidden.
        var conditionValues = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        foreach (var key in LeasePlaceholderCatalog.AllKeys)
        {
            if (!string.Equals(values[key], BlankRule, StringComparison.Ordinal))
            {
                continue;
            }

            conditionValues[key] = key switch
            {
                _ when key.StartsWith("owner.", StringComparison.OrdinalIgnoreCase) =>
                    hasOwner ? "Yes" : string.Empty,
                _ when key.StartsWith("broker.", StringComparison.OrdinalIgnoreCase) =>
                    includeBroker ? "Yes" : string.Empty,
                _ => "Yes"
            };
        }

        var withConditionals = LeaseTemplateConditionals.Apply(template.BodyHtml, conditionValues);
        var filledHtml = ReplacePlaceholders(withConditionals, values);

        // A preview is blank by design, so the required-field check that guards
        // deal PDFs deliberately does not apply here.
        return new FilledLeaseAgreement(
            template.TemplateId,
            template.ActiveVersionId,
            template.VersionNumber,
            template.JurisdictionCode,
            template.Title,
            filledHtml,
            values,
            []);
    }

    /// <summary>
    /// Mirrors the redaction the listing detail endpoint applies for anyone who
    /// is not the owner: city, state and country are public, street and ZIP are
    /// not, so the preview shows the same and no more.
    /// </summary>
    private static string FormatPreviewAddress(ListingAddressDto? address)
    {
        if (address is null
            || string.IsNullOrWhiteSpace(address.City)
            || string.IsNullOrWhiteSpace(address.State))
        {
            return BlankRule;
        }

        return $"{BlankRule}, {address.City.Trim()}, {address.State.Trim()} {BlankRule}";
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

    private static string ExpandUtilities(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)
            || text.Equals("Tenant pays all utilities", StringComparison.OrdinalIgnoreCase))
        {
            return "The Tenant agrees to pay all charges for all utilities, including electricity, internet, cable, gas, water, garbage disposal, and telephones, used in or on the Leased Property during the term of this Lease. The Tenant shall make payments for these utilities directly to the Landlord. The Landlord shall be responsible for making payments to the respective utility companies.";
        }

        return text.Trim();
    }

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
        var line = $"{address.Street.Trim()}, {address.City.Trim()}, {address.State.Trim()} {address.ZipCode.Trim()}";
        if (country.Equals("US", StringComparison.OrdinalIgnoreCase)
            || country.Equals("USA", StringComparison.OrdinalIgnoreCase)
            || country.Equals("United States", StringComparison.OrdinalIgnoreCase))
        {
            return line;
        }

        return $"{line}, {country}";
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_.]+)\s*\}\}")]
    private static partial Regex PlaceholderRegex();
}
