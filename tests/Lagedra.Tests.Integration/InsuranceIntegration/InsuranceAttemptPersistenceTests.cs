using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.Time;
using Lagedra.Modules.InsuranceIntegration.Application.Commands;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Repositories;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lagedra.Tests.Integration.InsuranceIntegration;

/// <summary>
/// Every verification attempt written against an <em>already-persisted</em>
/// policy record went through the same defect that broke the lease template
/// seed: the attempt Id is assigned in its constructor, so EF read the entity
/// it found in the loaded record's collection as an existing row and emitted an
/// UPDATE matching nothing. The save then threw
/// DbUpdateConcurrencyException and rolled back the state transition alongside
/// it — a purchased policy would not be marked active.
///
/// These only fail on a relational provider; the in-memory provider does not
/// check affected row counts.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class InsuranceAttemptPersistenceTests(PostgresFixture postgres)
{
    private const string Schema = "insurance";

    private InsuranceDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseNpgsql(postgres.ConnectionString)
            .Options;

        return new InsuranceDbContext(options, new SystemClock());
    }

    /// <summary>
    /// Persists a policy record and returns its deal id. The record is saved in
    /// its own context so that the handler under test loads it as a tracked
    /// existing row — the path that was broken. Creating it in the same unit of
    /// work masks the defect, because Add propagates Added across the graph.
    /// </summary>
    private async Task<Guid> GivenPersistedRecordAsync()
    {
        var dealId = Guid.NewGuid();

        await using var db = NewContext();
        await db.MigrateAndClearAsync(Schema);
        db.PolicyRecords.Add(InsurancePolicyRecord.Create(Guid.NewGuid(), dealId));
        await db.SaveChangesAsync();

        return dealId;
    }

    private async Task<InsurancePolicyRecord> LoadAsync(Guid dealId)
    {
        await using var db = NewContext();
        return await db.PolicyRecords
            .Include(r => r.Attempts)
            .SingleAsync(r => r.DealId == dealId);
    }

    [Fact]
    public async Task Purchase_webhook_records_the_attempt_and_activates_the_policy()
    {
        var dealId = await GivenPersistedRecordAsync();

        await using (var db = NewContext())
        {
            var handler = new HandleInsurancePurchaseWebhookCommandHandler(db);
            var result = await handler.Handle(
                new HandleInsurancePurchaseWebhookCommand(
                    dealId, "Truvi", "POL-1", "Damage", DateTime.UtcNow.AddDays(30), "{\"ok\":true}"),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        var record = await LoadAsync(dealId);

        // The activation and the attempt share one transaction, so a failed
        // attempt insert silently loses the activation too.
        record.State.Should().Be(InsuranceState.Active);
        record.PolicyNumber.Should().Be("POL-1");
        record.Attempts.Should().HaveCount(1);
        record.Attempts.Single().Source.Should().Be(VerificationSource.API);
    }

    [Fact]
    public async Task Manual_proof_upload_records_the_attempt()
    {
        var dealId = await GivenPersistedRecordAsync();

        await using (var db = NewContext())
        {
            var handler = new UploadManualProofCommandHandler(db);
            var result = await handler.Handle(
                new UploadManualProofCommand(dealId, "proof-123.pdf"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        var record = await LoadAsync(dealId);

        record.Attempts.Should().HaveCount(1);
        record.Attempts.Single().Source.Should().Be(VerificationSource.ManualUpload);
        record.Attempts.Single().Result.Should().Contain("proof-123.pdf");
    }

    [Fact]
    public async Task Starting_verification_records_the_attempt_on_an_existing_record()
    {
        // The conditional half of this defect: the handler creates the record
        // when absent, which happens to work, and reuses it when present,
        // which did not.
        var dealId = await GivenPersistedRecordAsync();

        await using (var db = NewContext())
        {
            var handler = new StartInsuranceVerificationCommandHandler(db);
            var result = await handler.Handle(
                new StartInsuranceVerificationCommand(dealId, Guid.NewGuid()), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        (await LoadAsync(dealId)).Attempts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Starting_verification_twice_records_both_attempts()
    {
        var dealId = await GivenPersistedRecordAsync();

        for (var i = 0; i < 2; i++)
        {
            await using var db = NewContext();
            var handler = new StartInsuranceVerificationCommandHandler(db);
            await handler.Handle(
                new StartInsuranceVerificationCommand(dealId, Guid.NewGuid()), CancellationToken.None);
        }

        (await LoadAsync(dealId)).Attempts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Store_records_an_attempt_against_a_loaded_record()
    {
        // Covers the Truvi screening paths, which all funnel their attempts
        // through the store rather than touching the DbContext directly.
        var dealId = await GivenPersistedRecordAsync();

        await using (var db = NewContext())
        {
            var store = new InsurancePolicyRecordRepository(db);
            var record = await store.GetByDealIdAsync(dealId);

            store.AddAttempt(record!, new InsuranceVerificationAttempt(
                record!.Id, "Truvi declined", VerificationSource.API));

            await store.SaveChangesAsync();
        }

        var reloaded = await LoadAsync(dealId);

        reloaded.Attempts.Should().HaveCount(1);
        reloaded.Attempts.Single().Result.Should().Be("Truvi declined");
    }
}
