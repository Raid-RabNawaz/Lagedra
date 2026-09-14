namespace Lagedra.SharedKernel.Email;

/// <summary>
/// The Lagedra wordmark embedded in the assembly so every outbound email can
/// show the logo without depending on a public CDN URL.
/// </summary>
public static class EmailBrandLogo
{
    public const string ContentId = "lagedra-logo";
    public const string FileName = "lagedra-logo.png";
    public const string ContentType = "image/png";
    public const int DisplayWidth = 138;
    public const int DisplayHeight = 31;

    private const string ResourceName = "Lagedra.SharedKernel.Email.Assets.lagedra-logo.png";

    private static readonly byte[] PngBytes = LoadPng();

    public static EmailAttachment CreateInlineAttachment() =>
        new()
        {
            FileName = FileName,
            ContentType = ContentType,
            Content = (byte[])PngBytes.Clone(),
            ContentId = ContentId,
            Disposition = "inline"
        };

    private static byte[] LoadPng()
    {
        var assembly = typeof(EmailBrandLogo).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded email logo '{ResourceName}' was not found.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("Embedded email logo is empty.");
        }

        return bytes;
    }
}
