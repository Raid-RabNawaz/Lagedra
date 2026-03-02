using Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Application.Commands;

/// <summary>
/// One-time seed command to bootstrap the California jurisdiction pack
/// with deposit cap rules per CA Civil Code §1950.5.
/// Idempotent — skips if the pack already exists.
/// </summary>
public sealed record SeedCaliforniaDepositCapCommand : IRequest<Result>;

public sealed class SeedCaliforniaDepositCapCommandHandler(JurisdictionDbContext dbContext)
    : IRequestHandler<SeedCaliforniaDepositCapCommand, Result>
{
    private const string CaliforniaCode = "US-CA";

    public async Task<Result> Handle(
        SeedCaliforniaDepositCapCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await dbContext.JurisdictionPacks
            .AnyAsync(p => p.JurisdictionCode.Code == CaliforniaCode, cancellationToken)
            .ConfigureAwait(false);

        if (existing)
        {
            return Result.Success();
        }

        var pack = JurisdictionPack.CreateDraft(CaliforniaCode);
        var version = pack.AddVersion();

        version.SetEffectiveDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // CA Civil Code §1950.5: unfurnished = 1× monthly rent, furnished = 2× monthly rent
        version.AddDepositCapRule(
            CaliforniaCode,
            maxMultiplier: 1.0m,
            legalReference: "CA Civil Code §1950.5(a)",
            exceptionCondition: "furnished",
            exceptionMultiplier: 2.0m);

        // LA-specific rent stabilization overlay
        version.AddDepositCapRule(
            "US-CA-LA",
            maxMultiplier: 1.0m,
            legalReference: "LA Municipal Code §151.06; CA Civil Code §1950.5(a)",
            exceptionCondition: "furnished",
            exceptionMultiplier: 2.0m);

        version.AddFieldGatingRule(
            "DepositAmount",
            Domain.Enums.GatingType.Hard,
            "must-not-exceed-jurisdiction-cap",
            condition: null);

        version.AddEvidenceSchedule(
            "MoveInConditionReport",
            "Timestamped photo/video walkthrough of the rental unit within 7 days before tenant move-in");

        dbContext.JurisdictionPacks.Add(pack);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
