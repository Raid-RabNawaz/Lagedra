using System;
using System.Linq;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Infrastructure;

public sealed class OpenGraphJsonLdExtractorTests
{
    private readonly OpenGraphJsonLdExtractor _extractor = new();
    private static readonly Uri SampleUrl = new("https://example-rentals.test/listings/sunny-loft");

    [Fact]
    public void Extract_OpenGraphAndJsonLd_PopulatesAllSupportedFields()
    {
        var html = ListingImportFixtures.Load("og-jsonld-apartment.html");

        var draft = _extractor.Extract(html, SampleUrl);

        // og:title beats the <title> fallback.
        draft.Title.Should().Be("Sunny Downtown Loft");
        draft.Description.Should().Contain("bright, modern loft");
        draft.PropertyType.Should().Be("Apartment");
        draft.Bedrooms.Should().Be(2);
        draft.Bathrooms.Should().Be(1.5m);
        draft.SquareFootage.Should().Be(950);
        draft.MaxGuests.Should().Be(4);
        draft.CheckInTime.Should().Be("15:00");
        draft.CheckOutTime.Should().Be("11:00");
        draft.NightlyRateCents.Should().Be(18000);
        draft.MonthlyRentCents.Should().BeNull();
        draft.Currency.Should().Be("USD");
    }

    [Fact]
    public void Extract_Address_KeepsCityRegionCountryOnly()
    {
        var html = ListingImportFixtures.Load("og-jsonld-apartment.html");

        var draft = _extractor.Extract(html, SampleUrl);

        draft.ApproxAddress.Should().Be("Austin, TX, US");
        // Precise street/postal must never leak through.
        draft.ApproxAddress.Should().NotContain("742");
        draft.ApproxAddress.Should().NotContain("78701");
    }

    [Fact]
    public void Extract_Amenities_OnlyIncludesPresentFeaturesAndDeduplicates()
    {
        var html = ListingImportFixtures.Load("og-jsonld-apartment.html");

        var draft = _extractor.Extract(html, SampleUrl);

        draft.AmenityHints.Should().NotBeNull();
        draft.AmenityHints!.Should().Contain("Wifi");
        draft.AmenityHints!.Should().Contain("Kitchen");
        // "Pool" has value:false and must be excluded.
        draft.AmenityHints!.Should().NotContain("Pool");
    }

    [Fact]
    public void Extract_Photos_MergesOgAndJsonLdAndDeduplicates()
    {
        var html = ListingImportFixtures.Load("og-jsonld-apartment.html");

        var draft = _extractor.Extract(html, SampleUrl);

        draft.Photos.Should().NotBeNull();
        var urls = draft.Photos!.Select(p => p.Url).ToList();
        urls.Should().Contain("https://cdn.example-rentals.test/photos/loft-1.jpg");
        urls.Should().Contain("https://cdn.example-rentals.test/photos/loft-2.jpg");
        urls.Should().Contain("https://cdn.example-rentals.test/photos/loft-3.jpg");
        urls.Should().OnlyHaveUniqueItems();

        var first = draft.Photos!.First(p => p.Url.EndsWith("loft-1.jpg", StringComparison.Ordinal));
        first.AltText.Should().Be("Living room with large windows");
        first.Width.Should().Be(1200);
        first.Height.Should().Be(800);
    }

    [Fact]
    public void Extract_Provenance_UsesCanonicalUrlAndBareHost()
    {
        var html = ListingImportFixtures.Load("og-jsonld-apartment.html");

        var draft = _extractor.Extract(html, SampleUrl);

        draft.SourceHost.Should().Be("example-rentals.test");
        draft.SourceUrl.Should().Be("https://example-rentals.test/listings/sunny-loft");
    }

    [Fact]
    public void Extract_OpenGraphOnly_FallsBackToOgTypeAndLeavesStructuredFieldsNull()
    {
        var html = ListingImportFixtures.Load("og-only.html");

        var draft = _extractor.Extract(html, new Uri("https://cabins.test/cozy-forest-cabin"));

        draft.Title.Should().Be("Cozy Forest Cabin");
        draft.Description.Should().Contain("quiet cabin");
        draft.PropertyType.Should().Be("website");
        draft.Bedrooms.Should().BeNull();
        draft.Bathrooms.Should().BeNull();
        draft.Photos.Should().NotBeNull();
        draft.Photos!.Should().ContainSingle(p => p.Url == "https://cabins.test/img/cabin.jpg");
    }

    [Fact]
    public void Extract_MonthlyPriceSpecification_MapsToMonthlyRentNotNightly()
    {
        var html = ListingImportFixtures.Load("jsonld-house-monthly.html");

        var draft = _extractor.Extract(html, new Uri("https://homes.test/spacious-family-house"));

        draft.PropertyType.Should().Be("House");
        draft.Bedrooms.Should().Be(4);
        draft.Bathrooms.Should().Be(2m);
        draft.ApproxAddress.Should().Be("Portland, OR");
        draft.MonthlyRentCents.Should().Be(320000);
        draft.NightlyRateCents.Should().BeNull();
    }

    [Fact]
    public void Extract_NoMetadata_ReturnsMostlyEmptyDraftWithProvenance()
    {
        var html = ListingImportFixtures.Load("empty.html");
        var url = new Uri("https://blank.test/page");

        var draft = _extractor.Extract(html, url);

        draft.Title.Should().BeNull();
        draft.Description.Should().BeNull();
        draft.Bedrooms.Should().BeNull();
        draft.Photos.Should().BeNull();
        draft.AmenityHints.Should().BeNull();
        draft.SourceHost.Should().Be("blank.test");
    }

    [Fact]
    public void Extract_TitleCounts_FillBedroomsBathroomsGuestsWhenStructuredDataMissing()
    {
        // Airbnb-style title/subtitle with no JSON-LD numbers (the common case for
        // platforms that block crawlers but still emit a descriptive og:title).
        const string html =
            "<html><head>" +
            "<meta property=\"og:title\" content=\"Tiny home in Chapel Hill \u00b7 4 guests \u00b7 1 bedroom \u00b7 1 bed \u00b7 1 private bath\" />" +
            "<meta property=\"og:description\" content=\"Chic Modern Tiny House Nestled in the Trees\" />" +
            "</head><body></body></html>";

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/26421553"));

        draft.Bedrooms.Should().Be(1);
        draft.Bathrooms.Should().Be(1m);
        draft.MaxGuests.Should().Be(4);
    }

    [Fact]
    public void Extract_StudioTitle_MapsToZeroBedrooms()
    {
        const string html =
            "<html><head>" +
            "<meta property=\"og:title\" content=\"Studio in Berlin \u00b7 2 guests \u00b7 Studio \u00b7 1 bed \u00b7 1.5 baths\" />" +
            "</head><body></body></html>";

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/1"));

        draft.Bedrooms.Should().Be(0);
        draft.Bathrooms.Should().Be(1.5m);
        draft.MaxGuests.Should().Be(2);
    }

    [Fact]
    public void Extract_StructuredCounts_TakePrecedenceOverTitleText()
    {
        // JSON-LD says 3 bedrooms / 2 baths; the title text says 1/1. The
        // structured value must win — the title is only a gap filler.
        const string html =
            "<html><head>" +
            "<meta property=\"og:title\" content=\"Loft \u00b7 1 bedroom \u00b7 1 bath\" />" +
            "<script type=\"application/ld+json\">" +
            "{\"@context\":\"https://schema.org\",\"@type\":\"Apartment\",\"name\":\"Loft\"," +
            "\"numberOfBedrooms\":3,\"numberOfBathroomsTotal\":2}" +
            "</script>" +
            "</head><body></body></html>";

        var draft = _extractor.Extract(html, new Uri("https://x.test/loft"));

        draft.Bedrooms.Should().Be(3);
        draft.Bathrooms.Should().Be(2m);
    }

    [Fact]
    public void Extract_NoisyTitle_StripsRatingAndSpecSegments()
    {
        // A non-Airbnb host still benefits from the generic title cleanup that
        // drops "★rating" and "· N bedroom · N bath" specification tails.
        const string html =
            "<html><head>" +
            "<meta property=\"og:title\" content=\"Lakeside Cabin \u00b7 \u26054.9 \u00b7 2 bedrooms \u00b7 1.5 baths\" />" +
            "</head><body></body></html>";

        var draft = _extractor.Extract(html, new Uri("https://lakecabins.test/lakeside"));

        draft.Title.Should().Be("Lakeside Cabin");
    }

    [Fact]
    public void Extract_AirbnbState_UsesNameDescriptionAmenitiesAndGallery()
    {
        var html = ListingImportFixtures.Load("airbnb-deferred-state.html");

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/26421553"));

        // Title is the host's real name (og:description), not the noisy og:title.
        draft.Title.Should().Be("Chic Modern Tiny House Nestled in the Trees");

        // Description comes from the embedded state's main htmlDescription, with
        // <br> turned into line breaks and HTML entities decoded; the shorter
        // neighborhood section must not win.
        draft.Description.Should().Contain("240 sq ft");
        draft.Description.Should().Contain("rest & reset");
        draft.Description.Should().NotContain("<br");
        draft.Description.Should().NotContain("Historic downtown");
    }

    [Fact]
    public void Extract_AirbnbState_AmenitiesExcludeUnavailable()
    {
        var html = ListingImportFixtures.Load("airbnb-deferred-state.html");

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/26421553"));

        draft.AmenityHints.Should().NotBeNull();
        draft.AmenityHints!.Should().Contain(["Wifi", "Hair dryer", "Shower gel", "Dishes and silverware"]);
        // "Pool" is available:false and must be excluded.
        draft.AmenityHints!.Should().NotContain("Pool");
    }

    [Fact]
    public void Extract_AirbnbState_PhotosAreListingScopedDedupedAndSized()
    {
        var html = ListingImportFixtures.Load("airbnb-deferred-state.html");

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/26421553"));

        draft.Photos.Should().NotBeNull();
        var urls = draft.Photos!.Select(p => p.Url).ToList();

        // Three distinct listing photos (the ?im_w=720 variant dedupes against the
        // original, and the platform-asset favicon is filtered out entirely).
        urls.Should().HaveCount(3);
        urls.Should().OnlyContain(u => u.Contains("Hosting-26421553", StringComparison.Ordinal));
        urls.Should().OnlyContain(u => u.EndsWith("?im_w=1200", StringComparison.Ordinal));
        urls.Should().NotContain(u => u.Contains("AirbnbPlatformAssets", StringComparison.Ordinal));
    }

    [Fact]
    public void Extract_AirbnbState_HouseRulesAndCheckTimes()
    {
        var html = ListingImportFixtures.Load("airbnb-deferred-state.html");

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/26421553"));

        // Check-in/out come from the house-rules list (Airbnb omits them from
        // structured metadata) and narrow no-break spaces must parse cleanly.
        draft.CheckInTime.Should().Be("14:00");
        draft.CheckOutTime.Should().Be("11:00");

        draft.PetsAllowed.Should().BeTrue();
        draft.SmokingAllowed.Should().BeFalse();
        draft.PartiesAllowed.Should().BeFalse();

        draft.QuietHoursStart.Should().Be("22:00");
        draft.QuietHoursEnd.Should().Be("09:00");

        draft.HouseRules.Should().Contain("water the plants");
        draft.HouseRules.Should().Contain("No loud music");
    }

    [Fact]
    public void Extract_AirbnbState_CancellationPolicyLabel()
    {
        var html = ListingImportFixtures.Load("airbnb-deferred-state.html");

        var draft = _extractor.Extract(html, new Uri("https://www.airbnb.com/rooms/26421553"));

        draft.CancellationPolicy.Should().Be("Moderate");
    }
}
