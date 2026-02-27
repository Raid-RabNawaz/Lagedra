using System.Security.Cryptography;
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
    DateTime? ExpiresAt,
    int? MaxUses) : IRequest<Result<ReferralLinkDto>>;

public sealed class GenerateReferralLinkCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<GenerateReferralLinkCommand, Result<ReferralLinkDto>>
{
    private static readonly char[] s_alphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public async Task<Result<ReferralLinkDto>> Handle(
        GenerateReferralLinkCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orgExists = await dbContext.Organizations
            .AnyAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (!orgExists)
        {
            return Result<ReferralLinkDto>.Failure(
                new Error("Partner.NotFound", "Partner organization not found."));
        }

        var code = GenerateUniqueCode();

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

    private static string GenerateUniqueCode()
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
