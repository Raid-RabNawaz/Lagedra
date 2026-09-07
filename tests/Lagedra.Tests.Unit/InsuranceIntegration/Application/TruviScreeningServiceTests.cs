using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lagedra.Tests.Unit.InsuranceIntegration.Application;

public class TruviScreeningServiceTests
{
    private static readonly Guid DealId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid TenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid LandlordId = Guid.Parse("66666666-7777-8888-9999-000000000000");
    private static readonly Guid ListingId = Guid.Parse("99999999-8888-7777-6666-555555555555");
    private static readonly DateTime Now = new(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Skips_when_already_screened()
    {
        var store = new FakeStore();
        var existing = InsurancePolicyRecord.Create(TenantId, DealId);
        existing.RecordScreeningResult("ver_existing", TruviScreeningStatus.Approved, null, Now.AddDays(30));
        store.Items.Add(existing);

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        var sut = CreateSut(store, client);

        await sut.RequestForDealAsync(DealId, CancellationToken.None);

        await client.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task Persists_failed_when_check_in_is_past()
    {
        var store = new FakeStore();
        var deals = Substitute.For<IDealApplicationStatusProvider>();
        deals.GetDealDetailsAsync(DealId, Arg.Any<CancellationToken>())
            .Returns(Deal(checkIn: new DateOnly(2026, 8, 1)));

        var sut = CreateSut(store, Substitute.For<ITruviScreenAndProtectClient>(), deals);

        await sut.RequestForDealAsync(DealId, CancellationToken.None);

        store.Items.Should().ContainSingle();
        store.Items[0].ScreeningStatus.Should().Be(TruviScreeningStatus.Failed);
        store.Items[0].State.Should().Be(InsuranceState.Unknown);
    }

    [Fact]
    public async Task Screens_same_day_check_in()
    {
        var store = new FakeStore();
        var deals = Substitute.For<IDealApplicationStatusProvider>();
        deals.GetDealDetailsAsync(DealId, Arg.Any<CancellationToken>())
            .Returns(Deal(checkIn: DateOnly.FromDateTime(Now)));

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        client.CreateAsync(Arg.Any<TruviCreateVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TruviVerificationResult("ver_today", TruviScreeningStatus.Approved, null));

        var sut = CreateSut(store, client, deals);

        await sut.RequestForDealAsync(DealId, CancellationToken.None);

        await client.Received(1).CreateAsync(Arg.Any<TruviCreateVerificationRequest>(), Arg.Any<CancellationToken>());
        store.Items[0].ScreeningStatus.Should().Be(TruviScreeningStatus.Approved);
    }

    [Fact]
    public async Task Sends_host_company_and_deposit_starting_level()
    {
        var store = new FakeStore();
        TruviCreateVerificationRequest? sent = null;
        var client = Substitute.For<ITruviScreenAndProtectClient>();
        client.CreateAsync(Arg.Any<TruviCreateVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<TruviCreateVerificationRequest>();
                return new TruviVerificationResult("ver_1", TruviScreeningStatus.Approved, null);
            });

        var sut = CreateSut(store, client);

        await sut.RequestForDealAsync(DealId, CancellationToken.None);

        sent.Should().NotBeNull();
        sent!.Company.Name.Should().Be("Harbor Stays");
        sent.Company.Email.Should().Be("host@example.com");
        sent.Protection.StartingLevel.Should().Be(1000);
        store.Items[0].PolicyNumber.Should().Be(DealId.ToString("D"));
    }

    [Fact]
    public async Task Persists_failed_when_Truvi_throws()
    {
        var store = new FakeStore();
        var client = Substitute.For<ITruviScreenAndProtectClient>();
        client.CreateAsync(Arg.Any<TruviCreateVerificationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TruviScreenAndProtectException("sandbox down") { Status = 503 });

        var sut = CreateSut(store, client);

        await sut.RequestForDealAsync(DealId, CancellationToken.None);

        store.Items.Should().ContainSingle();
        store.Items[0].ScreeningStatus.Should().Be(TruviScreeningStatus.Failed);
        store.Items[0].HasExternalVerification.Should().BeFalse();
    }

    [Fact]
    public async Task Cancel_calls_Truvi_when_verification_id_exists()
    {
        var store = new FakeStore();
        var existing = InsurancePolicyRecord.Create(TenantId, DealId);
        existing.RecordScreeningResult(
            "ver_1",
            TruviScreeningStatus.Approved,
            null,
            Now.AddDays(30),
            DealId.ToString("D"));
        store.Items.Add(existing);

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        var sut = CreateSut(store, client);

        await sut.CancelForDealAsync(DealId, "guest cancelled", CancellationToken.None);

        await client.Received(1).CancelAsync(
            Arg.Is<TruviCancelVerificationRequest>(r =>
                r.Verification.VerificationId == "ver_1"
                && r.Reservation.ReservationId == DealId.ToString("D")),
            Arg.Any<CancellationToken>());
        store.Items[0].ScreeningStatus.Should().Be(TruviScreeningStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_skips_Truvi_after_check_in_has_started()
    {
        var store = new FakeStore();
        var existing = InsurancePolicyRecord.Create(TenantId, DealId);
        existing.RecordScreeningResult("ver_1", TruviScreeningStatus.Approved, null, Now.AddDays(30));
        store.Items.Add(existing);

        var deals = Substitute.For<IDealApplicationStatusProvider>();
        deals.GetDealDetailsAsync(DealId, Arg.Any<CancellationToken>())
            .Returns(Deal(checkIn: DateOnly.FromDateTime(Now)));

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        var sut = CreateSut(store, client, deals);

        await sut.CancelForDealAsync(DealId, "guest cancelled", CancellationToken.None);

        await client.DidNotReceiveWithAnyArgs().CancelAsync(default!, default);
        store.Items[0].ScreeningStatus.Should().Be(TruviScreeningStatus.Cancelled);
    }

    [Fact]
    public async Task Modify_sends_current_deal_dates()
    {
        var store = new FakeStore();
        var existing = InsurancePolicyRecord.Create(TenantId, DealId);
        existing.RecordScreeningResult(
            "ver_1",
            TruviScreeningStatus.Approved,
            null,
            Now.AddDays(30),
            DealId.ToString("D"));
        store.Items.Add(existing);

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        var sut = CreateSut(store, client);

        var result = await sut.ModifyForDealAsync(DealId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await client.Received(1).ModifyAsync(
            Arg.Is<TruviModifyVerificationRequest>(r =>
                r.Verification.VerificationId == "ver_1"
                && r.Reservation.CheckOut == "2026-11-14"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rescreen_posts_new_create_only_when_flagged()
    {
        var store = new FakeStore();
        var existing = InsurancePolicyRecord.Create(TenantId, DealId);
        existing.RecordScreeningResult("ver_old", TruviScreeningStatus.Flagged, "email", Now.AddDays(30));
        store.Items.Add(existing);

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        client.CreateAsync(Arg.Any<TruviCreateVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TruviVerificationResult("ver_new", TruviScreeningStatus.Approved, null));

        var sut = CreateSut(store, client);

        var result = await sut.RescreenForDealAsync(DealId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await client.Received(1).CreateAsync(
            Arg.Is<TruviCreateVerificationRequest>(r =>
                r.Reservation.ReservationId != DealId.ToString("D")
                && r.Reservation.ReservationId.StartsWith(DealId.ToString("D"), StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
        store.Items[0].ExternalVerificationId.Should().Be("ver_new");
        store.Items[0].ScreeningStatus.Should().Be(TruviScreeningStatus.Approved);
    }

    [Fact]
    public async Task Rescreen_rejects_approved_screenings()
    {
        var store = new FakeStore();
        var existing = InsurancePolicyRecord.Create(TenantId, DealId);
        existing.RecordScreeningResult("ver_1", TruviScreeningStatus.Approved, null, Now.AddDays(30));
        store.Items.Add(existing);

        var client = Substitute.For<ITruviScreenAndProtectClient>();
        var sut = CreateSut(store, client);

        var result = await sut.RescreenForDealAsync(DealId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Insurance.NotFlagged");
        await client.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    private static TruviScreeningService CreateSut(
        FakeStore store,
        ITruviScreenAndProtectClient client,
        IDealApplicationStatusProvider? deals = null)
    {
        if (deals is null)
        {
            deals = Substitute.For<IDealApplicationStatusProvider>();
            deals.GetDealDetailsAsync(DealId, Arg.Any<CancellationToken>()).Returns(Deal());
        }

        var listings = Substitute.For<IListingProvider>();
        listings.GetListingDetailsAsync(ListingId, Arg.Any<CancellationToken>()).Returns(new ListingDetailsDto(
            ListingId,
            LandlordId,
            30,
            180,
            500_000,
            275_000,
            null,
            PreciseAddress: new ListingAddressDto("12 Main St", "Los Angeles", "CA", "90012", "United States"),
            HouseRules: new ListingHouseRulesDto("15:00", "11:00", 4, false, null, false, false, null, null, null, null),
            Bedrooms: 1,
            Bathrooms: 1m));

        var parties = Substitute.For<ILeasePartyProfileProvider>();
        parties.GetAsync(TenantId, Arg.Any<CancellationToken>()).Returns(new LeasePartyProfileDto(
            TenantId,
            "Ada Lovelace",
            "ada@example.com",
            "+15555550100",
            null,
            null,
            null,
            null,
            null,
            "Ada",
            "Lovelace"));
        parties.GetAsync(LandlordId, Arg.Any<CancellationToken>()).Returns(new LeasePartyProfileDto(
            LandlordId,
            "Ada Host",
            "host@example.com",
            "+15555550999",
            null,
            null,
            null,
            null,
            null,
            "Ada",
            "Host",
            "Harbor Stays"));

        return new TruviScreeningService(
            client,
            store,
            deals,
            listings,
            parties,
            new FixedClock(Now),
            Options.Create(new TruviScreenAndProtectSettings
            {
                ScreeningEnabled = true,
                SubscriptionKey = "test-key",
            }),
            NullLogger<TruviScreeningService>.Instance);
    }

    private static DealApplicationDetailsDto Deal(DateOnly? checkIn = null)
        => new(
            Guid.NewGuid(),
            DealId,
            ListingId,
            TenantId,
            LandlordId,
            checkIn ?? new DateOnly(2026, 10, 15),
            new DateOnly(2026, 11, 14),
            30,
            null,
            200_000,
            null,
            null,
            2);

    private sealed class FakeStore : IInsurancePolicyRecordStore
    {
        public List<InsurancePolicyRecord> Items { get; } = [];

        public Task<InsurancePolicyRecord?> GetByDealIdAsync(
            Guid dealId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(r => r.DealId == dealId));

        public void Add(InsurancePolicyRecord record) => Items.Add(record);

        public void AddAttempt(InsurancePolicyRecord record, InsuranceVerificationAttempt attempt)
            => record.AddAttempt(attempt);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }
}
