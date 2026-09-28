using System;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

public sealed class OwnerRezHaXmlFeedParserTests
{
    [Fact]
    public void ReadIndex_returns_active_listings_only()
    {
        const string xml = """
            <advertiserListingContentIndex>
              <advertiser><assignedId>ora7ccdcead6x</assignedId></advertiser>
              <advertiserListingContentIndexEntry>
                <listingExternalId>orpactive</listingExternalId>
                <listingUrl>https://faststage.ownerrez.com/haapi/haxml/ora7ccdcead6x/listing/orpactive</listingUrl>
              </advertiserListingContentIndexEntry>
              <advertiserListingContentIndexEntry>
                <listingExternalId>orpinactive</listingExternalId>
                <active>false</active>
              </advertiserListingContentIndexEntry>
            </advertiserListingContentIndex>
            """;

        var links = OwnerRezHaXmlFeedParser.ReadIndex(xml, "listingExternalId", "listingUrl");

        links.Should().ContainSingle();
        links[0].ExternalId.Should().Be("orpactive");
        links[0].ContentLocation.Should().Contain("/listing/orpactive");
    }

    [Fact]
    public void ParseListing_maps_content_rate_and_rooms()
    {
        const string listing = """
            <listing>
              <externalId>orp5b2abax</externalId>
              <active>true</active>
              <adContent>
                <propertyName><texts><text locale="en"><textValue>Beach house</textValue></text></texts></propertyName>
                <description><texts><text locale="en"><textValue>On the sand.</textValue></text></texts></description>
              </adContent>
              <location>
                <address>
                  <addressLine1>10 Main</addressLine1>
                  <city>Austin</city>
                  <stateOrProvince>TX</stateOrProvince>
                  <postalCode>78701</postalCode>
                  <country>US</country>
                </address>
                <geoCode><latDegrees>30.25</latDegrees><lngDegrees>-97.75</lngDegrees></geoCode>
              </location>
              <images>
                <image>
                  <externalId>img1</externalId>
                  <uri>https://cdn.example/a.jpg</uri>
                </image>
              </images>
              <units>
                <unit>
                  <externalId>orp5b2abax</externalId>
                  <propertyType>PROPERTY_TYPE_HOUSE</propertyType>
                  <bedrooms><bedroom><name>One</name></bedroom><bedroom><name>Two</name></bedroom></bedrooms>
                  <bathrooms>
                    <bathroom><roomSubType>FULL_BATH</roomSubType></bathroom>
                    <bathroom><roomSubType>HALF_BATH</roomSubType></bathroom>
                  </bathrooms>
                </unit>
              </units>
              <featureValues>
                <featureValue><unitFeatureName>KITCHEN</unitFeatureName></featureValue>
              </featureValues>
            </listing>
            """;

        const string rate = """
            <lodgingRateContent>
              <lodgingRate>
                <currency>USD</currency>
                <minimumStay>4</minimumStay>
                <nightlyRates>
                  <mon>100.00</mon>
                  <tue>200.00</tue>
                </nightlyRates>
              </lodgingRate>
            </lodgingRateContent>
            """;

        var snapshot = OwnerRezHaXmlFeedParser.ParseListing(listing, rate);

        snapshot.Should().NotBeNull();
        snapshot!.ExternalListingId.Should().Be("orp5b2abax");
        snapshot.Title.Should().Be("Beach house");
        snapshot.Description.Should().Be("On the sand.");
        snapshot.PropertyType.Should().Be("house");
        snapshot.Bedrooms.Should().Be(2);
        snapshot.Bathrooms.Should().Be(1.5m);
        snapshot.NightlyRateCents.Should().Be(15000);
        snapshot.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        snapshot.MinStayNights.Should().Be(4);
        snapshot.Currency.Should().Be("USD");
        snapshot.Address!.City.Should().Be("Austin");
        snapshot.Latitude.Should().Be(30.25);
        snapshot.Photos.Should().ContainSingle().Which.ExternalId.Should().Be("img1");
        snapshot.AmenityCodes.Should().Contain("KITCHEN");
    }

    [Fact]
    public void ParseListing_skips_inactive_listings()
    {
        const string listing = """
            <listing>
              <externalId>orp5b2abax</externalId>
              <active>false</active>
            </listing>
            """;

        OwnerRezHaXmlFeedParser.ParseListing(listing, rateXml: null).Should().BeNull();
    }

    [Fact]
    public void ParseAvailability_collapses_open_and_blocked_nights()
    {
        const string xml = """
            <unitAvailability>
              <dateRange>
                <beginDate>2026-10-01</beginDate>
                <endDate>2026-10-04</endDate>
              </dateRange>
              <unitAvailabilityConfiguration>
                <availability>YYNN</availability>
              </unitAvailabilityConfiguration>
            </unitAvailability>
            """;

        var calendar = OwnerRezHaXmlFeedParser.ParseAvailability("orp5b2abax", xml);

        calendar.Blocks.Should().HaveCount(2);
        calendar.Blocks[0].Available.Should().BeTrue();
        calendar.Blocks[0].Start.Should().Be(new DateOnly(2026, 10, 1));
        calendar.Blocks[0].End.Should().Be(new DateOnly(2026, 10, 2));
        calendar.Blocks[1].Available.Should().BeFalse();
        calendar.Blocks[1].Start.Should().Be(new DateOnly(2026, 10, 3));
    }

    [Theory]
    [InlineData("ora7ccdcead6x", true)]
    [InlineData("ORA7CCDCEAD6X", true)]
    [InlineData("host@example.com", false)]
    [InlineData("pt_token", false)]
    [InlineData("", false)]
    public void Advertiser_ids_match_connection_ids_only(string value, bool valid)
    {
        OwnerRezAdvertiserIds.IsValid(value).Should().Be(valid);
    }

    [Fact]
    public void Gateway_keeps_token_connections_on_the_open_api()
    {
        OwnerRezChannelGateway.UsesOpenApiCredentials(
            new ChannelCredentials("ownerrez", "host@example.com", Secret: "pt_token"))
            .Should().BeTrue();

        OwnerRezChannelGateway.UsesOpenApiCredentials(
            new ChannelCredentials("ownerrez", "ora7ccdcead6x"))
            .Should().BeFalse();
    }
}
