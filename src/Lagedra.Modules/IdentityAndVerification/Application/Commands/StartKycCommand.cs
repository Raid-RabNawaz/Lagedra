using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record StartKycCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth) : IRequest<Result<VerificationStatusDto>>;

public sealed class StartKycCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<StartKycCommand, Result<VerificationStatusDto>>
{
    public async Task<Result<VerificationStatusDto>> Handle(
        StartKycCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return Result<VerificationStatusDto>.Failure(
                new Error("Identity.AlreadyExists", "An identity profile already exists for this user."));
        }

        var profile = IdentityProfile.Create(
            request.UserId, request.FirstName, request.LastName, request.DateOfBirth);

        profile.StartVerification();

        var verificationCase = VerificationCase.Create(request.UserId);

        dbContext.IdentityProfiles.Add(profile);
        dbContext.VerificationCases.Add(verificationCase);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerificationStatusDto>.Success(MapToDto(profile));
    }

    private static VerificationStatusDto MapToDto(IdentityProfile p) =>
        new(p.Id, p.UserId, p.Status, p.VerificationClass,
            p.FirstName, p.LastName, p.DateOfBirth, p.CreatedAt);
}
