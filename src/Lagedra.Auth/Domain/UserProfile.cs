namespace Lagedra.Auth.Domain;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Bio { get; private set; }
    public Uri? ProfilePhotoUrl { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Country { get; private set; }
    public string? Work { get; private set; }
    public string? Languages { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public bool GovernmentIdVerified { get; private set; }
    public bool PhoneVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ApplicationUser? User { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(Guid userId)
    {
        var now = DateTime.UtcNow;
        return new UserProfile
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? displayName,
        string? phoneNumber,
        string? bio,
        Uri? profilePhotoUrl,
        string? city,
        string? state,
        string? country,
        string? work,
        string? languages,
        DateOnly? dateOfBirth,
        string? emergencyContactName,
        string? emergencyContactPhone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        PhoneNumber = phoneNumber;
        Bio = bio;
        ProfilePhotoUrl = profilePhotoUrl;
        City = city;
        State = state;
        Country = country;
        Work = work;
        Languages = languages;
        DateOfBirth = dateOfBirth;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProfilePhoto(Uri url)
    {
        ProfilePhotoUrl = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkGovernmentIdVerified()
    {
        GovernmentIdVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPhoneVerified()
    {
        PhoneVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
