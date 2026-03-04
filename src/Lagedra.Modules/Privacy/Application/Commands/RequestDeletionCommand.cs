using Lagedra.Modules.Privacy.Application.DTOs;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainDeletionRequest = Lagedra.Modules.Privacy.Domain.Entities.DeletionRequest;

namespace Lagedra.Modules.Privacy.Application.Commands;

public sealed record RequestDeletionCommand(Guid UserId) : IRequest<Result<DeletionRequestDto>>;

public sealed class RequestDeletionCommandHandler(PrivacyDbContext dbContext)
    : IRequestHandler<RequestDeletionCommand, Result<DeletionRequestDto>>
{
    public async Task<Result<DeletionRequestDto>> Handle(
        RequestDeletionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasActiveHold = await dbContext.LegalHolds
            .AnyAsync(h => h.UserId == request.UserId && h.ReleasedAt == null, cancellationToken)
            .ConfigureAwait(false);

        var deletionRequest = DomainDeletionRequest.Create(request.UserId);

        if (hasActiveHold)
        {
            deletionRequest.Block("Active legal hold exists for this user.");
        }

        dbContext.DeletionRequests.Add(deletionRequest);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DeletionRequestDto>.Success(new DeletionRequestDto(
            deletionRequest.Id, deletionRequest.UserId, deletionRequest.Status,
            deletionRequest.RequestedAt, deletionRequest.CompletedAt, deletionRequest.BlockingReason));
    }
}
