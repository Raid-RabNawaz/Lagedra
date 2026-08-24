namespace Lagedra.SharedKernel.Integration;

public interface IUserEmailResolver
{
    Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Resolves the user id for an email address, or null when no account
    /// with that email exists. Lets flows accept a human-friendly email
    /// (e.g. adding partner members) instead of asking users for their GUID.
    /// </summary>
    Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct = default);
}
