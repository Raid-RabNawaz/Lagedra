using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Application.DTOs;

public sealed record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserRole Role);
