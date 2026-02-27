namespace Lagedra.Auth.Presentation.Contracts;

public sealed record UpdateProfileRequest(
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
    string? EmergencyContactPhone);
