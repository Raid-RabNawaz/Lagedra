using System.Diagnostics.CodeAnalysis;

namespace Lagedra.SharedKernel.Email;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }

    [SuppressMessage(
        "Performance", "CA1819:Properties should not return arrays",
        Justification = "Email attachment content is a binary payload.")]
    public required byte[] Content { get; init; }
}
