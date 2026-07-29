namespace Lagedra.Modules.IdentityAndVerification.Domain.Enums;

public enum KycDocumentType
{
    /// <summary>Front side of a government-issued photo ID.</summary>
    IdFront,

    /// <summary>Back side of a government-issued photo ID (optional for passports).</summary>
    IdBack,

    /// <summary>Live selfie captured during submission, used to match the ID photo.</summary>
    Selfie
}
