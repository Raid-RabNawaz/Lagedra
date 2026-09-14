using FluentAssertions;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Screening;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class ScreeningRequesterFactoryTests
{
    private static readonly Guid HostId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void Individual_host_uses_person_name_and_mailing_address()
    {
        var host = new LeasePartyProfileDto(
            HostId,
            "Ada Host",
            "host@example.com",
            "+14152345678",
            null,
            null,
            null,
            null,
            null,
            "Ada",
            "Host",
            null,
            "500 Market St",
            "San Francisco",
            "CA",
            "94105");

        var requester = ScreeningRequesterFactory.TryCreateFromHost(HostId, host);

        requester.Should().NotBeNull();
        requester!.ExternalId.Should().Be(HostId.ToString());
        requester.Type.Should().Be(ScreeningRequesterType.Individual);
        requester.Name.Should().Be("Ada Host");
        requester.Email.Should().Be("host@example.com");
        requester.PhoneNumber.Should().Be("+14152345678");
        requester.Street.Should().Be("500 Market St");
        requester.City.Should().Be("San Francisco");
        requester.State.Should().Be("CA");
        requester.Zipcode.Should().Be("94105");
    }

    [Fact]
    public void Company_host_is_a_business_requester()
    {
        var host = new LeasePartyProfileDto(
            HostId,
            "Ada Host",
            "ops@kimco.example",
            null,
            null,
            null,
            null,
            null,
            null,
            "Ada",
            "Host",
            "KimCo Property Management");

        var requester = ScreeningRequesterFactory.TryCreateFromHost(HostId, host);

        requester.Should().NotBeNull();
        requester!.Type.Should().Be(ScreeningRequesterType.Business);
        requester.Name.Should().Be("KimCo Property Management");
        requester.Email.Should().Be("ops@kimco.example");
    }

    [Theory]
    [InlineData("+1 (415) 234-5678", "+14152345678")]
    [InlineData("4152345678", "+14152345678")]
    [InlineData("+18005550100", null)]
    [InlineData("800-555-0100", null)]
    [InlineData("+1 (415) 555-0123", null)]
    [InlineData("+92 300 1234567", null)]
    [InlineData("555", null)]
    [InlineData(null, null)]
    public void Us_or_null_keeps_only_ten_digit_us_numbers(string? raw, string? expected)
    {
        ScreeningPhone.UsOrNull(raw).Should().Be(expected);
    }

    [Fact]
    public void Missing_email_or_name_returns_null()
    {
        ScreeningRequesterFactory.TryCreateFromHost(HostId, null).Should().BeNull();
        ScreeningRequesterFactory.TryCreateFromHost(
            HostId,
            new LeasePartyProfileDto(HostId, "Ada", null, null, null, null, null, null, null, "Ada", "Host"))
            .Should().BeNull();
        ScreeningRequesterFactory.TryCreateFromHost(
            HostId,
            new LeasePartyProfileDto(HostId, "host@example.com", "host@example.com", null, null, null, null, null, null))
            .Should().BeNull();
    }
}
