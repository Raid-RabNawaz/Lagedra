using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// Pins the OwnerRez OAuth handshake against the shapes documented at
/// https://www.ownerrez.com/support/articles/api-oauth-app: client credentials as
/// HTTP Basic on the token endpoint, form-encoded grants, and bearer access tokens.
/// </summary>
public sealed class OwnerRezOAuthClientTests
{
    private static readonly OwnerRezChannelSettings Settings = new()
    {
        ClientId = "c_lagedra",
        ClientSecret = "s_shhh",
    };

    private static readonly Uri Redirect = new("https://api.lagedra.com/v1/channels/ownerrez/oauth/callback");

    private static OwnerRezOAuthClient CreateClient(StubHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.ownerrez.com") },
        Options.Create(Settings),
        NullLogger<OwnerRezOAuthClient>.Instance);

    [Fact]
    public void BuildAuthorizationUrl_CarriesCodeGrantParameters()
    {
        var url = CreateClient(new StubHandler(_ => (HttpStatusCode.OK, "{}")))
            .BuildAuthorizationUrl(Redirect, "state-123");

        url.GetLeftPart(UriPartial.Path).Should().Be("https://app.ownerrez.com/oauth/authorize");
        url.Query.Should().Contain("response_type=code");
        url.Query.Should().Contain("client_id=c_lagedra");
        url.Query.Should().Contain("state=state-123");
        url.Query.Should().Contain(Uri.EscapeDataString(Redirect.ToString()));
    }

    [Fact]
    public async Task ExchangeCodeAsync_SendsBasicClientAuthAndAuthorizationCodeGrant()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """
            {"access_token":"at_abc","token_type":"bearer","scope":"full","user_id":123456}
            """));

        var tokens = await CreateClient(handler)
            .ExchangeCodeAsync("tc_xyz", Redirect, CancellationToken.None);

        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().Be("at_abc");
        // Numeric in the payload, but only ever used as an opaque account id.
        tokens.UserId.Should().Be("123456");
        tokens.RefreshToken.Should().BeNull();
        tokens.ExpiresAt.Should().BeNull();

        var call = handler.Calls.Should().ContainSingle().Subject;
        call.Method.Should().Be("POST");
        call.Path.Should().Be("/oauth/access_token");
        call.Authorization.Should().Be(
            "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("c_lagedra:s_shhh")));
        call.Body.Should().Contain("grant_type=authorization_code");
        call.Body.Should().Contain("code=tc_xyz");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ReadsRefreshTokenAndAbsoluteExpiry()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """
            {"access_token":"at_abc","refresh_token":"rt_def","user_id":7,
             "user_display_name":"Beach House Rentals",
             "expires_in":2592000,"expires_at":"2026-08-30T12:00:00.0000000Z",
             "refresh_token_supported":true}
            """));

        var tokens = await CreateClient(handler)
            .ExchangeCodeAsync("tc_xyz", Redirect, CancellationToken.None);

        tokens!.RefreshToken.Should().Be("rt_def");
        tokens.UserDisplayName.Should().Be("Beach House Rentals");
        tokens.ExpiresAt.Should().Be(new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Device-style grants omit <c>expires_at</c>, so the relative lifetime has to
    /// be honoured or the refresh job would never see the token as due.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_FallsBackToRelativeExpiry()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """
            {"access_token":"at_abc","refresh_token":"rt_def","user_id":7,"expires_in":3600}
            """));

        var tokens = await CreateClient(handler)
            .ExchangeCodeAsync("tc_xyz", Redirect, CancellationToken.None);

        tokens!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ExchangeCodeAsync_RejectedCode_ReturnsNull()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.BadRequest, """
            {"error":"invalid_grant","error_description":"Code already used"}
            """));

        var tokens = await CreateClient(handler)
            .ExchangeCodeAsync("tc_used", Redirect, CancellationToken.None);

        tokens.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_SendsRefreshTokenGrant()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """
            {"access_token":"at_new","refresh_token":"rt_new","user_id":7,"expires_in":2592000}
            """));

        var tokens = await CreateClient(handler).RefreshAsync("rt_old", CancellationToken.None);

        tokens!.AccessToken.Should().Be("at_new");
        var call = handler.Calls.Should().ContainSingle().Subject;
        call.Body.Should().Contain("grant_type=refresh_token");
        call.Body.Should().Contain("refresh_token=rt_old");
    }

    [Fact]
    public async Task RevokeAsync_DeletesTheTokenAtOwnerRez()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, "{}"));

        var revoked = await CreateClient(handler).RevokeAsync("at_abc", CancellationToken.None);

        revoked.Should().BeTrue();
        var call = handler.Calls.Should().ContainSingle().Subject;
        call.Method.Should().Be("DELETE");
        call.Path.Should().Be("/oauth/access_token/at_abc");
    }

    [Fact]
    public async Task RevokeAsync_Failure_IsReportedWithoutThrowing()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.NotFound, "{}"));

        var revoked = await CreateClient(handler).RevokeAsync("at_gone", CancellationToken.None);

        revoked.Should().BeFalse();
    }

    private sealed record Call(string Method, string Path, string? Authorization, string? Body);

    private sealed class StubHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> router)
        : HttpMessageHandler
    {
        public List<Call> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Calls.Add(new Call(
                request.Method.Method,
                request.RequestUri!.AbsolutePath,
                request.Headers.Authorization?.ToString(),
                body));

            var (status, responseBody) = router(request);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
