#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply EF Core migrations for host-authored lease agreements.

.DESCRIPTION
    Adds two migrations:
      * AddListingCustomLeaseAgreement (ListingsDbContext) — the listing's lease
        agreement source and the pointer to the host's uploaded document.
      * AddDealLeaseDocumentSource (LeaseAgreementDbContext) — provenance column
        on deal lease documents, plus nullable template identifiers so a
        host-provided document can be stored without a Lagedra template.

    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipApply
    Generate the migrations without running `database update`.
#>

param(
    [switch]$SkipApply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$migrations = @(
    @{
        Name    = "AddListingCustomLeaseAgreement"
        Project = "src/Lagedra.Modules/ListingAndLocation"
        Context = "ListingsDbContext"
        Output  = "Migrations"
    },
    @{
        Name    = "AddDealLeaseDocumentSource"
        Project = "src/Lagedra.Modules/LeaseAgreements"
        Context = "LeaseAgreementDbContext"
        Output  = "Migrations"
    }
)

foreach ($migration in $migrations) {
    $project = Join-Path $root $migration.Project

    Write-Host "Adding $($migration.Name) to $($migration.Context)..." -ForegroundColor Cyan
    dotnet ef migrations add $migration.Name `
        --project $project `
        --startup-project $startup `
        --context $migration.Context `
        --output-dir $migration.Output

    if ($SkipApply) {
        continue
    }

    Write-Host "Applying $($migration.Context)..." -ForegroundColor Cyan
    dotnet ef database update `
        --project $project `
        --startup-project $startup `
        --context $migration.Context
}

if ($SkipApply) {
    Write-Host "Migrations generated. Re-run without -SkipApply, or use tools/scripts/db-migrate.ps1, to apply." -ForegroundColor Yellow
}
else {
    Write-Host "Done." -ForegroundColor Green
}
