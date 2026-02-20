using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(AuthDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindActiveByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow,
                ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct).ConfigureAwait(true);

    public void Update(RefreshToken token) =>
        db.RefreshTokens.Update(token);

    public async Task<int> DeleteExpiredAsync(DateTime olderThan, CancellationToken ct = default) =>
        await db.RefreshTokens
            .Where(t => (t.RevokedAt != null || t.ExpiresAt < DateTime.UtcNow) && t.CreatedAt < olderThan)
            .ExecuteDeleteAsync(ct).ConfigureAwait(true);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
