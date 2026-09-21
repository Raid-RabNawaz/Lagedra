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

public class LeaseAgreementFillerDealTests
{
    private static readonly Guid DealId = Guid.NewGuid();
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private const string BodyHtml =
        """
        <p>{{host.fullName}} / {{host.phone}} / {{host.email}} / {{host.mailingAddress}}</p>
        <p>{{tenant.fullName}} / {{tenant.phone}} / {{tenant.email}}</p>
        <p>{{listing.fullAddress}} {{deal.monthlyRent}} {{deal.securityDeposit}}</p>
        """;

    [Fact]
    public async Task FillForDeal_does_not_block_on_missing_party_phone_or_mailing_address()
    {
        var filler = CreateFiller(CreateListing());

        var result = await filler.FillForDealAsync(DealId);

        result.MissingRequiredPlaceholders.Should().BeEmpty();
        result.Values["host.phone"].Should().Be("__________");
        result.Values["host.mailingAddress"].Should().Be("__________");
        result.Values["tenant.phone"].Should().Be("__________");
        result.FilledHtml.Should().Contain("Jane Host");
        result.FilledHtml.Should().Contain("8917 Wakefield Ave");
    }

    [Fact]
    public async Task FillForDeal_still_requires_the_locked_property_address()
    {
        var listing = CreateListing() with { PreciseAddress = null };
        var filler = CreateFiller(listing);

        var result = await filler.FillForDealAsync(DealId);

        result.MissingRequiredPlaceholders.Should().Contain("listing.fullAddress");
    }

    private static LeaseAgreementFiller CreateFiller(ListingDetailsDto listing) =>
        new(
            new StubDealProvider(),
            new StubListingProvider(listing),
            new StubPartyProfileProvider(),
            new StubTemplateProvider(BodyHtml),
            new StubClock());

    private static ListingDetailsDto CreateListing() =>
        new(
            Id: ListingId,
            LandlordUserId: HostId,
            MinStayDays: 30,
            MaxStayDays: 365,
            MaxDepositCents: 400_000,
            MonthlyRentCents: 360_000,
            JurisdictionCode: "US-CA",
            Title: "Sunny two-bedroom",
            PropertyType: "Apartment",
            PreciseAddress: new ListingAddressDto(
                "8917 Wakefield Ave", "Panorama City", "CA", "91402", "US"),
            DefaultDepositCents: 360_000,
            LeaseAgreementSource: "LagedraTemplate");

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

        public Task ReplaceHostBlockedDatesAsync(
            Guid listingId,
            IReadOnlyList<ListingDateRangeDto> blockedRanges,
            CancellationToken ct = default) =>
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
            Task.FromResult(dealId == DealId);

        public Task<DealParticipantsDto?> GetParticipantsAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult<DealParticipantsDto?>(
                dealId == DealId ? new DealParticipantsDto(HostId, TenantId, ListingId) : null);

        public Task<DateOnly?> GetRequestedCheckOutAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult<DateOnly?>(dealId == DealId ? new DateOnly(2027, 9, 30) : null);

        public Task<DealApplicationDetailsDto?> GetDealDetailsAsync(Guid dealId, CancellationToken ct = default) =>
            Task.FromResult<DealApplicationDetailsDto?>(dealId == DealId
                ? new DealApplicationDetailsDto(
                    Guid.NewGuid(),
                    DealId,
                    ListingId,
                    TenantId,
                    HostId,
                    new DateOnly(2026, 10, 1),
                    new DateOnly(2027, 9, 30),
                    365,
                    360_000,
                    360_000,
                    0,
                    null,
                    2)
                : null);
    }

    private sealed class StubPartyProfileProvider : ILeasePartyProfileProvider
    {
        public Task<LeasePartyProfileDto?> GetAsync(Guid userId, CancellationToken ct = default)
        {
            if (userId == HostId)
            {
                return Task.FromResult<LeasePartyProfileDto?>(new LeasePartyProfileDto(
                    HostId,
                    "Jane Host",
                    "host@example.com",
                    Phone: null,
                    MailingAddress: null,
                    NoticeAddress: null,
                    BrokerName: null,
                    BrokerDreLicense: null,
                    BrokerScopeNotes: null));
            }

            if (userId == TenantId)
            {
                return Task.FromResult<LeasePartyProfileDto?>(new LeasePartyProfileDto(
                    TenantId,
                    "Jordan Tenant",
                    "tenant@example.com",
                    Phone: null,
                    MailingAddress: null,
                    NoticeAddress: null,
                    BrokerName: null,
                    BrokerDreLicense: null,
                    BrokerScopeNotes: null));
            }

            return Task.FromResult<LeasePartyProfileDto?>(null);
        }
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc);
    }
}
