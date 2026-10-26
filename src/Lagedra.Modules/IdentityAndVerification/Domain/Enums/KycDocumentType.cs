namespace Lagedra.Modules.IdentityAndVerification.Domain.Enums;

public enum KycDocumentType
{
    /// <summary>Front side of a government-issued photo ID.</summary>
    IdFront,

    /// <summary>Back side of a government-issued photo ID (optional for passports).</summary>
    IdBack,

    /// <summary>Live selfie captured during submission, used to match the ID photo.</summary>
    Selfie,

    /// <summary>
    /// Certificate of incorporation, business license, or equivalent registration
    /// for a property-management company. Replaces a personal ID.
    /// </summary>
    BusinessRegistration,

    /// <summary>
    /// Optional proof that the account holder may act for the company, such as a
    /// property-management agreement or an authorization letter.
    /// </summary>
    ProofOfAuthority
}
