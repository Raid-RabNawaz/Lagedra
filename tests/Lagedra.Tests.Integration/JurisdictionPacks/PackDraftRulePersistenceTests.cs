using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.Time;
using Lagedra.Modules.JurisdictionPacks.Application.Commands;
using Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;
using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lagedra.Tests.Integration.JurisdictionPacks;

/// <summary>
/// Editing a saved pack draft appends rules to a loaded <c>PackVersion</c>,
/// and rule Ids are assigned by the domain — so EF treated each new rule as an
/// existing row and emitted an UPDATE that matched nothing. Every rule an
/// admin added to an already-saved draft was lost when the save rolled back,
/// while the handler still returned the in-memory version as if it had
/// persisted.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PackDraftRulePersistenceTests(PostgresFixture postgres)
{
    private const string Schema = "jurisdiction";

    private JurisdictionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<JurisdictionDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .Options;

        return new JurisdictionDbContext(options, new SystemClock());
    }

    /// <summary>
    /// Saves a pack with an empty draft version in its own unit of work, so the
    /// handler under test loads it as an existing row. Adding rules while the
    /// pack itself is still new hides the defect.
    /// </summary>
    private async Task<(Guid PackId, Guid VersionId)> GivenPersistedDraftAsync()
    {
        await using var db = NewContext();
        await db.MigrateAndClearAsync(Schema);

        var pack = JurisdictionPack.CreateDraft("US-CA");
        var version = pack.AddVersion();
        db.JurisdictionPacks.Add(pack);
        await db.SaveChangesAsync();

        return (pack.Id, version.Id);
    }

    private async Task ApplyAsync(UpdatePackDraftCommand command)
    {
        await using var db = NewContext();
        var handler = new UpdatePackDraftCommandHandler(new JurisdictionPackRepository(db));
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Updating_a_saved_draft_persists_rules_of_every_type()
    {
        var (packId, versionId) = await GivenPersistedDraftAsync();

        await ApplyAsync(new UpdatePackDraftCommand(
            packId,
            versionId,
            EffectiveDate: new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveDateRules: [new EffectiveDateRuleInput("depositCap", new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc))],
            FieldGatingRules: [new FieldGatingRuleInput("petDeposit", GatingType.Hard, "true", null)],
            EvidenceSchedules: [new EvidenceScheduleInput("Photos", "Minimum 6 photos")],
            DepositCapRules: [new DepositCapRuleInput("US-CA", 2m, "Cal. Civ. Code 1950.5")]));

        await using var db = NewContext();
        var version = await db.PackVersions
            .Include(v => v.EffectiveDateRules)
            .Include(v => v.FieldGatingRules)
            .Include(v => v.EvidenceSchedules)
            .Include(v => v.DepositCapRules)
            .SingleAsync(v => v.Id == versionId);

        version.EffectiveDate.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        version.EffectiveDateRules.Should().ContainSingle().Which.FieldName.Should().Be("depositCap");
        version.FieldGatingRules.Should().ContainSingle().Which.FieldName.Should().Be("petDeposit");
        version.EvidenceSchedules.Should().ContainSingle().Which.Category.Should().Be("Photos");
        version.DepositCapRules.Should().ContainSingle().Which.MaxMultiplier.Should().Be(2m);
    }

    [Fact]
    public async Task Rules_added_across_separate_edits_accumulate()
    {
        // The second edit loads a version that already has children, which is
        // the shape most likely to regress: the existing rows must stay
        // untouched while only the appended rule is inserted.
        var (packId, versionId) = await GivenPersistedDraftAsync();

        for (var i = 1; i <= 3; i++)
        {
            await ApplyAsync(new UpdatePackDraftCommand(
                packId,
                versionId,
                EffectiveDate: null,
                EffectiveDateRules: [new EffectiveDateRuleInput($"field{i}", new DateTime(2027, 1, i, 0, 0, 0, DateTimeKind.Utc))],
                FieldGatingRules: null,
                EvidenceSchedules: null,
                DepositCapRules: null));
        }

        await using var db = NewContext();
        var version = await db.PackVersions
            .Include(v => v.EffectiveDateRules)
            .SingleAsync(v => v.Id == versionId);

        version.EffectiveDateRules.Select(r => r.FieldName)
            .Should().BeEquivalentTo(["field1", "field2", "field3"]);
    }

    [Fact]
    public async Task Deposit_cap_rules_are_returned_by_the_repository()
    {
        // GetByIdAsync did not Include DepositCapRules, so the handler's
        // response omitted any already-saved caps even when the write worked.
        var (packId, versionId) = await GivenPersistedDraftAsync();

        await ApplyAsync(new UpdatePackDraftCommand(
            packId, versionId, null, null, null, null,
            DepositCapRules: [new DepositCapRuleInput("US-CA", 2m, "Cal. Civ. Code 1950.5")]));

        await using var db = NewContext();
        var pack = await new JurisdictionPackRepository(db).GetByIdAsync(packId);

        pack!.Versions.Single(v => v.Id == versionId)
            .DepositCapRules.Should().ContainSingle();
    }
}
