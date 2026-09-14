namespace Lagedra.SharedKernel.Email;

/// <summary>
/// Contact constants and backward-compatible wrap helpers. All outbound mail
/// is styled through <see cref="EmailLayout"/>.
/// </summary>
public static class EmailFooter
{
    public const string ContactEmail = "info@lagedra.com";
    public const string ContactPhone = "213-735-2362";

    public static string AppendHtml(string htmlBody) => EmailLayout.WrapHtml(htmlBody);

    public static string AppendPlainText(string? plainTextBody) =>
        EmailLayout.WrapPlainText(plainTextBody);
}
