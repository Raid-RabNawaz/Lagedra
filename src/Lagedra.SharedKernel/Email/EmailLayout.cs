using System.Globalization;
using System.Text.RegularExpressions;

namespace Lagedra.SharedKernel.Email;

/// <summary>
/// Shared HTML/plain-text chrome applied to every outbound Lagedra email:
/// brand header (logo + tagline) and company footer.
/// </summary>
public static partial class EmailLayout
{
    public const string Brand = "Lagedra";
    public const string LegalName = "Lagedra LLC";
    public const string Tagline = "Verified furnished stays, 30 days and up";
    public const string StreetAddress = "14622 Ventura Boulevard";
    public const string CityStateZip = "Sherman Oaks, CA 91403";
    public const string ContactEmail = EmailFooter.ContactEmail;
    public const string ContactPhone = EmailFooter.ContactPhone;
    public const string WebsiteUrl = "https://www.lagedra.com";
    public const string PrivacyUrl = "https://www.lagedra.com/privacy";
    public const string TermsUrl = "https://www.lagedra.com/terms";
    public const string BrandColor = "#4141E5";
    public const string LayoutMarker = "data-lagedra-email-layout";

    private const string MailingAddress = StreetAddress + ", " + CityStateZip;

    public static string WrapHtml(string htmlBody)
    {
        ArgumentException.ThrowIfNullOrEmpty(htmlBody);

        if (htmlBody.Contains(LayoutMarker, StringComparison.Ordinal))
        {
            return htmlBody;
        }

        var inner = ExtractBodyContent(htmlBody);
        inner = StripLegacyChrome(inner);
        inner = RecolorLegacyButtons(inner);
        inner = CollapseLeadingHeadingMargin(inner);

        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        return $$"""
            <!doctype html>
            <html lang="en" {{LayoutMarker}}="1">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <meta http-equiv="x-ua-compatible" content="ie=edge" />
              <title>{{Brand}}</title>
            </head>
            <body style="margin:0;padding:0;background:#F3F0FE;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#F3F0FE;">
                <tr>
                  <td align="center" style="padding:24px 12px;">
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;">
                      <tr>
                        <td style="height:6px;background:{{BrandColor}};font-size:0;line-height:0;">&nbsp;</td>
                      </tr>
                      <tr>
                        <td style="padding:28px 36px 20px 36px;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                          <a href="{{WebsiteUrl}}" style="text-decoration:none;">
                            <img src="cid:{{EmailBrandLogo.ContentId}}" width="{{EmailBrandLogo.DisplayWidth}}" height="{{EmailBrandLogo.DisplayHeight}}" alt="{{Brand}}" style="display:block;border:0;outline:none;text-decoration:none;height:{{EmailBrandLogo.DisplayHeight}}px;width:{{EmailBrandLogo.DisplayWidth}}px;" />
                          </a>
                          <p style="margin:10px 0 0 0;font-size:13px;line-height:1.4;color:#6B6B80;">{{Tagline}}</p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 36px 32px 36px;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;font-size:16px;line-height:1.65;color:#1A1A2E;">
                          {{inner}}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:24px 36px 28px 36px;background:#F8F7FC;border-top:1px solid #E5E5EE;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                          <p style="margin:0 0 8px 0;font-size:14px;font-weight:700;color:#1A1A2E;">{{LegalName}}</p>
                          <p style="margin:0 0 12px 0;font-size:13px;line-height:1.55;color:#6B6B80;">
                            {{StreetAddress}}<br />
                            {{CityStateZip}}
                          </p>
                          <p style="margin:0 0 12px 0;font-size:13px;line-height:1.55;color:#6B6B80;">
                            Contact / Email Us at:<br />
                            <a href="mailto:{{ContactEmail}}" style="color:{{BrandColor}};text-decoration:none;">{{ContactEmail}}</a>
                            &nbsp;·&nbsp;{{ContactPhone}}
                          </p>
                          <p style="margin:0 0 16px 0;font-size:13px;line-height:1.55;">
                            <a href="{{WebsiteUrl}}" style="color:{{BrandColor}};text-decoration:none;">Website</a>
                            &nbsp;·&nbsp;
                            <a href="{{PrivacyUrl}}" style="color:{{BrandColor}};text-decoration:none;">Privacy</a>
                            &nbsp;·&nbsp;
                            <a href="{{TermsUrl}}" style="color:{{BrandColor}};text-decoration:none;">Terms</a>
                          </p>
                          <p style="margin:0;font-size:12px;line-height:1.55;color:#8A8A9C;">
                            You're receiving this email from {{Brand}} because you have an account or a booking with us.
                            If you weren't expecting it, you can safely ignore it.
                          </p>
                          <p style="margin:10px 0 0 0;font-size:12px;line-height:1.55;color:#8A8A9C;">
                            © {{year}} {{LegalName}}
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string WrapPlainText(string? plainTextBody)
    {
        var year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        var footer = $"""

            ---
            {Brand}
            {Tagline}

            {LegalName}
            {MailingAddress}
            Contact / Email Us at:
            {ContactEmail}
            {ContactPhone}
            {WebsiteUrl}

            You're receiving this email from {Brand} because you have an account or a booking with us.
            © {year} {LegalName}
            """;

        if (string.IsNullOrWhiteSpace(plainTextBody))
        {
            return footer.TrimStart('\n', '\r');
        }

        return plainTextBody.TrimEnd() + footer;
    }

    /// <summary>
    /// Adds the inline logo unless the caller already attached one.
    /// </summary>
    public static IReadOnlyList<EmailAttachment> WithBrandLogo(
        IReadOnlyList<EmailAttachment>? attachments)
    {
        var logo = EmailBrandLogo.CreateInlineAttachment();
        if (attachments is not { Count: > 0 })
        {
            return [logo];
        }

        if (attachments.Any(a => string.Equals(a.ContentId, EmailBrandLogo.ContentId, StringComparison.Ordinal)))
        {
            return attachments;
        }

        var merged = new EmailAttachment[attachments.Count + 1];
        for (var i = 0; i < attachments.Count; i++)
        {
            merged[i] = attachments[i];
        }

        merged[attachments.Count] = logo;
        return merged;
    }

    internal static string ExtractBodyContent(string html)
    {
        var match = BodyContentRegex().Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : html.Trim();
    }

    internal static string StripLegacyChrome(string html)
    {
        var result = html.Trim();
        result = LegacyContactFooterRegex().Replace(result, string.Empty);
        result = TrailingDisclaimerRegex().Replace(result, string.Empty);
        return result.Trim();
    }

    private static string RecolorLegacyButtons(string html) =>
        html.Replace("background: #111", $"background: {BrandColor}", StringComparison.Ordinal)
            .Replace("background:#111", $"background: {BrandColor}", StringComparison.Ordinal);

    private static string CollapseLeadingHeadingMargin(string html) =>
        LeadingHeadingMarginRegex().Replace(html, "<h2 style=\"margin-top: 0;\"", 1);

    [GeneratedRegex(@"<body[^>]*>(.*)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BodyContentRegex();

    [GeneratedRegex(
        @"<hr\s+style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0;""\s*/>\s*<p\s+style=""color:#6b7280;font-size:13px;line-height:1.6;margin:0;"">[\s\S]*?</p>",
        RegexOptions.IgnoreCase)]
    private static partial Regex LegacyContactFooterRegex();

    [GeneratedRegex(
        @"(<hr\b[^>]*/?>\s*)+(<p\b[^>]*>\s*(This email was sent automatically by Lagedra|You(?:'|&#39;|&apos;)re receiving this because)[\s\S]*?</p>\s*)+$",
        RegexOptions.IgnoreCase)]
    private static partial Regex TrailingDisclaimerRegex();

    [GeneratedRegex(@"<h2\s+style=""margin-top:\s*32px;""", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingHeadingMarginRegex();
}
