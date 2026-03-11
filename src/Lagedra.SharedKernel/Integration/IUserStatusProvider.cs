namespace Lagedra.SharedKernel.Integration;

public interface IUserStatusProvider
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken ct = default);
}
