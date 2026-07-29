using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

/// <summary>
/// Submits the uploaded ID photos + live selfie for manual admin review.
/// Requires at least the ID front and a selfie. Moves the identity profile
/// to ManualReviewRequired, where it appears in the admin queue.
/// </summary>
public sealed record SubmitManualKycCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth) : IRequest<Result<VerificationStatusDto>>;

public sealed class SubmitManualKycCommandHandler(IdentityDbContext dbContext)
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

        if (!documentTypes.Contains(KycDocumentType.IdFront))
        {
            return Result<VerificationStatusDto>.Failure(
                new Error("Identity.Kyc.MissingIdFront",
                    "Upload a photo of the front of your government-issued ID before submitting."));
        }

        if (!documentTypes.Contains(KycDocumentType.Selfie))
        {
            return Result<VerificationStatusDto>.Failure(
                new Error("Identity.Kyc.MissingSelfie",
                    "Capture a live selfie before submitting."));
        }

        var profile = await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

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

        profile.RequireManualReview();

        var hasOpenCase = await dbContext.VerificationCases
            .AnyAsync(c => c.UserId == request.UserId && c.CompletedAt == null, cancellationToken)
            .ConfigureAwait(false);

        if (!hasOpenCase)
        {
            dbContext.VerificationCases.Add(VerificationCase.Create(request.UserId));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VerificationStatusDto>.Success(new VerificationStatusDto(
            profile.Id, profile.UserId, profile.Status, profile.VerificationClass,
            profile.FirstName, profile.LastName, profile.DateOfBirth, profile.CreatedAt));
    }
}
