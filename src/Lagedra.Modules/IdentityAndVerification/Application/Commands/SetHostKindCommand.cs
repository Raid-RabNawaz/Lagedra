using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

/// <summary>
/// Chooses whether this host verifies as an individual or as a property-management
/// company. Documents that belong to the other path are dropped so a company
/// file never still contains a personal ID (and the reverse).
/// </summary>
public sealed record SetHostKindCommand(Guid UserId, HostKind HostKind)
    : IRequest<Result<VerificationStatusDto>>;

public sealed class SetHostKindCommandHandler(
    IdentityDbContext dbContext,
    IUserVerificationFlagUpdater flagUpdater)
    : IRequestHandler<SetHostKindCommand, Result<VerificationStatusDto>>
{
    public async Task<Result<VerificationStatusDto>> Handle(
        SetHostKindCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.HostKind))
        {
            return Result<VerificationStatusDto>.Failure(
                new Error("Identity.HostKind.Invalid", "Choose an individual host or a property management company."));
        }

        var profile = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            profile = IdentityProfile.Create(request.UserId, null, null, null);
            dbContext.IdentityProfiles.Add(profile);
        }

        if (!profile.TrySetHostKind(request.HostKind))
        {
            return Result<VerificationStatusDto>.Failure(new Error(
                "Identity.HostKind.Locked",
                "Verification is already submitted, so the host type can no longer be changed."));
        }

        var documents = await dbContext.KycDocuments
            .Where(d => d.UserId == request.UserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var stale = documents
            .Where(d => request.HostKind == HostKind.PropertyManagementCompany
                ? HostVerificationRules.IsPersonalDocument(d.DocumentType)
                : HostVerificationRules.IsBusinessDocument(d.DocumentType))
            .ToList();

        if (stale.Count > 0)
        {
            dbContext.KycDocuments.RemoveRange(stale);
        }

        await flagUpdater.SetHostKindAsync(
            request.UserId,
            request.HostKind,
            profile.LegalBusinessName,
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerificationStatusDto>.Success(VerificationStatusDto.From(profile));
    }
}
