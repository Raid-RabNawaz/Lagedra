namespace Lagedra.Auth.Domain;

/// <summary>
/// Shared rules for who may sign in / self-register while
/// <c>prelaunch.enabled</c> is on. Founding hosts get a real account with a
/// limited product surface; partners stay on the waitlist.
/// </summary>
public static class PreLaunchAccess
{
    public const string HostSignupType = "Host";
    public const string MemberSignupType = "Member";
    public const string PartnerSignupType = "Partner";

    private static readonly HashSet<UserRole> ExemptRoles =
        [UserRole.PlatformAdmin, UserRole.Arbitrator];

    public static bool IsExemptRole(UserRole role) => ExemptRoles.Contains(role);

    public static bool IsHostSignup(string? signupType) =>
        string.Equals(signupType, HostSignupType, StringComparison.OrdinalIgnoreCase);

    public static bool IsMemberSignup(string? signupType) =>
        string.Equals(signupType, MemberSignupType, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The personal join door: new accounts (<see cref="MemberSignupType"/>)
    /// and legacy founding-host accounts (<see cref="HostSignupType"/>).
    /// </summary>
    public static bool IsPersonalSignup(string? signupType) =>
        IsMemberSignup(signupType) || IsHostSignup(signupType);

    public static bool IsPartnerSignup(string? signupType) =>
        string.Equals(signupType, PartnerSignupType, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Personal accounts (guests, and hosts who joined before the guest-first
    /// signup) may authenticate during pre-launch. Institution partners stay
    /// on the waitlist. Operational staff are always allowed.
    /// </summary>
    public static bool CanSignIn(ApplicationUser user) =>
        IsExemptRole(user.Role) || IsPersonalSignup(user.SignupType);
}
