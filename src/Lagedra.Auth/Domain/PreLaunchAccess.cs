namespace Lagedra.Auth.Domain;

/// <summary>
/// Shared rules for who may sign in / self-register while
/// <c>prelaunch.enabled</c> is on. Founding hosts get a real account with a
/// limited product surface; partners stay on the waitlist.
/// </summary>
public static class PreLaunchAccess
{
    public const string HostSignupType = "Host";
    public const string PartnerSignupType = "Partner";

    private static readonly HashSet<UserRole> ExemptRoles =
        [UserRole.PlatformAdmin, UserRole.Arbitrator];

    public static bool IsExemptRole(UserRole role) => ExemptRoles.Contains(role);

    public static bool IsHostSignup(string? signupType) =>
        string.Equals(signupType, HostSignupType, StringComparison.OrdinalIgnoreCase);

    public static bool IsPartnerSignup(string? signupType) =>
        string.Equals(signupType, PartnerSignupType, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Members who signed up as hosts may authenticate during pre-launch.
    /// Everyone else (except operational staff) is blocked at login.
    /// </summary>
    public static bool CanSignIn(ApplicationUser user) =>
        IsExemptRole(user.Role) || IsHostSignup(user.SignupType);
}
