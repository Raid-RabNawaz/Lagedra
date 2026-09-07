using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.LeaseAgreements.Infrastructure;

/// <summary>
/// The preview is what a prospective tenant reads before booking, so it must be
/// readable (no leaked template tokens), complete (clauses not silently dropped)
/// and no more disclosing about the property than the public listing page.
/// </summary>
public class LeaseAgreementFillerPreviewTests
{
    private static readonly Guid ListingId = Guid.NewGuid();

    private const string BodyHtml =
        """
        <p>Entered into on {{lease.effectiveDate}} between {{host.fullName}} ("Landlord")
        and {{tenant.fullName}} ("Tenant").</p>
        <p>Premises: {{listing.fullAddress}} ({{listing.propertyTypeLabel}}).</p>
        <p>Rent {{deal.monthlyRent}} due on the {{listing.rentDueDay}} day. Deposit
        {{deal.securityDeposit}}. Late fee {{listing.lateFeePercent}} ({{listing.lateFeeAmount}})
        after {{listing.lateFeeGraceDays}} days.</p>
        {{#if listing.petsAllowed}}<p>PETS CLAUSE: pets are permitted.</p>{{/if}}
        {{#if tenant.fullName}}<p>TENANT CLAUSE: Tenant shall maintain the premises.</p>{{/if}}
        {{#if broker.name}}<p>BROKER CLAUSE: {{broker.name}} acts for the Landlord.</p>{{/if}}
        {{#if owner.fullName}}<p>OWNER CLAUSE: {{owner.fullName}} consents.</p>{{/if}}
        """;

    private static LeaseAgreementFiller CreateFiller(ListingDetailsDto listing) =>
        new(
            new StubDealProvider(),
            new StubListingProvider(listing),
            new StubPartyProfileProvider(),
            new StubTemplateProvider(BodyHtml),
            new StubClock());

    private static ListingDetailsDto CreateListing(
        bool petsAllowed = true,
        bool includeBrokerClause = false,
        Guid? homeOwnerUserId = null) =>
        new(
            Id: ListingId,
            LandlordUserId: Guid.NewGuid(),
            MinStayDays: 30,
            MaxStayDays: 365,
            MaxDepositCents: 400_000,
            MonthlyRentCents: 360_000,
            JurisdictionCode: "US-CA",
            Title: "Sunny two-bedroom",
            PropertyType: "Apartment",
            Bedrooms: 2,
            Bathrooms: 1.5m,
            PreciseAddress: new ListingAddressDto(
                "8917 Wakefield Ave", "Panorama City", "CA", "91402", "US"),
            HouseRules: new ListingHouseRulesDto(
                "15:00", "11:00", 4, petsAllowed, null, false, false, null, null, null, null),
            DefaultDepositCents: 360_000,
            LeaseTerms: new ListingLeaseTermsDto(
                RentDueDayOfMonth: 1,
                NsfFirstFeeCents: 2500,
                NsfSubsequentFeeCents: 3500,
                LateFeePercent: 5m,
                LateFeeGraceDays: 3,
                UtilitiesResponsibility: null,
                YardMaintenanceByTenant: false,
                Furnished: false,
                IncludedAppliancesNotes: null,
                KeyCount: 1,
                MailboxKeyCount: 0,
                KeyReplacementFeeCents: 20000,
                LockoutFeeCents: 20000,
                ParkingSpaceCount: 1,
                ParkingDescription: null,
                ParkingIncludedInRent: true,
                MaxGuestConsecutiveDays: 7,
                RentersInsuranceMinLiabilityCents: 100_000_00,
                EarlyTerminationFeeMonths: 2,
                BuiltBefore1978: false,
                LeadPaintKnowledge: null,
                RentCapJustCauseExempt: false,
                PaymentMethods: "Zelle"),
            HomeOwnerUserId: homeOwnerUserId,
            IncludeBrokerClause: includeBrokerClause);

    [Fact]
    public async Task Leaves_no_raw_template_tokens_in_the_output()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        result.FilledHtml.Should().NotContain("{{");
        result.FilledHtml.Should().NotContain("}}");
    }

    [Fact]
    public async Task Fills_the_listings_own_financial_terms()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        result.FilledHtml.Should().Contain("$3,600.00");
        result.FilledHtml.Should().Contain("5%");
        result.FilledHtml.Should().Contain("$180.00");
        result.FilledHtml.Should().Contain("first day");
    }

    [Fact]
    public async Task Blanks_party_names_and_dates()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        result.Values["tenant.fullName"].Should().Be("__________");
        result.Values["host.fullName"].Should().Be("__________");
        result.Values["lease.effectiveDate"].Should().Be("__________");
        result.Values["deal.startDate"].Should().Be("__________");
    }

    [Fact]
    public async Task Discloses_no_more_of_the_address_than_the_public_listing_page()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        // City and state are already public; street and ZIP are not.
        result.FilledHtml.Should().NotContain("8917 Wakefield Ave");
        result.FilledHtml.Should().NotContain("91402");
        result.FilledHtml.Should().Contain("Panorama City");
    }

    [Fact]
    public async Task Keeps_clauses_that_depend_on_values_a_preview_cannot_know()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        // The tenant's name is blank, but the clause it guards is still part of
        // the lease they would sign, so it must not be stripped.
        result.FilledHtml.Should().Contain("TENANT CLAUSE");
    }

    [Fact]
    public async Task Honours_listing_level_conditions()
    {
        var withPets = await CreateFiller(CreateListing(petsAllowed: true))
            .FillPreviewForListingAsync(ListingId);
        var withoutPets = await CreateFiller(CreateListing(petsAllowed: false))
            .FillPreviewForListingAsync(ListingId);

        withPets.FilledHtml.Should().Contain("PETS CLAUSE");
        withoutPets.FilledHtml.Should().NotContain("PETS CLAUSE");
    }

    [Fact]
    public async Task Drops_broker_and_owner_clauses_the_listing_knows_do_not_apply()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        result.FilledHtml.Should().NotContain("BROKER CLAUSE");
        result.FilledHtml.Should().NotContain("OWNER CLAUSE");
    }

    [Fact]
    public async Task Keeps_broker_and_owner_clauses_when_the_listing_uses_them()
    {
        var listing = CreateListing(includeBrokerClause: true, homeOwnerUserId: Guid.NewGuid());

        var result = await CreateFiller(listing).FillPreviewForListingAsync(ListingId);

        result.FilledHtml.Should().Contain("BROKER CLAUSE");
        result.FilledHtml.Should().Contain("OWNER CLAUSE");
    }

    [Fact]
    public async Task Reports_no_missing_required_placeholders()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        // A preview is blank by design; the deal-time required check must not
        // fire and block it.
        result.MissingRequiredPlaceholders.Should().BeEmpty();
    }

    [Fact]
    public async Task Covers_every_placeholder_in_the_catalog()
    {
        var result = await CreateFiller(CreateListing()).FillPreviewForListingAsync(ListingId);

        var uncovered = LeasePlaceholderCatalog.AllKeys
            .Where(key => !result.Values.ContainsKey(key))
            .ToList();

        uncovered.Should().BeEmpty();
    }

    private sealed class StubTemplateProvider(string bodyHtml) : ILeaseAgreementTemplateProvider
    {
        public Task<LeaseAgreementTemplateInfo?> GetActiveTemplateAsync(
            string jurisdictionCode,
            CancellationToken ct = default) =>
            Task.FromResult<LeaseAgreementTemplateInfo?>(new LeaseAgreementTemplateInfo(
                Guid.NewGuid(),
                jurisdictionCode,
                "Residential Lease Agreement",
                Guid.NewGuid(),
                3,
                null,
                bodyHtml));
    }

    private sealed class StubListingProvider(ListingDetailsDto listing) : IListingProvider
    {
        public Task<ListingDetailsDto?> GetListingDetailsAsync(Guid listingId, CancellationToken ct = default) =>
            Task.FromResult<ListingDetailsDto?>(listingId == listing.Id ? listing : null);

        public Task<bool> IsAvailableAsync(Guid listingId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task BlockDatesForDealAsync(Guid listingId, Guid dealId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ListingSummaryInfoDto>> GetListingSummariesAsync(IReadOnlyList<Guid> listingIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ListingSummaryInfoDto>>([]);

        public Task<IReadOnlyList<Guid>> GetListingIdsForLandlordAsync(Guid landlordUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyDictionary<Guid, Guid>> GetLandlordIdsForListingsAsync(IReadOnlyList<Guid> listingIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid>>(new Dictionary<Guid, Guid>());
    }

    private sealed class StubDealProvider : IDealApplicationStatusProvider
    {
        public Task<bool> IsApprovedAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<DealParticipantsDto?> GetParticipantsAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult<DealParticipantsDto?>(null);

        public Task<DateOnly?> GetRequestedCheckOutAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult<DateOnly?>(null);

        public Task<DealApplicationDetailsDto?> GetDealDetailsAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult<DealApplicationDetailsDto?>(null);
    }

    private sealed class StubPartyProfileProvider : ILeasePartyProfileProvider
    {
        public Task<LeasePartyProfileDto?> GetAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<LeasePartyProfileDto?>(null);
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc);
    }
}
