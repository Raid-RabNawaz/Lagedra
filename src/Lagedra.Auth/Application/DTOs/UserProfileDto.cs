using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Application.DTOs;

public sealed record UserProfileDto(
    Guid UserId,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
