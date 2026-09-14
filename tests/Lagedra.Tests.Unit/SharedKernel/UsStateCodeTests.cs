using FluentAssertions;
using Lagedra.SharedKernel.Integration.Screening;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class UsStateCodeTests
{
    [Theory]
    [InlineData("CA", "CA")]
    [InlineData("ca", "CA")]
    [InlineData("California", "CA")]
    [InlineData("Calif.", "CA")]
    [InlineData("New York", "NY")]
    [InlineData("NY", "NY")]
    [InlineData("Washington, D.C.", "DC")]
    [InlineData("District of Columbia", "DC")]
    public void Maps_names_and_codes(string raw, string expected)
    {
        UsStateCode.TryNormalize(raw, out var code).Should().BeTrue();
        code.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Californication")]
    [InlineData("XX")]
    public void Rejects_unknown_values(string? raw)
    {
        UsStateCode.TryNormalize(raw, out _).Should().BeFalse();
    }
}
