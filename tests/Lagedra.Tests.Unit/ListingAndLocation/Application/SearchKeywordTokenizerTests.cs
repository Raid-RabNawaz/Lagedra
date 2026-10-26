using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Application.Queries;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Application;

public class SearchKeywordTokenizerTests
{
    [Fact]
    public void Splits_a_city_and_state_so_each_part_can_match_address_fields()
    {
        SearchKeywordTokenizer.Tokenize("Austin, TX")
            .Should().BeEquivalentTo("Austin", "TX");
    }

    [Fact]
    public void Keeps_a_single_word_keyword()
    {
        SearchKeywordTokenizer.Tokenize("furnished")
            .Should().Equal("furnished");
    }

    [Fact]
    public void Returns_empty_for_blank_input()
    {
        SearchKeywordTokenizer.Tokenize("   ").Should().BeEmpty();
        SearchKeywordTokenizer.Tokenize(null).Should().BeEmpty();
    }

    [Theory]
    [InlineData("90210", "90210")]
    [InlineData("90210-1234", "90210")]
    [InlineData("M5V 2T6", "M5V2T6")]
    [InlineData("m5v2t6", "M5V2T6")]
    [InlineData("Los Angeles, CA", null)]
    public void Normalizes_postal_codes(string? input, string? expected)
    {
        SearchKeywordTokenizer.NormalizePostalCode(input).Should().Be(expected);
    }
}
