using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.ValueObjects;

public sealed class FileHash : ValueObject
{
    public string Value { get; }

    private FileHash(string value)
    {
        Value = value;
    }

    public static FileHash Create(string hexSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hexSha256);

        if (hexSha256.Length != 64)
        {
            throw new ArgumentException("SHA-256 hex string must be exactly 64 characters.", nameof(hexSha256));
        }

        return new FileHash(hexSha256.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
