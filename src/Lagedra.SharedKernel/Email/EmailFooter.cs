namespace Lagedra.SharedKernel.Email;

/// <summary>
/// Standard contact footer appended to every outbound email by <see cref="IEmailService"/>.
/// </summary>
public static class EmailFooter
{
    public const string ContactEmail = "info@lagedra.com";
    public const string ContactPhone = "213-735-2362";

    private const string HtmlFooter = """
        <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;" />
        <p style="color:#6b7280;font-size:13px;line-height:1.6;margin:0;">
          Contact / Email Us at:<br />
          <a href="mailto:info@lagedra.com" style="color:#5B3FE0;text-decoration:none;">info@lagedra.com</a><br />
          213-735-2362
        </p>
        """;

    private const string PlainTextFooter = """

        ---
        Contact / Email Us at:
        info@lagedra.com
        213-735-2362
        """;

    public static string AppendHtml(string htmlBody)
    {
        ArgumentException.ThrowIfNullOrEmpty(htmlBody);

        const string bodyClose = "</body>";
        var insertAt = htmlBody.LastIndexOf(bodyClose, StringComparison.OrdinalIgnoreCase);
        if (insertAt >= 0)
        {
            return htmlBody.Insert(insertAt, HtmlFooter);
        }

        return htmlBody + HtmlFooter;
    }

    public static string AppendPlainText(string? plainTextBody)
    {
        if (string.IsNullOrWhiteSpace(plainTextBody))
        {
            return PlainTextFooter.TrimStart('\n', '\r');
        }

        return plainTextBody.TrimEnd() + PlainTextFooter;
    }
}
