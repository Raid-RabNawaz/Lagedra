namespace Lagedra.SharedKernel.Integration;

public interface IUserEmailResolver
{
    Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default);
}
