using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record VerifyInstitutionAffiliationCommand(
    Guid UserId,
    string? OrganizationType,
    Guid? OrganizationId,
    VerificationMethod Method) : IRequest<Result>;

public sealed class VerifyInstitutionAffiliationCommandHandler(IdentityDbContext dbContext)
    : IRequestHandler<VerifyInstitutionAffiliationCommand, Result>
{
    public async Task<Result> Handle(
        VerifyInstitutionAffiliationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var affiliation = AffiliationVerification.Create(
            request.UserId,
            request.OrganizationType,
            request.OrganizationId,
            request.Method);

        affiliation.MarkVerified();

        dbContext.AffiliationVerifications.Add(affiliation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
