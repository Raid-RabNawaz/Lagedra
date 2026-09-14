using FluentAssertions;
using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Application;
using Stripe;
using System.Security.Claims;
using Xunit;

namespace Lagedra.Tests.Unit.IdentityAndVerification.Application;

public sealed class StripeConnectErrorsTests
{
    [Fact]
    public void IsMissingAccount_when_stripe_says_no_such_account()
    {
        var ex = new StripeException("No such account: 'acct_123'")
        {
            StripeError = new StripeError
            {
                Code = "resource_missing",
                Message = "No such account: 'acct_123'",
            },
        };

        StripeConnectErrors.IsMissingAccount(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMissingAccount_when_key_cannot_access_connect_account_on_other_platform()
    {
        var ex = new StripeException(
            "The provided key 'sk_live_***' does not have access to account 'acct_old' (or that account does not exist). Application access may have been revoked.")
        {
            StripeError = new StripeError
            {
                Code = "account_invalid",
                Message =
                    "The provided key 'sk_live_***' does not have access to account 'acct_old' (or that account does not exist). Application access may have been revoked.",
            },
        };

        StripeConnectErrors.IsMissingAccount(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMissingAccount_when_unrelated_stripe_error()
    {
        var ex = new StripeException("Your account cannot currently make live charges")
        {
            StripeError = new StripeError
            {
                Code = "account_invalid",
                Message = "Your account cannot currently make live charges",
            },
        };

        StripeConnectErrors.IsMissingAccount(ex).Should().BeFalse();
    }

    [Fact]
    public void Email_reads_mapped_or_raw_jwt_email_claim()
    {
        var mapped = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "host@example.com"),
        ]));
        HostStripeClaimReader.Email(mapped).Should().Be("host@example.com");

        var raw = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("email", " raw@example.com "),
        ]));
        HostStripeClaimReader.Email(raw).Should().Be("raw@example.com");

        HostStripeClaimReader.Email(new ClaimsPrincipal(new ClaimsIdentity())).Should().BeNull();
    }
}
