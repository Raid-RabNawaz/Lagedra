using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Application.DTOs;

public sealed record UserProfileDto(
    Guid UserId,
    string Email,
    UserRole Role,
    bool IsActive,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PhoneNumber,
    string? Bio,
    Uri? ProfilePhotoUrl,
    string? City,
    string? State,
    string? Country,
    string? Languages,
    string? Occupation,
    DateOnly? DateOfBirth,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool IsGovernmentIdVerified,
    bool IsPhoneVerified,
    int? ResponseRatePercent,
    int? ResponseTimeMinutes,
    DateTime MemberSince,
    DateTime? LastLoginAt);
