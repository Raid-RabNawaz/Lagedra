using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.StructuredInquiry.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Infrastructure.Persistence;

namespace Lagedra.Modules.StructuredInquiry.Application.Commands;

public sealed record CloseInquiryOnTruthSurfaceConfirmationCommand(
    Guid DealId) : IRequest<Result>;

public sealed class CloseInquiryOnTruthSurfaceConfirmationCommandHandler(
    InquiryDbContext dbContext)
    : IRequestHandler<CloseInquiryOnTruthSurfaceConfirmationCommand, Result>
{
    public async Task<Result> Handle(
        CloseInquiryOnTruthSurfaceConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.DealId == request.DealId
                && s.Status == InquirySessionStatus.Open, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result.Failure(
                new Error("Inquiry.NotFound", "No open inquiry session found for this deal."));
        }

        session.Close();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
