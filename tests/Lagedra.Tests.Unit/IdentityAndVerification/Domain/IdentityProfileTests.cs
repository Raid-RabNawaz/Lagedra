using System;
using FluentAssertions;
using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Xunit;

namespace Lagedra.Tests.Unit.IdentityAndVerification.Domain;

public class IdentityProfileTests
{
    // Request bodies deserialize dates with Kind=Unspecified; Npgsql refuses
    // to write those to timestamptz columns, which 500'd every manual-KYC
    // submit that included a date of birth (live incident, Aug 11).
    private static readonly DateTime UnspecifiedDob = new(2000, 10, 24, 0, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void Create_normalizes_date_of_birth_to_utc_kind()
    {
        var profile = IdentityProfile.Create(Guid.NewGuid(), "Ada", "Lovelace", UnspecifiedDob);

        profile.DateOfBirth.Should().NotBeNull();
        profile.DateOfBirth!.Value.Kind.Should().Be(DateTimeKind.Utc);
        profile.DateOfBirth.Value.Date.Should().Be(new DateTime(2000, 10, 24, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void UpdatePersonalInfo_normalizes_date_of_birth_to_utc_kind()
    {
        var profile = IdentityProfile.Create(Guid.NewGuid(), null, null, null);

        profile.UpdatePersonalInfo("Ada", "Lovelace", UnspecifiedDob);

        profile.DateOfBirth.Should().NotBeNull();
        profile.DateOfBirth!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_keeps_null_date_of_birth()
    {
        var profile = IdentityProfile.Create(Guid.NewGuid(), null, null, null);

        profile.DateOfBirth.Should().BeNull();
        profile.HostKind.Should().Be(HostKind.Individual);
    }

    [Fact]
    public void TrySetHostKind_switches_to_a_company_until_review_starts()
    {
        var profile = IdentityProfile.Create(Guid.NewGuid(), null, null, null);

        profile.TrySetHostKind(HostKind.PropertyManagementCompany).Should().BeTrue();
        profile.HostKind.Should().Be(HostKind.PropertyManagementCompany);

        profile.StartVerification();
        profile.RequireManualReview();

        profile.TrySetHostKind(HostKind.Individual).Should().BeFalse();
        profile.HostKind.Should().Be(HostKind.PropertyManagementCompany);
        profile.Status.Should().Be(VerificationStatus.ManualReviewRequired);
    }
}
