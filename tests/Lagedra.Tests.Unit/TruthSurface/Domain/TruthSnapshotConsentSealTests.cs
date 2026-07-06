using System;
using System.Linq;
using FluentAssertions;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.TruthSurface.Domain;
using Xunit;

namespace Lagedra.Tests.Unit.TruthSurface.Domain;

public class TruthSnapshotConsentSealTests
{
    private static readonly Guid Deal = Guid.NewGuid();
    private static readonly Guid TenantUser = Guid.NewGuid();
    private static readonly Guid HostUser = Guid.NewGuid();

    private static TruthSnapshot PendingDraft()
    {
        var snapshot = TruthSnapshot.CreateDraft(
            Deal, "proto-v1", "pack-v1", "{\"deal\":\"content\"}");
        snapshot.SubmitForConfirmation();
        return snapshot;
    }

    private static TruthSnapshot RecordConsents(TruthSnapshot snapshot)
    {
        snapshot.RecordBothConsents(
            tenantUserId: TenantUser,
            tenantConsentAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            tenantConsentIp: "1.2.3.4",
            tenantConsentUserAgent: "tenant-ua",
            tenantConsentVersion: "ts-consent-v1",
            hostUserId: HostUser,
            hostConsentAt: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            hostConsentIp: "5.6.7.8",
            hostConsentUserAgent: "host-ua",
            hostConsentVersion: "ts-consent-v1");
        return snapshot;
    }

    [Fact]
    public void RecordBothConsents_marks_both_parties_and_stores_metadata()
    {
        var snapshot = RecordConsents(PendingDraft());

        snapshot.LandlordConfirmed.Should().BeTrue();
        snapshot.TenantConfirmed.Should().BeTrue();

        snapshot.TenantConsentUserId.Should().Be(TenantUser);
        snapshot.TenantConsentIp.Should().Be("1.2.3.4");
        snapshot.TenantConsentVersion.Should().Be("ts-consent-v1");

        snapshot.HostConsentUserId.Should().Be(HostUser);
        snapshot.HostConsentIp.Should().Be("5.6.7.8");
        snapshot.HostConsentVersion.Should().Be("ts-consent-v1");
    }

    [Fact]
    public void Seal_after_both_consents_locks_and_confirms()
    {
        var snapshot = RecordConsents(PendingDraft());
        var sealedAt = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        snapshot.Seal("hash-abc", "sig-xyz", sealedAt);

        snapshot.Status.Should().Be(TruthSurfaceStatus.Confirmed);
        snapshot.IsLocked.Should().BeTrue();
        snapshot.LockedAt.Should().Be(sealedAt);
        snapshot.SealedAt.Should().Be(sealedAt);
        snapshot.Hash.Should().Be("hash-abc");
        snapshot.Signature.Should().Be("sig-xyz");
        snapshot.Proof.Should().NotBeNull();
        snapshot.DomainEvents.OfType<TruthSurfaceConfirmedEvent>().Should().ContainSingle();
    }

    [Fact]
    public void Seal_without_both_confirmations_is_rejected()
    {
        var snapshot = PendingDraft();
        snapshot.ConfirmByLandlord(); // only one party

        var act = () => snapshot.Seal("h", "s", DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Sealing_twice_is_rejected_keeping_the_record_immutable()
    {
        var snapshot = RecordConsents(PendingDraft());
        snapshot.Seal("h", "s", DateTime.UtcNow);

        var act = () => snapshot.Seal("h2", "s2", DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecordBothConsents_requires_consent_versions()
    {
        var snapshot = PendingDraft();

        var act = () => snapshot.RecordBothConsents(
            TenantUser, DateTime.UtcNow, null, null, tenantConsentVersion: "",
            HostUser, DateTime.UtcNow, null, null, hostConsentVersion: "ts-consent-v1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Void_on_terminal_cancel_flips_status_but_keeps_proof()
    {
        var snapshot = RecordConsents(PendingDraft());
        snapshot.Seal("h", "s", DateTime.UtcNow);

        snapshot.Void("tenant cancelled before move-in");

        snapshot.Status.Should().Be(TruthSurfaceStatus.Voided);
        // Append-only: the sealed proof/content survive the void for the audit trail.
        snapshot.Proof.Should().NotBeNull();
        snapshot.Hash.Should().Be("h");
    }
}
