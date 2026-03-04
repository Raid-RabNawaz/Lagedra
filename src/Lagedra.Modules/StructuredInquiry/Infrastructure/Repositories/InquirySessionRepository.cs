using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Domain.Aggregates;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Infrastructure.Repositories;

public sealed class InquirySessionRepository(InquiryDbContext dbContext)
{
    public async Task<InquirySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Sessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<InquirySession?> GetOpenForDealAsync(Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.Sessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .FirstOrDefaultAsync(s => s.DealId == dealId
                && s.Status == InquirySessionStatus.Open, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<InquirySession>> GetAllForDealAsync(Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answer)
            .Where(s => s.DealId == dealId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(InquirySession session) =>
        dbContext.Sessions.Add(session);
}
