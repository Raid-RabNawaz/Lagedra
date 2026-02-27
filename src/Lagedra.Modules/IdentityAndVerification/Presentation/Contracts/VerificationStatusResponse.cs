using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.ValueObjects;

namespace Lagedra.Modules.IdentityAndVerification.Presentation.Contracts;

public sealed record VerificationStatusResponse(
    Guid ProfileId,
    Guid UserId,
    VerificationStatus Status,
    VerificationClass VerificationClass,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    DateTime CreatedAt);
