using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Lagedra.SharedKernel.Insurance;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class TruviVerificationRequestFactoryTests
{
    private static readonly Guid DealId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly DateTime UtcNow = new(2026, 9, 4, 12, 30, 45, 120, DateTimeKind.Utc);

    [Theory]
    [InlineData("US", "USA")]
    [InlineData("United States", "USA")]
    [InlineData("usa", "USA")]
    [InlineData("GB", "GBR")]
    [InlineData("Canada", "CAN")]
    public void Maps_common_country_aliases_to_alpha3(string input, string expected)
    {
        TruviVerificationRequestFactory.ToCountryIso(input).Should().Be(expected);
    }

    [Fact]
    public void Strips_digits_from_guest_names()
    {
        var (first, last) = TruviVerificationRequestFactory.ResolveNames("Ann3", "O4Brien", "Ann3 O4Brien");
        first.Should().Be("Ann");
        last.Should().Be("OBrien");
    }

    [Fact]
    public void Timestamp_and_echo_token_match_Truvi_rules()
    {
        TruviVerificationRequestFactory.FormatTimestamp(UtcNow)
            .Should().Be("2026-09-04T12:30:45.12");
        TruviVerificationRequestFactory.EchoTokenForDeal(DealId)
            .Should().HaveLength(36);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(24_900, null)]
    [InlineData(200_000, 1000)]
    [InlineData(500_000, 5000)]
    [InlineData(2_000_000, 10_000)]
    public void Maps_deposit_to_largest_startingLevel_at_or_below_deposit(
        int? depositCents,
        int? expected)
    {
        TruviVerificationRequestFactory.StartingLevelForDepositCents(depositCents)
            .Should().Be(expected);
    }

    [Fact]
    public void Resolves_company_from_host_or_pm_not_platform()
    {
        TruviVerificationRequestFactory.TryResolveCompany(
            "Harbor Stays",
            "Ada Host",
            "host@example.com",
            out var name,
            out var email,
            out var error).Should().BeTrue();

        error.Should().BeNull();
        name.Should().Be("Harbor Stays");
        email.Should().Be("host@example.com");
    }

    [Fact]
    public void Create_omits_startingLevel_without_deposit_and_isPerStay()
    {
        TruviVerificationRequestFactory.TryCreate(
            DealId,
            UtcNow,
            "Harbor Stays",
            "host@example.com",
            50_000,
            "12 Main St",
            "Los Angeles",
            "90012",
            "United States",
            petsAllowed: false,
            guestCount: 2,
            bedrooms: 1,
            bathrooms: 1m,
            checkIn: new DateOnly(2026, 10, 15),
            checkOut: new DateOnly(2026, 11, 14),
            firstName: "Ada",
            lastName: "Lovelace",
            fullName: "Ada Lovelace",
            email: "ada@example.com",
            phone: "+15555550100",
            out var request,
            out var error).Should().BeTrue();

        error.Should().BeNull();
        request.Should().NotBeNull();
        request!.Protection.Type.Should().Be(TruviVerificationRequestFactory.CompleteProtection);
        request.Protection.ExtendedAmount.Should().Be(50_000);
        request.Protection.HasPetProtection.Should().BeFalse();
        request.Reservation.Channel.Should().Be(TruviVerificationRequestFactory.DirectWebChannel);
        request.Listing.Address.CountryIso.Should().Be("USA");

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
        json.Should().NotContain("startingLevel");
        json.Should().NotContain("isPerStay");
        json.Should().Contain("\"hasPetProtection\":false");
    }

    [Fact]
    public void Create_sends_startingLevel_1000_for_2000_deposit()
    {
        TruviVerificationRequestFactory.TryCreate(
            DealId,
            UtcNow,
            "Harbor Stays",
            "host@example.com",
            50_000,
            "12 Main St",
            "Los Angeles",
            "90012",
            "United States",
            false,
            2,
            1,
            1m,
            new DateOnly(2026, 10, 15),
            new DateOnly(2026, 11, 14),
            "Ada",
            "Lovelace",
            "Ada Lovelace",
            "ada@example.com",
            "+15555550100",
            200_000,
            null,
            out var request,
            out var error).Should().BeTrue();

        error.Should().BeNull();
        request!.Protection.StartingLevel.Should().Be(1000);
    }

    [Fact]
    public void Create_fails_without_guest_email()
    {
        TruviVerificationRequestFactory.TryCreate(
            DealId,
            UtcNow,
            "Lagedra",
            "raid@lagedra.com",
            50_000,
            "12 Main St",
            "Los Angeles",
            "90012",
            "USA",
            false,
            1,
            1,
            1m,
            new DateOnly(2026, 10, 15),
            new DateOnly(2026, 11, 14),
            "Ada",
            "Lovelace",
            "Ada Lovelace",
            email: null,
            phone: null,
            out var request,
            out var error).Should().BeFalse();

        request.Should().BeNull();
        error.Should().Contain("email");
    }
}
