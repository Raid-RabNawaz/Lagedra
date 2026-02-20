using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
