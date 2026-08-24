namespace Lagedra.Modules.LeaseAgreements.Application.Services;

public sealed record LeasePlaceholderDefinition(
    string Key,
    string Group,
    string Label,
    string Description,
    string Example,
    bool Required);

/// <summary>
/// Canonical list of merge variables admins can insert into lease templates.
/// </summary>
public static class LeasePlaceholderCatalog
{
    public static IReadOnlyList<LeasePlaceholderDefinition> All { get; } =
    [
        Def("lease.effectiveDate", "Lease", "Lease effective date", "Date the lease is entered into.", "May 1, 2026", true),
        Def("host.fullName", "Host", "Host / property manager full name", "Legal name of the listing host (landlord when they own the home; property manager otherwise).", "Jane Host", true),
        Def("host.phone", "Host", "Host phone", "Landlord phone number.", "(555) 010-1000", true),
        Def("host.email", "Host", "Host email", "Landlord email address.", "host@example.com", true),
        Def("host.mailingAddress", "Host", "Host mailing address", "Landlord mailing / notice address.", "123 Main St, Los Angeles, CA 90012", true),
        Def("host.noticeAddress", "Host", "Host notice address", "Address for formal notices (defaults to mailing).", "123 Main St, Los Angeles, CA 90012", false),
        Def("owner.fullName", "Owner", "Home owner full name", "Property owner when a manager lists the home (optional).", "Casey Owner", false),
        Def("owner.email", "Owner", "Home owner email", "Owner account email for consent on the lease.", "owner@example.com", false),
        Def("owner.phone", "Owner", "Home owner phone", "Owner phone number.", "(555) 010-3000", false),
        Def("owner.mailingAddress", "Owner", "Home owner mailing address", "Owner mailing address for notices.", "456 Oak Ave, Los Angeles, CA 90026", false),
        Def("owner.consentDate", "Owner", "Owner consent date", "Date the owner consented to this tenancy in Lagedra.", "August 18, 2026", false),
        Def("owner.consentVersion", "Owner", "Owner consent version", "Consent record version captured at owner approval.", "owner-tenancy-consent-v1", false),
        Def("owner.consented", "Owner", "Owner consented", "Yes when the owner recorded in-app consent for this stay.", "Yes", false),
        Def("broker.name", "Host", "Broker name", "Licensed broker acting for the landlord (optional).", "Alex Broker", false),
        Def("broker.dreLicense", "Host", "Broker DRE license", "California DRE license number.", "01234567", false),
        Def("broker.scopeNotes", "Host", "Broker scope notes", "Optional broker disclosure notes.", "Oversees notices and compliance.", false),
        Def("tenant.fullName", "Tenant", "Tenant full name", "Primary tenant legal name.", "Jordan Tenant", true),
        Def("tenant.phone", "Tenant", "Tenant phone", "Tenant phone number.", "(555) 010-2000", true),
        Def("tenant.email", "Tenant", "Tenant email", "Tenant email address.", "tenant@example.com", true),
        Def("tenant.mailingAddress", "Tenant", "Tenant mailing address", "Tenant mailing / notice address.", "8917 Wakefield Ave, Panorama City, CA 91402", false),
        Def("tenant.additionalOccupants", "Tenant", "Additional occupants", "Other adults/children listed on the lease.", "Alex Tenant", false),
        Def("listing.propertyTypeLabel", "Listing", "Property type", "Human-readable property type.", "single-family home", true),
        Def("listing.fullAddress", "Listing", "Property address", "Full leased property address.", "8917 Wakefield Ave, Panorama City, CA 91402", true),
        Def("listing.paymentMethods", "Listing", "Accepted payment methods", "How rent may be paid after month one.", "Zelle, Electronic Funds Transfer", false),
        Def("listing.rentDueDay", "Listing", "Rent due day", "Day of month rent is due.", "first", true),
        Def("listing.nsfFirstFee", "Listing", "NSF first fee", "Fee for first returned payment.", "$25.00", false),
        Def("listing.nsfSubsequentFee", "Listing", "NSF subsequent fee", "Fee for later returned payments.", "$35.00", false),
        Def("listing.lateFeePercent", "Listing", "Late fee percent", "Late fee as percent of monthly rent.", "5%", false),
        Def("listing.lateFeeAmount", "Listing", "Late fee amount", "Computed late fee in dollars.", "$180.00", false),
        Def("listing.lateFeeGraceDays", "Listing", "Late fee grace days", "Days after due date before late fee.", "3", false),
        Def("listing.isFurnished", "Listing", "Is furnished", "Yes/No whether the premises is furnished.", "Yes", false),
        Def("listing.maintenanceContactName", "Listing", "Maintenance contact name", "Name for routine maintenance requests.", "Jane Host", false),
        Def("listing.maintenanceContactPhone", "Listing", "Maintenance contact phone", "Phone for routine maintenance requests.", "(555) 010-1000", false),
        Def("listing.maintenanceContactEmail", "Listing", "Maintenance contact email", "Email for routine maintenance requests.", "host@example.com", false),
        Def("listing.utilitiesResponsibility", "Listing", "Utilities responsibility", "Who pays utilities.", "Tenant pays all utilities", false),
        Def("listing.yardMaintenanceByTenant", "Listing", "Yard maintenance by tenant", "Yes/No whether tenant maintains yard.", "Yes", false),
        Def("listing.furnished", "Listing", "Furnished", "Furnished / unfurnished label.", "unfurnished", false),
        Def("listing.includedAppliances", "Listing", "Included appliances", "Appliances included with the rental.", "refrigerator and stove/oven", false),
        Def("listing.keyCount", "Listing", "Key count", "Number of property keys provided.", "1", false),
        Def("listing.mailboxKeyCount", "Listing", "Mailbox key count", "Number of mailbox keys provided.", "0", false),
        Def("listing.keyReplacementFee", "Listing", "Key replacement fee", "Fee if keys are not returned.", "$200.00", false),
        Def("listing.lockoutFee", "Listing", "Lockout fee", "Fee to regain entry after lockout.", "$200.00", false),
        Def("listing.parkingSpaces", "Listing", "Parking spaces", "Number of parking spaces.", "4", false),
        Def("listing.parkingDescription", "Listing", "Parking description", "Where parking is located.", "Driveway and Garage", false),
        Def("listing.maxGuests", "Listing", "Max guests", "Maximum guests at one time.", "5", false),
        Def("listing.maxGuestConsecutiveDays", "Listing", "Max guest consecutive days", "Max consecutive guest stay days.", "7", false),
        Def("listing.petsAllowed", "Listing", "Pets allowed", "Whether pets are allowed.", "No", false),
        Def("listing.petsNotes", "Listing", "Pets notes", "Additional pet policy notes.", "Service animals excepted by law.", false),
        Def("listing.smokingAllowed", "Listing", "Smoking allowed", "Whether smoking is allowed.", "No", false),
        Def("listing.rentersInsuranceMinLiability", "Listing", "Renter's insurance minimum", "Minimum liability coverage required.", "$100,000.00", false),
        Def("listing.earlyTerminationFeeMonths", "Listing", "Early termination fee months", "Months of rent for early termination fee.", "2", false),
        Def("listing.earlyTerminationFeeAmount", "Listing", "Early termination fee amount", "Computed early-termination charge.", "$7,200.00", false),
        Def("listing.leadPaintKnowledge", "Listing", "Lead paint knowledge", "Landlord lead-paint knowledge statement.", "The Landlord has no records or reports pertaining to lead-based paint.", false),
        Def("listing.builtBefore1978", "Listing", "Built before 1978", "Yes/No for lead disclosure applicability.", "Yes", false),
        Def("listing.rentCapJustCauseExempt", "Listing", "Rent cap / just cause exempt", "Yes/No exemption attestation.", "No", false),
        Def("deal.startDate", "Deal", "Lease start date", "Check-in / start date.", "May 1, 2026", true),
        Def("deal.endDate", "Deal", "Lease end date", "Check-out / end date.", "April 30, 2027", true),
        Def("deal.termMonths", "Deal", "Term months", "Fixed term length in months.", "twelve (12)", true),
        Def("deal.monthlyRent", "Deal", "Monthly rent", "Monthly rent amount.", "$3,600.00", true),
        Def("deal.securityDeposit", "Deal", "Security deposit", "Security deposit amount.", "$3,600.00", true),
        Def("deal.guestCount", "Deal", "Guest count", "Number of guests on the booking.", "2", false),
    ];

    public static IReadOnlySet<string> RequiredKeys { get; } =
        All.Where(p => p.Required).Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlySet<string> AllKeys { get; } =
        All.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public const string UsageExampleHtml =
        """
        <p>This Lease Agreement ("Lease") is entered into on <strong>{{lease.effectiveDate}}</strong>,
        by and between <strong>{{host.fullName}}</strong> ("Landlord"), and
        <strong>{{tenant.fullName}}</strong> ("Tenant").</p>
        <p>The Landlord hereby leases to the Tenant the <em>{{listing.propertyTypeLabel}}</em>
        located at <strong>{{listing.fullAddress}}</strong>.</p>
        <p>Rent: <strong>{{deal.monthlyRent}}</strong> due on the <strong>{{listing.rentDueDay}}</strong>
        day of each month. Security Deposit: <strong>{{deal.securityDeposit}}</strong>.</p>
        """;

    private static LeasePlaceholderDefinition Def(
        string key,
        string group,
        string label,
        string description,
        string example,
        bool required) =>
        new(key, group, label, description, example, required);
}
