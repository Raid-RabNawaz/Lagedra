using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class ArbitratorPanelProvider(AuthDbContext dbContext) : IArbitratorPanelProvider
{
    public async Task<IReadOnlyList<ArbitratorPanelMemberDto>> GetPanelMembersAsync(CancellationToken ct = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Arbitrator && u.IsActive && !u.IsDeleted)
            .OrderBy(u => u.Email)
            .Select(u => new ArbitratorPanelMemberDto(
                u.Id,
                u.Email ?? string.Empty,
                u.DisplayName ?? (u.FirstName != null ? u.FirstName + " " + u.LastName : null)))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
