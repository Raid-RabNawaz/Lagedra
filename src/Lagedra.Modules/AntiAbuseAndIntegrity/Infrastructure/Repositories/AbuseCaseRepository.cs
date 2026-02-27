using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Repositories;

public sealed class AbuseCaseRepository(IntegrityDbContext dbContext)
{
    public async Task<AbuseCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.AbuseCases
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AbuseCase>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.AbuseCases
            .AsNoTracking()
            .Where(a => a.SubjectUserId == userId)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(AbuseCase abuseCase) =>
        dbContext.AbuseCases.Add(abuseCase);
}
