using FluentAssertions;
using Lagedra.SharedKernel.Integration;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class TenantVerificationTierMappingTests
{
    [Theory]
    [InlineData("Trusted", TenantVerificationTier.Trusted)]
    [InlineData("BackgroundVerified", TenantVerificationTier.Trusted)]
    [InlineData("Elite", TenantVerificationTier.Elite)]
    [InlineData("PartnerGuaranteed", TenantVerificationTier.Elite)]
    [InlineData("Verified", TenantVerificationTier.Verified)]
    [InlineData("Blocklisted", TenantVerificationTier.Blocklisted)]
    [InlineData("Unverified", TenantVerificationTier.Unverified)]
    [InlineData("", TenantVerificationTier.Unverified)]
    [InlineData(null, TenantVerificationTier.Unverified)]
    public void Parse_maps_legacy_and_current_names(string? value, TenantVerificationTier expected)
    {
        TenantVerificationTierMapping.Parse(value).Should().Be(expected);
    }

    [Fact]
    public void ToStorage_writes_current_names()
    {
        TenantVerificationTierMapping.ToStorage(TenantVerificationTier.Trusted)
            .Should().Be("Trusted");
        TenantVerificationTierMapping.ToStorage(TenantVerificationTier.Elite)
            .Should().Be("Elite");
    }
}
