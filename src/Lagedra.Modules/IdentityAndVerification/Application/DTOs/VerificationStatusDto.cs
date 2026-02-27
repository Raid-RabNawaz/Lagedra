using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;

namespace Lagedra.Modules.IdentityAndVerification.Application.DTOs;

public sealed record VerificationStatusDto(
    Guid ProfileId,
    Guid UserId,
    VerificationStatus Status,
    VerificationClass VerificationClass,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    DateTime CreatedAt);
