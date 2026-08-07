using System;
using System.Text;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// Pins how OwnerRez webhook deliveries are authenticated and interpreted. The
/// payload shapes come from https://www.ownerrez.com/support/articles/api-webhooks.
/// </summary>
public sealed class OwnerRezWebhookParserTests
{
    private static readonly OwnerRezChannelSettings Configured = new()
    {
        WebhookUsername = "lagedra-ownerrez",
        WebhookPassword = "s3cret",
    };

    private static string BasicHeader(string user, string password)
        => "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));

    // ── Authentication ───────────────────────────────────────────────────────

    [Fact]
    public void IsAuthorized_MatchingCredentials_Accepts()
        => OwnerRezWebhookParser
            .IsAuthorized(BasicHeader("lagedra-ownerrez", "s3cret"), Configured)
            .Should().BeTrue();

    [Theory]
    [InlineData("lagedra-ownerrez", "wrong")]
    [InlineData("someone-else", "s3cret")]
    [InlineData("", "")]
    public void IsAuthorized_WrongCredentials_Rejects(string user, string password)
        => OwnerRezWebhookParser
            .IsAuthorized(BasicHeader(user, password), Configured)
            .Should().BeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer at_token")]
    [InlineData("Basic not-base64!!")]
    [InlineData("Basic dXNlcm5vY29sb24=")]
    public void IsAuthorized_UnusableHeader_Rejects(string? header)
        => OwnerRezWebhookParser.IsAuthorized(header, Configured).Should().BeFalse();

    /// <summary>
    /// An unconfigured deployment must not become an open endpoint: these payloads
    /// can cancel a booking, so anyone who guessed the URL could act as OwnerRez.
    /// </summary>
    [Fact]
    public void IsAuthorized_CredentialsNotConfigured_RejectsEverything()
    {
        var settings = new OwnerRezChannelSettings();

        OwnerRezWebhookParser.IsAuthorized(BasicHeader("any", "thing"), settings)
            .Should().BeFalse();
    }

    // ── Envelope parsing ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2,3]")]
    public void TryParse_UnusableBody_ReturnsNull(string? payload)
        => OwnerRezWebhookParser.TryParse(payload).Should().BeNull();

    [Fact]
    public void TryParse_TestPing_IsRecognisedWithoutAnAccount()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"id":12345,"user_id":56789,"action":"webhook_test",
             "entity_type":"api_application","entity_id":12345}
            """);

        delivery.Should().NotBeNull();
        delivery!.IsTestPing.Should().BeTrue();
        delivery.BookingUpdate.Should().BeNull();
    }

    [Fact]
    public void TryParse_AuthorizationRevoked_IsRecognised()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"id":"abc","user_id":56789,"action":"application_authorization_revoked",
             "entity_type":"api_application","entity_id":12345}
            """);

        delivery!.IsAuthorizationRevoked.Should().BeTrue();
        delivery.UserId.Should().Be("56789");
    }

    /// <summary>
    /// OwnerRez documents these ids as integers, but they are matched against text
    /// columns, so both encodings have to land on the same string.
    /// </summary>
    [Fact]
    public void TryParse_StringUserId_ReadsTheSameAsNumeric()
        => OwnerRezWebhookParser
            .TryParse("""{"action":"entity_update","entity_type":"guest","user_id":"56789"}""")!
            .UserId.Should().Be("56789");

    [Fact]
    public void TryParse_ActiveBookingUpdate_MapsToConfirmed()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"id":"abc","user_id":56789,"action":"entity_update","entity_type":"booking",
             "entity_id":9001,"categories":["dates"],
             "entity":{"id":9001,"status":"active","is_block":false,
                       "updated_utc":"2026-08-01T10:00:00Z"}}
            """);

        delivery!.IsBooking.Should().BeTrue();
        delivery.BookingUpdate.Should().NotBeNull();
        delivery.BookingUpdate!.ExternalBookingId.Should().Be("9001");
        delivery.BookingUpdate.Status.Should().Be("confirmed");
        delivery.BookingUpdate.ChangedAtUtc.Should().Be(
            new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TryParse_CanceledBookingUpdate_MapsToCancelled()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"user_id":56789,"action":"entity_update","entity_type":"booking","entity_id":9001,
             "categories":["canceled"],
             "entity":{"id":9001,"status":"canceled","canceled_utc":"2026-08-01T10:00:00Z"}}
            """);

        delivery!.BookingUpdate!.Status.Should().Be("cancelled");
    }

    /// <summary>
    /// OwnerRez only allows deleting a reservation nothing else references, and the
    /// stay is gone either way, so a delete is reconciled as a cancellation. The
    /// envelope's entity_id is used because a delete may carry no entity.
    /// </summary>
    [Fact]
    public void TryParse_DeletedBooking_CancelsUsingEnvelopeId()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"user_id":56789,"action":"entity_delete","entity_type":"booking","entity_id":9001}
            """);

        delivery!.BookingUpdate!.ExternalBookingId.Should().Be("9001");
        delivery.BookingUpdate.Status.Should().Be("cancelled");
    }

    /// <summary>
    /// Blocks are owner holds, not reservations, so they must not reconcile against
    /// a guest booking link.
    /// </summary>
    [Fact]
    public void TryParse_BlockNotBooking_YieldsNoUpdate()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"user_id":56789,"action":"entity_update","entity_type":"booking","entity_id":9001,
             "entity":{"id":9001,"status":"active","is_block":true}}
            """);

        delivery!.IsBooking.Should().BeTrue();
        delivery.BookingUpdate.Should().BeNull();
    }

    [Fact]
    public void TryParse_BookingUpdateWithoutEntity_YieldsNoUpdate()
        => OwnerRezWebhookParser
            .TryParse("""{"user_id":1,"action":"entity_update","entity_type":"booking","entity_id":9001}""")!
            .BookingUpdate.Should().BeNull();

    [Fact]
    public void TryParse_PropertyChange_YieldsNoBookingUpdate()
    {
        var delivery = OwnerRezWebhookParser.TryParse(
            """
            {"user_id":56789,"action":"entity_update","entity_type":"property","entity_id":4321,
             "categories":["photos"],"entity":{"id":4321,"name":"Cabin"}}
            """);

        delivery!.IsBooking.Should().BeFalse();
        delivery.BookingUpdate.Should().BeNull();
        delivery.EntityType.Should().Be("property");
    }
}
