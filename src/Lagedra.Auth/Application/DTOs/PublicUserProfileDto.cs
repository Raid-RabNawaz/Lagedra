namespace Lagedra.Auth.Application.DTOs;

/// <summary>
/// Subset of <see cref="UserProfileDto"/> safe to expose to other authenticated
/// users (e.g. a host inspecting a booking request, or a tenant viewing a
/// host's public profile). Excludes contact details, emergency contacts,
/// date of birth, and other PII the platform must not leak across users.
/// </summary>
public sealed record PublicUserProfileDto(
    Guid UserId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio,
    Uri? ProfilePhotoUrl,
    string? City,
    string? State,
    string? Country,
    string? Languages,
    string? Occupation,
    bool IsGovernmentIdVerified,
    bool IsPhoneVerified,
    bool IsEmailVerified,
    int? ResponseRatePercent,
    int? ResponseTimeMinutes,
    DateTime MemberSince);
