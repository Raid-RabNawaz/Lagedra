using FluentAssertions;
using Lagedra.Modules.Analytics.Application;
using Xunit;

namespace Lagedra.Tests.Unit.Analytics;

public class ListingAddedViaFormatterTests
{
    [Theory]
    [InlineData("hostaway", "Hostaway")]
    [InlineData("ownerrez", "OwnerRez")]
    [InlineData("guesty", "Guesty")]
    public void Channel_map_uses_provider_display_name(string providerKey, string expected)
    {
        ListingAddedViaFormatter.Format("Manual", null, providerKey).Should().Be(expected);
    }

    [Fact]
    public void Url_import_includes_source_host_when_present()
    {
        ListingAddedViaFormatter.Format("Url", "airbnb.com", null).Should().Be("URL (airbnb.com)");
        ListingAddedViaFormatter.Format("Url", null, null).Should().Be("URL");
    }

    [Theory]
    [InlineData("Excel", "Excel import")]
    [InlineData("Xml", "XML import")]
    [InlineData("Manual", "Manual")]
    [InlineData(null, "Manual")]
    public void File_and_manual_sources_have_stable_labels(string? addedVia, string expected)
    {
        ListingAddedViaFormatter.Format(addedVia, null, null).Should().Be(expected);
    }

    [Fact]
    public void Stored_channel_detail_is_used_when_no_map_row_exists()
    {
        ListingAddedViaFormatter.Format("Channel", "smoobu", null).Should().Be("Smoobu");
    }
}
