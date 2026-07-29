using System.Diagnostics.CodeAnalysis;
using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Application.Emails;

/// <summary>Subject + HTML + plain-text bodies for a welcome / verification email.</summary>
public sealed record WelcomeEmailMessage(string Subject, string HtmlBody, string PlainTextBody);

/// <summary>
/// Shared welcome / verification email copy for hosts, tenants, and partners.
/// Keeps register, founding-host, waitlist, and resend paths on one template.
/// </summary>
public static class WelcomeEmailComposer
{
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings",
        Justification = "App:FrontendUrl comes from configuration as a string.")]
    public static Uri BuildSpaVerifyUrl(string frontendUrl, Guid userId, string encodedToken)
    {
        var baseUrl = frontendUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/auth/verify-email?userId={userId}&token={encodedToken}");
    }

    [SuppressMessage("Design", "CA1054:URI parameters should not be strings",
        Justification = "App:FrontendUrl comes from configuration as a string.")]
    public static Uri BuildHowItWorksUrl(string frontendUrl) =>
        new($"{frontendUrl.TrimEnd('/')}/how-it-works");

    /// <summary>
    /// Post-launch (and resend) verification email: partner / host / tenant variants.
    /// </summary>
    public static WelcomeEmailMessage BuildWelcomeEmail(ApplicationUser user, Uri verifyUrl)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(verifyUrl);

        var greeting = Greeting(user);
        var href = verifyUrl.AbsoluteUri;

        if (user.Role == UserRole.InstitutionPartner)
        {
            return ComposeVerifyEmail(
                subject: "Welcome to Lagedra — verify your partner account",
                title: "Welcome, partner",
                greeting: greeting,
                intro:
                "Thanks for joining Lagedra as a partner institution. Verify your email to activate your account and start sourcing verified 30+ day housing for relocation, insurance, and corporate placements.",
                nextSteps:
                [
                    "Complete your organization profile so hosts know who they're working with.",
                    "Search and request verified homes across the markets you cover — from one place.",
                    "Endorse the people you place so they qualify for reduced deposits."
                ],
                ctaLabel: "Verify email & start sourcing",
                href);
        }

        if (PreLaunchAccess.IsHostSignup(user.SignupType))
        {
            return ComposeVerifyEmail(
                subject: "Welcome to Lagedra — verify your email and start listing",
                title: "Welcome, host",
                greeting: greeting,
                intro:
                "Thanks for joining Lagedra as a host. Verify your email to activate your account, then list your furnished rentals for qualified tenants looking for 30+ day stays.",
                nextSteps:
                [
                    "Create your first listing — or import it straight from Hostaway.",
                    "Add the location, photos and house rules, then submit it for review.",
                    "Set up payouts so rent and deposits reach you as soon as bookings land."
                ],
                ctaLabel: "Verify email & start listing",
                href);
        }

        return ComposeVerifyEmail(
            subject: "Welcome to Lagedra — verify your email and find a home",
            title: "Welcome, tenant",
            greeting: greeting,
            intro:
            "Thanks for joining Lagedra as a tenant. Verify your email to activate your account and start browsing verified, fully-furnished homes for stays of 30 days and up.",
            nextSteps:
            [
                "Browse verified mid-term homes and save the ones that fit your stay.",
                "Complete your profile and get verified to qualify for reduced deposits.",
                "Book securely — payments and deposits are protected through Lagedra."
            ],
            ctaLabel: "Verify email & start browsing",
            href);
    }

    /// <summary>
    /// Pre-launch founding host: verify email, then set a password, then list / import.
    /// </summary>
    public static WelcomeEmailMessage BuildFoundingHostWelcomeEmail(ApplicationUser user, Uri verifyUrl)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(verifyUrl);

        return ComposeVerifyEmail(
            subject: "Welcome to Lagedra — verify your email and start listing",
            title: "Welcome, founding host",
            greeting: Greeting(user),
            intro:
            "Thanks for joining Lagedra as a founding host. Verify your email, set a password, and you can start listing your furnished rentals and importing from Hostaway right away.",
            nextSteps:
            [
                "Verify your email — you'll be asked to set a password next.",
                "Create your first listing — or import it straight from Hostaway.",
                "Add the location, photos and house rules so your home is ready for review."
            ],
            ctaLabel: "Verify email & start listing",
            verifyUrl.AbsoluteUri);
    }

    /// <summary>
    /// Pre-launch institution partner waitlist — no verification CTA.
    /// </summary>
    public static WelcomeEmailMessage BuildPreLaunchPartnerEmail(ApplicationUser user, Uri howItWorksUrl)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(howItWorksUrl);

        var greeting = Greeting(user);
        var href = howItWorksUrl.AbsoluteUri;
        const string subject = "You're in — welcome to Lagedra as a partner";
        const string intro =
            "Thanks for joining Lagedra as a founding partner institution. We're putting the final pieces in place ahead of launch — and you're on the early list for relocation and insurance housing.";
        const string footnote =
            "As a founding partner institution, you're first in line — and there's no cost to join. We'll be in touch shortly. In the meantime, keep an eye on your inbox.";

        var htmlBody = $"""
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #1A1A2E;">
              <h2 style="margin-top: 32px;">You're in. Welcome, partner.</h2>
              <p>{greeting}</p>
              <p>{intro}</p>
              <h3 style="margin-top: 28px;">What happens next</h3>
              <ol style="line-height: 1.6;">
                <li><strong>We'll reach out personally.</strong> Someone from our team will contact you soon to understand your placement needs and get your organization set up.</li>
                <li><strong>We'll connect you to inventory.</strong> We'll show you how to search and request verified homes for your clients across the markets you cover.</li>
                <li><strong>You place your clients faster.</strong> Vetted 30+ day housing, ready when you need it — without the usual scramble.</li>
              </ol>
              <p>{footnote}</p>
              <p style="margin: 24px 0;">
                <a href="{href}"
                   style="display: inline-block; background: #5B3FE0; color: #fff; padding: 12px 20px; border-radius: 10px; text-decoration: none; font-weight: 600;">
                  Explore how it works
                </a>
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                You're receiving this because you joined the Lagedra founding-partner list as a partner institution.
              </p>
            </body>
            </html>
            """;

        var plainTextBody = $"""
            You're in. Welcome, partner.

            {greeting}

            {intro}

            What happens next
            1. We'll reach out personally. Someone from our team will contact you soon to understand your placement needs and get your organization set up.
            2. We'll connect you to inventory. We'll show you how to search and request verified homes for your clients across the markets you cover.
            3. You place your clients faster. Vetted 30+ day housing, ready when you need it — without the usual scramble.

            {footnote}

            Explore how it works: {href}
            """;

        return new WelcomeEmailMessage(subject, htmlBody, plainTextBody);
    }

    /// <summary>
    /// Short in-app welcome title after email verification.
    /// </summary>
    public static string BuildInAppWelcomeTitle(UserRole role, string? signupType)
    {
        if (role == UserRole.InstitutionPartner)
        {
            return "Welcome, partner";
        }

        if (PreLaunchAccess.IsHostSignup(signupType))
        {
            return "Welcome, host";
        }

        return "Welcome, tenant";
    }

    /// <summary>
    /// Short in-app welcome body after email verification.
    /// </summary>
    public static string BuildInAppWelcomeBody(UserRole role, string? signupType)
    {
        if (role == UserRole.InstitutionPartner)
        {
            return "Your partner institution account is ready. Complete your organization profile to start sourcing verified 30+ day housing for your clients.";
        }

        if (PreLaunchAccess.IsHostSignup(signupType))
        {
            return "Your host account is ready. Create or import your first listing to get it in front of qualified long-stay tenants.";
        }

        return "Your tenant account is ready. Browse verified mid-term homes and complete your profile to get started.";
    }

    /// <summary>
    /// Chooses founding-host vs standard welcome based on whether the account already has a password.
    /// Used by resend so the second email matches the first.
    /// </summary>
    public static WelcomeEmailMessage BuildVerificationEmail(
        ApplicationUser user,
        Uri verifyUrl,
        bool hasPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(verifyUrl);

        if (!hasPassword && PreLaunchAccess.IsHostSignup(user.SignupType))
        {
            return BuildFoundingHostWelcomeEmail(user, verifyUrl);
        }

        return BuildWelcomeEmail(user, verifyUrl);
    }

    private static string Greeting(ApplicationUser user) =>
        string.IsNullOrWhiteSpace(user.FirstName) ? "Hi," : $"Hi {user.FirstName},";

    [SuppressMessage("Design", "CA1054:URI parameters should not be strings",
        Justification = "HTML email templates interpolate AbsoluteUri strings.")]
    private static WelcomeEmailMessage ComposeVerifyEmail(
        string subject,
        string title,
        string greeting,
        string intro,
        string[] nextSteps,
        string ctaLabel,
        string verifyUrl)
    {
        var htmlSteps = string.Join("\n", nextSteps.Select(s => $"    <li>{s}</li>"));
        var plainSteps = string.Join("\n", nextSteps.Select((s, i) => $"{i + 1}. {s}"));

        var htmlBody = $"""
            <!doctype html>
            <html>
            <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #1A1A2E;">
              <h2 style="margin-top: 32px;">{title}</h2>
              <p>{greeting}</p>
              <p>{intro}</p>
              <p style="margin: 24px 0;">
                <a href="{verifyUrl}"
                   style="display: inline-block; background: #5B3FE0; color: #fff; padding: 12px 20px; border-radius: 10px; text-decoration: none; font-weight: 600;">
                  {ctaLabel}
                </a>
              </p>
              <h3 style="margin-top: 28px;">What happens next</h3>
              <ol style="line-height: 1.6;">
            {htmlSteps}
              </ol>
              <p style="font-size: 13px; color: #666;">This verification link expires in 24 hours.</p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                You're receiving this because this email address was used to create a Lagedra account.
                If that wasn't you, you can safely ignore this email.
              </p>
            </body>
            </html>
            """;

        var plainTextBody = $"""
            {title}

            {greeting}

            {intro}

            Verify your email: {verifyUrl}

            What happens next
            {plainSteps}

            This verification link expires in 24 hours.
            If you didn't create this account, you can safely ignore this email.
            """;

        return new WelcomeEmailMessage(subject, htmlBody, plainTextBody);
    }
}
