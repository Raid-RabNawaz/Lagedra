using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

/// <summary>
/// Submits uploaded documents for manual admin review. Individuals must include
/// an ID front and a live selfie. Property-management companies submit a
/// business registration instead and are not asked for a personal ID.
/// </summary>
public sealed record SubmitManualKycCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    string? LegalBusinessName = null,
    string? BusinessRegistrationNumber = null,
    string? BusinessJurisdiction = null) : IRequest<Result<VerificationStatusDto>>;

public sealed class SubmitManualKycCommandHandler(
    IdentityDbContext dbContext,
    IUserVerificationFlagUpdater flagUpdater)
    : IRequestHandler<SubmitManualKycCommand, Result<VerificationStatusDto>>
{
    public async Task<Result<VerificationStatusDto>> Handle(
        SubmitManualKycCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documentTypes = await dbContext.KycDocuments
            .AsNoTracking()
            .Where(d => d.UserId == request.UserId)
            .Select(d => d.DocumentType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var profile = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        var hostKind = profile?.HostKind ?? HostKind.Individual;
        var validationError = HostVerificationRules.ValidateSubmission(
            hostKind,
            documentTypes,
            request.DateOfBirth,
            request.LegalBusinessName,
            request.BusinessRegistrationNumber,
            request.BusinessJurisdiction,
            DateTime.UtcNow);

        if (validationError is not null)
        {
            return Result<VerificationStatusDto>.Failure(validationError);
        }

        if (profile is null)
        {
            profile = IdentityProfile.Create(
                request.UserId, request.FirstName, request.LastName, request.DateOfBirth);
            profile.StartVerification();
            dbContext.IdentityProfiles.Add(profile);
        }
        else
        {
            switch (profile.Status)
            {
                case VerificationStatus.Verified:
                    return Result<VerificationStatusDto>.Failure(
                        new Error("Identity.Kyc.AlreadyVerified", "Your identity is already verified."));

                case VerificationStatus.ManualReviewRequired:
                    return Result<VerificationStatusDto>.Failure(
                        new Error("Identity.Kyc.UnderReview", "Your submission is already under review."));

                case VerificationStatus.NotStarted:
                case VerificationStatus.Failed:
                    profile.StartVerification();
                    break;

                case VerificationStatus.Pending:
                default:
                    break;
            }

            profile.UpdatePersonalInfo(
                request.FirstName ?? profile.FirstName,
                request.LastName ?? profile.LastName,
                request.DateOfBirth ?? profile.DateOfBirth);
        }

        if (hostKind == HostKind.PropertyManagementCompany)
        {
            profile.SetBusinessDetails(
                request.LegalBusinessName!,
                request.BusinessRegistrationNumber!,
                request.BusinessJurisdiction!);
        }

        profile.RequireManualReview();

        var hasOpenCase = await dbContext.VerificationCases
            .AnyAsync(c => c.UserId == request.UserId && c.CompletedAt == null, cancellationToken)
            .ConfigureAwait(false);

        if (!hasOpenCase)
        {
            dbContext.VerificationCases.Add(VerificationCase.Create(request.UserId));
        }

        await flagUpdater.SetHostKindAsync(
            request.UserId,
            profile.HostKind,
            profile.LegalBusinessName,
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerificationStatusDto>.Success(VerificationStatusDto.From(profile));
    }
}
