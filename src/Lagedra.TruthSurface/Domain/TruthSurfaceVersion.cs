using System.Globalization;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.TruthSurface.Domain;

public sealed class TruthSurfaceVersion : ValueObject
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public TruthSurfaceVersion(int major, int minor, int patch)
    {
        if (major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), "Version components must be non-negative.");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    public static TruthSurfaceVersion Parse(string version)
    {
        ArgumentNullException.ThrowIfNull(version);
        var parts = version.Split('.');
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid version format: '{version}'. Expected 'major.minor.patch'.");
        }

        return new TruthSurfaceVersion(int.Parse(parts[0], CultureInfo.InvariantCulture),
                                       int.Parse(parts[1], CultureInfo.InvariantCulture),
                                       int.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
    }
}
