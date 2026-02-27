using Lagedra.Compliance.Application.DTOs;
using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Compliance.Application.Commands;

public sealed record RecordViolationCommand(
    Guid DealId,
    Guid ReportedByUserId,
    ViolationCategory Category,
    string Description,
    string? EvidenceReference) : IRequest<Result<ViolationDto>>;

public sealed class RecordViolationCommandHandler(ComplianceDbContext dbContext)
    : IRequestHandler<RecordViolationCommand, Result<ViolationDto>>
{
    public async Task<Result<ViolationDto>> Handle(RecordViolationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violation = Violation.Record(
            request.DealId,
            request.ReportedByUserId,
            request.Category,
            request.Description,
            request.EvidenceReference);

        dbContext.Violations.Add(violation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ViolationDto>.Success(MapToDto(violation));
    }

    private static ViolationDto MapToDto(Violation v) =>
        new(v.Id, v.DealId, v.ReportedByUserId, v.Category, v.Status,
            v.Description, v.EvidenceReference, v.DetectedAt, v.ResolvedAt);
}
