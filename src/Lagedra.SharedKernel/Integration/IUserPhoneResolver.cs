namespace Lagedra.SharedKernel.Integration;

public interface IUserPhoneResolver
{
    Task<string?> GetPhoneAsync(Guid userId, CancellationToken ct = default);

    Task<bool> IsPhoneVerifiedAsync(Guid userId, CancellationToken ct = default);
}
