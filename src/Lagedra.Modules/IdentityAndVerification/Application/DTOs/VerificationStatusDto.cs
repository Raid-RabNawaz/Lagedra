using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

public sealed record VerificationStatusDto(
    Guid ProfileId,
    Guid UserId,
    VerificationStatus Status,
    VerificationClass VerificationClass,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    DateTime CreatedAt,
    HostKind HostKind,
    string? LegalBusinessName,
    string? BusinessRegistrationNumber,
    string? BusinessJurisdiction)
{
    public static VerificationStatusDto From(IdentityProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new VerificationStatusDto(
            profile.Id,
            profile.UserId,
            profile.Status,
            profile.VerificationClass,
            profile.FirstName,
            profile.LastName,
            profile.DateOfBirth,
            profile.CreatedAt,
            profile.HostKind,
            profile.LegalBusinessName,
            profile.BusinessRegistrationNumber,
            profile.BusinessJurisdiction);
    }

    public static VerificationStatusDto NotStarted(Guid userId) =>
        new(
            Guid.Empty,
            userId,
            VerificationStatus.NotStarted,
            VerificationClass.Low,
            null,
            null,
            null,
            DateTime.UtcNow,
            HostKind.Individual,
            null,
            null,
            null);
}
