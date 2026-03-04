using Lagedra.Auth.Domain;

namespace Lagedra.Auth.Infrastructure.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    void Update(RefreshToken token);
    Task<int> DeleteExpiredAsync(DateTime olderThan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
