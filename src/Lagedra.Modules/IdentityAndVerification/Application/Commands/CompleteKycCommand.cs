using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CompleteKycCommand(
    Guid UserId,
    string? PersonaInquiryId) : IRequest<Result<VerificationStatusDto>>;

public sealed class CompleteKycCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<CompleteKycCommand, Result<VerificationStatusDto>>
{
    public async Task<Result<VerificationStatusDto>> Handle(
        CompleteKycCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<VerificationStatusDto>.Failure(
                new Error("Identity.NotFound", "Identity profile not found."));
        }

        profile.Complete();

        var verificationCase = await dbContext.VerificationCases
            .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.CompletedAt == null, cancellationToken)
            .ConfigureAwait(false);

        if (verificationCase is not null)
        {
            if (request.PersonaInquiryId is not null)
            {
                verificationCase.AssignInquiry(request.PersonaInquiryId);
            }

            verificationCase.MarkCompleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerificationStatusDto>.Success(MapToDto(profile));
    }

    private static VerificationStatusDto MapToDto(IdentityProfile p) =>
        new(p.Id, p.UserId, p.Status, p.VerificationClass,
            p.FirstName, p.LastName, p.DateOfBirth, p.CreatedAt);
}
