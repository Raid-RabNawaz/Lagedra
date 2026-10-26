using System;
using System.Collections.Generic;
using FluentAssertions;
using Lagedra.Modules.IdentityAndVerification.Application;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.SharedKernel.Domain;
using Xunit;

namespace Lagedra.Tests.Unit.IdentityAndVerification.Application;

public class HostVerificationRulesTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Individual_requires_id_front_and_selfie()
    {
        var error = HostVerificationRules.ValidateSubmission(
            HostKind.Individual,
            new List<KycDocumentType> { KycDocumentType.IdFront },
            new DateTime(1990, 1, 1),
            null,
            null,
            null,
            UtcNow);

        error!.Code.Should().Be("Identity.Kyc.MissingSelfie");
    }

    [Fact]
    public void Individual_rejects_an_underage_date_of_birth()
    {
        var error = HostVerificationRules.ValidateSubmission(
            HostKind.Individual,
            new List<KycDocumentType> { KycDocumentType.IdFront, KycDocumentType.Selfie },
            UtcNow.AddYears(-17),
            null,
            null,
            null,
            UtcNow);

        error!.Code.Should().Be("Identity.Kyc.Underage");
    }

    [Fact]
    public void Company_accepts_a_business_registration_without_an_id()
    {
        var error = HostVerificationRules.ValidateSubmission(
            HostKind.PropertyManagementCompany,
            new List<KycDocumentType> { KycDocumentType.BusinessRegistration },
            null,
            "Northwind Property Management LLC",
            "C1234567",
            "California, United States",
            UtcNow);

        error.Should().BeNull();
    }

    [Fact]
    public void Company_does_not_ask_for_a_personal_id()
    {
        var upload = HostVerificationRules.ValidateUpload(
            HostKind.PropertyManagementCompany,
            KycDocumentType.IdFront);

        upload!.Code.Should().Be("Identity.Kyc.PersonalIdNotRequired");
    }

    [Fact]
    public void Individual_cannot_upload_a_business_registration()
    {
        var upload = HostVerificationRules.ValidateUpload(
            HostKind.Individual,
            KycDocumentType.BusinessRegistration);

        upload!.Code.Should().Be("Identity.Kyc.BusinessDocumentNotAllowed");
    }
}
