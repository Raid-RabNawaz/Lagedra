using System.Security.Cryptography;
using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record GenerateReferralLinkCommand(
    Guid OrganizationId,
    Guid CreatedByUserId,
    bool CreatedByIsPlatformAdmin,
    DateTime? ExpiresAt,
    int? MaxUses) : IRequest<Result<ReferralLinkDto>>;

public sealed class GenerateReferralLinkCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IClock clock)
    : IRequestHandler<GenerateReferralLinkCommand, Result<ReferralLinkDto>>
{
    private const int MaxCodeCollisionRetries = 3;

    private static readonly char[] s_alphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<Result<ReferralLinkDto>> Handle(
        GenerateReferralLinkCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireVerifiedOrgAdminAsync(
            request.CreatedByUserId,
            request.OrganizationId,
            request.CreatedByIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<ReferralLinkDto>.Failure(authzResult.Error);
        }

        string? code = null;
        for (var attempt = 0; attempt < MaxCodeCollisionRetries; attempt++)
        {
            var candidate = GenerateCode();
            var exists = await dbContext.ReferralLinks
                .AnyAsync(l => l.Code == candidate, cancellationToken)
                .ConfigureAwait(false);
            if (!exists)
            {
                code = candidate;
                break;
            }
        }

        if (code is null)
        {
            return Result<ReferralLinkDto>.Failure(new Error(
                "Referral.CodeCollision",
                "Could not generate a unique referral code after multiple attempts; please retry."));
        }

        var link = ReferralLink.Create(
            request.OrganizationId, code, request.CreatedByUserId,
            request.ExpiresAt, request.MaxUses, clock);

        dbContext.ReferralLinks.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ReferralLinkDto>.Success(
            new ReferralLinkDto(link.Id, link.OrganizationId, link.Code,
                link.CreatedByUserId, link.ExpiresAt, link.MaxUses,
                link.UsageCount, link.IsActive, link.CreatedAt));
    }

    private static string GenerateCode()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(8, bytes.ToArray(), static (span, data) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = s_alphanumericChars[data[i] % s_alphanumericChars.Length];
            }
        });
    }
}
