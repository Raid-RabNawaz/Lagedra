using System.Security.Cryptography;
using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Repositories;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Auth.Application.Services;

public sealed class RefreshTokenService(
    IRefreshTokenRepository repository,
    IHashingService hashingService,
    IClock clock,
    IConfiguration configuration)
{
    private readonly int _expiryDays = int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;

    public async Task<(RefreshToken token, string rawToken)> CreateAsync(
        Guid userId, string ipAddress, CancellationToken ct = default)
    {
        var rawToken = GenerateRaw();
        var tokenHash = hashingService.Hash(rawToken);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedByIp = ipAddress,
            CreatedAt = clock.UtcNow,
            ExpiresAt = clock.UtcNow.AddDays(_expiryDays)
        };

        await repository.AddAsync(refreshToken, ct).ConfigureAwait(true);
        await repository.SaveChangesAsync(ct).ConfigureAwait(true);

        return (refreshToken, rawToken);
    }

    public async Task<RefreshToken?> GetActiveAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = hashingService.Hash(rawToken);
        return await repository.FindActiveByHashAsync(hash, ct).ConfigureAwait(true);
    }

    public async Task RevokeAsync(RefreshToken token, string ipAddress, string? replacedByHash = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        token.RevokedAt = clock.UtcNow;
        token.ReplacedByTokenHash = replacedByHash;
        repository.Update(token);
        await repository.SaveChangesAsync(ct).ConfigureAwait(true);
    }

    private static string GenerateRaw()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
