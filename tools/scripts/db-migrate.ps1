#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run EF Core migrations for all DbContexts.
    Runs inside the API container (or locally with dotnet ef if SDK available).
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."

# Mapping of project path -> DbContext class name
# Add entries here as new modules with DbContexts are created
$migrations = @(
    @{ Project = "src/Lagedra.Auth";                                       Context = "AuthDbContext" }
    @{ Project = "src/Lagedra.Modules/ActivationAndBilling";               Context = "BillingDbContext" }
    @{ Project = "src/Lagedra.Modules/IdentityAndVerification";            Context = "IdentityVerificationDbContext" }
    @{ Project = "src/Lagedra.Modules/ListingAndLocation";                 Context = "ListingDbContext" }
    @{ Project = "src/Lagedra.Modules/StructuredInquiry";                  Context = "InquiryDbContext" }
    @{ Project = "src/Lagedra.Modules/VerificationAndRisk";                Context = "VerificationDbContext" }
    @{ Project = "src/Lagedra.Modules/InsuranceIntegration";               Context = "InsuranceDbContext" }
    @{ Project = "src/Lagedra.Modules/ComplianceMonitoring";               Context = "ComplianceDbContext" }
    @{ Project = "src/Lagedra.Modules/Arbitration";                        Context = "ArbitrationDbContext" }
    @{ Project = "src/Lagedra.Modules/JurisdictionPacks";                  Context = "JurisdictionDbContext" }
    @{ Project = "src/Lagedra.Modules/Evidence";                           Context = "EvidenceDbContext" }
    @{ Project = "src/Lagedra.Modules/Privacy";                            Context = "PrivacyDbContext" }
    @{ Project = "src/Lagedra.Modules/Notifications";                      Context = "NotificationDbContext" }
    @{ Project = "src/Lagedra.Modules/AntiAbuseAndIntegrity";              Context = "IntegrityDbContext" }
    @{ Project = "src/Lagedra.Modules/ContentManagement";                  Context = "ContentDbContext" }
    @{ Project = "src/Lagedra.TruthSurface";                               Context = "TruthSurfaceDbContext" }
    @{ Project = "src/Lagedra.Compliance";                                  Context = "ComplianceLedgerDbContext" }
)

foreach ($m in $migrations) {
    $proj = Join-Path $root $m.Project
    if (Test-Path $proj) {
        Write-Host "Migrating $($m.Context)..." -ForegroundColor Cyan
        dotnet ef database update `
            --project $proj `
            --startup-project (Join-Path $root "src/Lagedra.ApiGateway") `
            --context $m.Context
    } else {
        Write-Host "Skipping $($m.Context) — project not yet created." -ForegroundColor DarkGray
    }
}

Write-Host "All migrations complete." -ForegroundColor Green
