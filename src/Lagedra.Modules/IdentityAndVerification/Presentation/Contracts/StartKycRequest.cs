namespace Lagedra.Modules.IdentityAndVerification.Presentation.Contracts;

public sealed record StartKycRequest(
    Guid UserId,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth);
