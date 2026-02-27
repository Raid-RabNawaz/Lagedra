using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Compliance.Application.Commands;

public sealed record RecordLedgerEntryCommand(
    Guid UserId,
    TrustLedgerEntryType EntryType,
    Guid? ReferenceId,
    string? Description,
    bool IsPublic) : IRequest<Result<TrustLedgerEntryDto>>;

public sealed class RecordLedgerEntryCommandHandler(ComplianceDbContext dbContext)
    : IRequestHandler<RecordLedgerEntryCommand, Result<TrustLedgerEntryDto>>
{
    public async Task<Result<TrustLedgerEntryDto>> Handle(RecordLedgerEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = TrustLedgerEntry.Create(
            request.UserId,
            request.EntryType,
            request.ReferenceId,
            request.Description,
            request.IsPublic);

        dbContext.TrustLedgerEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<TrustLedgerEntryDto>.Success(
            new TrustLedgerEntryDto(entry.Id, entry.UserId, entry.EntryType,
                entry.ReferenceId, entry.Description, entry.OccurredAt, entry.IsPublic));
    }
}
