namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Single source of truth for user-visible copy related to the Partner-Backed
/// Protection tier (Phase 18 — Option A). Backend DTO mappers, Truth Surface
/// canonical content, email templates, and frontend i18n keys all reference
/// these constants so the label is consistent across surfaces.
/// </summary>
public static class PartnerEndorsementCopy
{
    /// <summary>The user-visible name of the Partner-Backed Protection tier.</summary>
    public const string PartnerBackedTierLabel = "Partner-Backed Protection";

    /// <summary>The user-visible name of the third-party insurance tier.</summary>
    public const string ThirdPartyInsuredTierLabel = "Insured";

    /// <summary>The user-visible name of the no-coverage tier.</summary>
    public const string UninsuredTierLabel = "Uninsured";

    /// <summary>
    /// Returns the wire-format token used in DTOs / canonical content for a given
    /// <see cref="ProtectionTierKind"/>. Stable across releases — never localised.
    /// </summary>
    public static string ToToken(ProtectionTierKind tier) => tier switch
    {
        ProtectionTierKind.PartnerBacked => "PartnerBacked",
        ProtectionTierKind.ThirdPartyInsured => "ThirdPartyInsured",
        _ => "Uninsured"
    };
}

public enum ProtectionTierKind
{
    Uninsured,
    ThirdPartyInsured,
    PartnerBacked
}
