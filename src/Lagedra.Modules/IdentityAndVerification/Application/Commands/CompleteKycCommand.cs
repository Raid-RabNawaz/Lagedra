using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CompleteKycCommand(
    Guid UserId,
    string? ExternalInquiryId) : IRequest<Result<VerificationStatusDto>>;

public sealed class CompleteKycCommandHandler(
    IdentityDbContext dbContext,
    IKycProvider kycProvider)
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

        var verificationCase = await dbContext.VerificationCases
            .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.CompletedAt == null, cancellationToken)
            .ConfigureAwait(false);

        var inquiryId = request.ExternalInquiryId ?? verificationCase?.ExternalInquiryId;

        if (!string.IsNullOrEmpty(inquiryId))
        {
            var statusResult = await kycProvider
                .GetInquiryStatusAsync(inquiryId, cancellationToken)
                .ConfigureAwait(false);

            if (statusResult.Status == KycInquiryStatus.Failed)
            {
                profile.Fail("KYC inquiry failed at provider.");
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result<VerificationStatusDto>.Failure(
                    new Error("Identity.KycFailed", "KYC verification failed at the provider."));
            }

            if (statusResult.Status != KycInquiryStatus.Completed)
            {
                return Result<VerificationStatusDto>.Failure(
                    new Error("Identity.KycNotComplete",
                        $"KYC inquiry is not yet completed (status: {statusResult.Status})."));
            }
        }

        profile.Complete();

        if (verificationCase is not null)
        {
            if (!string.IsNullOrEmpty(inquiryId) && verificationCase.ExternalInquiryId is null)
            {
                verificationCase.AssignInquiry(inquiryId);
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
