namespace Lagedra.Auth.Infrastructure.Seed;

public sealed class SuperAdminSettings
{
    public const string SectionName = "Seed:SuperAdmin";

    public string Email { get; init; } = "superadmin@lagedra.com";
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "Super Admin";
}
