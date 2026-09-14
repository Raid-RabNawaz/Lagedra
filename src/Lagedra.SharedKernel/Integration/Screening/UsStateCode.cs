namespace Lagedra.SharedKernel.Integration.Screening;

/// <summary>
/// Checkr requires a two-letter USPS code. Listings often store the full name.
/// </summary>
public static class UsStateCode
{
    private static readonly Dictionary<string, string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AL"] = "AL", ["ALABAMA"] = "AL",
        ["AK"] = "AK", ["ALASKA"] = "AK",
        ["AZ"] = "AZ", ["ARIZONA"] = "AZ",
        ["AR"] = "AR", ["ARKANSAS"] = "AR",
        ["CA"] = "CA", ["CALIFORNIA"] = "CA", ["CALIF"] = "CA",
        ["CO"] = "CO", ["COLORADO"] = "CO",
        ["CT"] = "CT", ["CONNECTICUT"] = "CT",
        ["DE"] = "DE", ["DELAWARE"] = "DE",
        ["DC"] = "DC", ["DISTRICT OF COLUMBIA"] = "DC", ["WASHINGTON DC"] = "DC", ["WASHINGTON D C"] = "DC",
        ["FL"] = "FL", ["FLORIDA"] = "FL",
        ["GA"] = "GA", ["GEORGIA"] = "GA",
        ["HI"] = "HI", ["HAWAII"] = "HI",
        ["ID"] = "ID", ["IDAHO"] = "ID",
        ["IL"] = "IL", ["ILLINOIS"] = "IL",
        ["IN"] = "IN", ["INDIANA"] = "IN",
        ["IA"] = "IA", ["IOWA"] = "IA",
        ["KS"] = "KS", ["KANSAS"] = "KS",
        ["KY"] = "KY", ["KENTUCKY"] = "KY",
        ["LA"] = "LA", ["LOUISIANA"] = "LA",
        ["ME"] = "ME", ["MAINE"] = "ME",
        ["MD"] = "MD", ["MARYLAND"] = "MD",
        ["MA"] = "MA", ["MASSACHUSETTS"] = "MA",
        ["MI"] = "MI", ["MICHIGAN"] = "MI",
        ["MN"] = "MN", ["MINNESOTA"] = "MN",
        ["MS"] = "MS", ["MISSISSIPPI"] = "MS",
        ["MO"] = "MO", ["MISSOURI"] = "MO",
        ["MT"] = "MT", ["MONTANA"] = "MT",
        ["NE"] = "NE", ["NEBRASKA"] = "NE",
        ["NV"] = "NV", ["NEVADA"] = "NV",
        ["NH"] = "NH", ["NEW HAMPSHIRE"] = "NH",
        ["NJ"] = "NJ", ["NEW JERSEY"] = "NJ",
        ["NM"] = "NM", ["NEW MEXICO"] = "NM",
        ["NY"] = "NY", ["NEW YORK"] = "NY",
        ["NC"] = "NC", ["NORTH CAROLINA"] = "NC",
        ["ND"] = "ND", ["NORTH DAKOTA"] = "ND",
        ["OH"] = "OH", ["OHIO"] = "OH",
        ["OK"] = "OK", ["OKLAHOMA"] = "OK",
        ["OR"] = "OR", ["OREGON"] = "OR",
        ["PA"] = "PA", ["PENNSYLVANIA"] = "PA",
        ["RI"] = "RI", ["RHODE ISLAND"] = "RI",
        ["SC"] = "SC", ["SOUTH CAROLINA"] = "SC",
        ["SD"] = "SD", ["SOUTH DAKOTA"] = "SD",
        ["TN"] = "TN", ["TENNESSEE"] = "TN",
        ["TX"] = "TX", ["TEXAS"] = "TX",
        ["UT"] = "UT", ["UTAH"] = "UT",
        ["VT"] = "VT", ["VERMONT"] = "VT",
        ["VA"] = "VA", ["VIRGINIA"] = "VA",
        ["WA"] = "WA", ["WASHINGTON"] = "WA",
        ["WV"] = "WV", ["WEST VIRGINIA"] = "WV",
        ["WI"] = "WI", ["WISCONSIN"] = "WI",
        ["WY"] = "WY", ["WYOMING"] = "WY",
        ["PR"] = "PR", ["PUERTO RICO"] = "PR",
        ["VI"] = "VI", ["VIRGIN ISLANDS"] = "VI", ["US VIRGIN ISLANDS"] = "VI",
        ["GU"] = "GU", ["GUAM"] = "GU",
        ["AS"] = "AS", ["AMERICAN SAMOA"] = "AS",
        ["MP"] = "MP", ["NORTHERN MARIANA ISLANDS"] = "MP",
    };

    public static bool TryNormalize(string? raw, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var key = NormalizeKey(raw);
        if (Codes.TryGetValue(key, out var mapped))
        {
            code = mapped;
            return true;
        }

        var comma = key.IndexOf(',', StringComparison.Ordinal);
        if (comma > 0 && Codes.TryGetValue(key[..comma].Trim(), out mapped))
        {
            code = mapped;
            return true;
        }

        return false;
    }

    private static string NormalizeKey(string raw)
    {
        var chars = raw.Trim().Select(ch => char.IsLetter(ch) || char.IsWhiteSpace(ch) ? ch : ' ');
        return string.Join(' ', new string(chars.ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
