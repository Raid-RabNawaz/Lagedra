using System;
using System.IO;

namespace Lagedra.Tests.Unit.ListingAndLocation;

/// <summary>Helper for loading the sanitized listing-import HTML fixtures.</summary>
internal static class ListingImportFixtures
{
    public static string Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ListingImport", fileName);
        return File.ReadAllText(path);
    }
}
