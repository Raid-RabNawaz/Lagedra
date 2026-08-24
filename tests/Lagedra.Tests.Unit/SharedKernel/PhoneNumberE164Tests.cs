using FluentAssertions;
using Lagedra.SharedKernel.Sms;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class PhoneNumberE164Tests
{
    [Theory]
    // The three formats seen in production logs on Aug 10-11.
    [InlineData("7149105177", "+17149105177")]
    [InlineData("+1714 910 5177", "+17149105177")]
    [InlineData("(818) 305-6520", "+18183056520")]
    // Common US variants.
    [InlineData("818-305-6520", "+18183056520")]
    [InlineData("1 (818) 305-6520", "+18183056520")]
    [InlineData("818.305.6520", "+18183056520")]
    [InlineData("  +18183056520  ", "+18183056520")]
    // International.
    [InlineData("+44 20 7946 0958", "+442079460958")]
    [InlineData("+92 300 1234567", "+923001234567")]
    public void Normalizes_valid_numbers_to_e164(string input, string expected)
    {
        PhoneNumberE164.TryNormalize(input, out var normalized).Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a phone")]
    [InlineData("555-123")]           // too short
    [InlineData("+0123456789")]       // country codes never start with 0
    [InlineData("0149105177")]        // 10 digits but leading 0 is not a US area code
    [InlineData("1149105177")]        // 10 digits but leading 1 is not a US area code
    [InlineData("11049105177")]       // 11 digits, second digit 0 is invalid US
    [InlineData("+123456789012345678")] // exceeds E.164 max length
    [InlineData("818305652O")]        // letter O, not zero
    [InlineData("123456789")]         // 9 digits, no country hint
    public void Rejects_numbers_that_cannot_be_normalized(string? input)
    {
        PhoneNumberE164.TryNormalize(input, out var normalized).Should().BeFalse();
        normalized.Should().BeEmpty();
    }
}
