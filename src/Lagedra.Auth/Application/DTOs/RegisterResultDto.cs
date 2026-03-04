namespace Lagedra.Auth.Application.DTOs;

public sealed record RegisterResultDto(
    Guid UserId,
    Uri VerificationUrl,
    string VerificationToken);
