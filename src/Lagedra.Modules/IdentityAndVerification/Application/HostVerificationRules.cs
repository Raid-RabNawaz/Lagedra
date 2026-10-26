using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.IdentityAndVerification.Application;

/// <summary>
/// Decides which documents a host must submit. Individuals need a government ID
/// and a live selfie. Property-management companies need a business registration
/// and are not asked for a personal ID.
/// </summary>
public static class HostVerificationRules
{
    public static bool IsPersonalDocument(KycDocumentType documentType) =>
        documentType is KycDocumentType.IdFront or KycDocumentType.IdBack or KycDocumentType.Selfie;

    public static bool IsBusinessDocument(KycDocumentType documentType) =>
        documentType is KycDocumentType.BusinessRegistration or KycDocumentType.ProofOfAuthority;

    public static bool AcceptsPdf(KycDocumentType documentType) => IsBusinessDocument(documentType);

    public static Error? ValidateUpload(HostKind hostKind, KycDocumentType documentType)
    {
        if (hostKind == HostKind.PropertyManagementCompany && IsPersonalDocument(documentType))
        {
            return new Error(
                "Identity.Kyc.PersonalIdNotRequired",
                "Property management companies verify with a business registration, not a personal ID.");
        }

        if (hostKind == HostKind.Individual && IsBusinessDocument(documentType))
        {
            return new Error(
                "Identity.Kyc.BusinessDocumentNotAllowed",
                "Business documents are only for property management companies. Upload a government-issued ID instead.");
        }

        return null;
    }

    public static Error? ValidateSubmission(
        HostKind hostKind,
        IReadOnlyCollection<KycDocumentType> documents,
        DateTime? dateOfBirth,
        string? legalBusinessName,
        string? businessRegistrationNumber,
        string? businessJurisdiction,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(documents);

        if (hostKind == HostKind.PropertyManagementCompany)
        {
            if (string.IsNullOrWhiteSpace(legalBusinessName))
            {
                return new Error(
                    "Identity.Kyc.MissingBusinessName",
                    "Enter the legal name of the property management company.");
            }

            if (string.IsNullOrWhiteSpace(businessRegistrationNumber))
            {
                return new Error(
                    "Identity.Kyc.MissingRegistrationNumber",
                    "Enter the company's registration or license number.");
            }

            if (string.IsNullOrWhiteSpace(businessJurisdiction))
            {
                return new Error(
                    "Identity.Kyc.MissingJurisdiction",
                    "Enter the state or country where the company is registered.");
            }

            if (!documents.Contains(KycDocumentType.BusinessRegistration))
            {
                return new Error(
                    "Identity.Kyc.MissingBusinessRegistration",
                    "Upload the company's registration certificate or business license. A personal ID is not required.");
            }

            return null;
        }

        if (dateOfBirth is { } dob && !MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, utcNow))
        {
            return new Error(
                "Identity.Kyc.Underage",
                "You must be at least 18 years old to complete identity verification.");
        }

        if (!documents.Contains(KycDocumentType.IdFront))
        {
            return new Error(
                "Identity.Kyc.MissingIdFront",
                "Upload a photo of the front of your government-issued ID before submitting.");
        }

        if (!documents.Contains(KycDocumentType.Selfie))
        {
            return new Error(
                "Identity.Kyc.MissingSelfie",
                "Capture a live selfie before submitting.");
        }

        return null;
    }
}
