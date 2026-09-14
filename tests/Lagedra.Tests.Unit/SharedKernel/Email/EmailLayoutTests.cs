using System;
using System.Linq;
using FluentAssertions;
using Lagedra.SharedKernel.Email;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel.Email;

public class EmailLayoutTests
{
    [Fact]
    public void WrapHtml_adds_header_logo_tagline_and_footer()
    {
        var html = EmailLayout.WrapHtml("<h2>Your booking is active</h2><p>Deal 123 is live.</p>");

        html.Should().Contain(EmailLayout.LayoutMarker);
        html.Should().Contain($"cid:{EmailBrandLogo.ContentId}");
        html.Should().Contain($"alt=\"{EmailLayout.Brand}\"");
        html.Should().Contain(EmailLayout.Tagline);
        html.Should().Contain(EmailLayout.LegalName);
        html.Should().Contain(EmailLayout.StreetAddress);
        html.Should().Contain(EmailLayout.CityStateZip);
        html.Should().Contain(EmailLayout.ContactEmail);
        html.Should().Contain(EmailLayout.ContactPhone);
        html.Should().Contain(EmailLayout.PrivacyUrl);
        html.Should().Contain(EmailLayout.TermsUrl);
        html.Should().Contain("Your booking is active");
        html.Should().Contain("© ");
    }

    [Fact]
    public void WrapHtml_is_idempotent()
    {
        var once = EmailLayout.WrapHtml("<p>Hello</p>");
        var twice = EmailLayout.WrapHtml(once);

        twice.Should().Be(once);
        twice.Split(EmailLayout.LayoutMarker, StringSplitOptions.None).Length.Should().Be(2);
    }

    [Fact]
    public void WrapHtml_extracts_inner_body_and_strips_legacy_chrome()
    {
        const string raw = """
            <!doctype html>
            <html>
            <body style="font-family: sans-serif; max-width: 560px; margin: 0 auto; color: #111;">
              <h2 style="margin-top: 32px;">New booking application</h2>
              <p>A guest applied.</p>
              <p style="margin: 24px 0;">
                <a href="https://example.test" style="display: inline-block; background: #111; color: #fff; padding: 12px 20px;">
                  Approve booking
                </a>
              </p>
              <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
              <p style="font-size: 12px; color: #999;">
                This email was sent automatically by Lagedra. If you weren't expecting it,
                you can safely ignore it.
              </p>
            </body>
            </html>
            """;

        var html = EmailLayout.WrapHtml(raw);

        html.Should().Contain("New booking application");
        html.Should().Contain("A guest applied.");
        html.Should().Contain("Approve booking");
        html.Should().Contain($"background: {EmailLayout.BrandColor}");
        html.Should().NotContain("This email was sent automatically by Lagedra");
        html.Should().NotContain("background: #111");
        html.Should().Contain("margin-top: 0;");
    }

    [Fact]
    public void WrapPlainText_appends_brand_footer()
    {
        var text = EmailLayout.WrapPlainText("Your booking is active.");

        text.Should().StartWith("Your booking is active.");
        text.Should().Contain(EmailLayout.Brand);
        text.Should().Contain(EmailLayout.Tagline);
        text.Should().Contain(EmailLayout.LegalName);
        text.Should().Contain(EmailLayout.ContactEmail);
        text.Should().Contain(EmailLayout.WebsiteUrl);
    }

    [Fact]
    public void WithBrandLogo_adds_inline_png_once()
    {
        var first = EmailLayout.WithBrandLogo(null);
        first.Should().ContainSingle();
        first[0].ContentId.Should().Be(EmailBrandLogo.ContentId);
        first[0].Disposition.Should().Be("inline");
        first[0].ContentType.Should().Be("image/png");
        first[0].Content.Should().NotBeEmpty();
        first[0].Content[0].Should().Be(0x89);
        first[0].Content[1].Should().Be(0x50); // P
        first[0].Content[2].Should().Be(0x4E); // N
        first[0].Content[3].Should().Be(0x47); // G

        var lease = new EmailAttachment
        {
            FileName = "lease.pdf",
            ContentType = "application/pdf",
            Content = [0x25, 0x50, 0x44, 0x46]
        };

        var merged = EmailLayout.WithBrandLogo([lease]);
        merged.Should().HaveCount(2);
        merged.Select(a => a.FileName).Should().Contain(["lease.pdf", EmailBrandLogo.FileName]);

        EmailLayout.WithBrandLogo(first).Should().BeSameAs(first);
    }

    [Fact]
    public void EmailFooter_delegates_to_layout()
    {
        var html = EmailFooter.AppendHtml("<p>Hi</p>");
        html.Should().Contain(EmailLayout.LayoutMarker);
        html.Should().Contain(EmailFooter.ContactEmail);
    }
}
