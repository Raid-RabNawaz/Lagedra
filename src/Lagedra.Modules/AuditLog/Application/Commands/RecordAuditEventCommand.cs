using Lagedra.SharedKernel.Results;
using Lagedra.Modules.AuditLog.Domain.Entities;
using Lagedra.Modules.AuditLog.Infrastructure.Persistence;
using MediatR;

namespace Lagedra.Modules.AuditLog.Application.Commands;

public sealed record RecordAuditEventCommand(
    Guid? UserId,
    string EventType,
    string EntityType,
    string EntityId,
    string? Details,
    string? IpAddress) : IRequest<Result>;

public sealed class RecordAuditEventCommandHandler(AuditDbContext dbContext)
    : IRequestHandler<RecordAuditEventCommand, Result>
{
    public async Task<Result> Handle(RecordAuditEventCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var auditEvent = AuditEvent.Create(
            request.UserId,
            request.EventType,
            request.EntityType,
            request.EntityId,
            request.Details,
            request.IpAddress);

        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
