using Lagedra.SharedKernel.Domain;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public Uri? ProfilePhotoUrl { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Languages { get; set; }
    public string? Occupation { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool IsGovernmentIdVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public int? ResponseRatePercent { get; set; }
    public int? ResponseTimeMinutes { get; set; }
}
