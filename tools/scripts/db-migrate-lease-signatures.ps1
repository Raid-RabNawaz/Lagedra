#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply EF Core migrations for in-app Lagedra lease signatures.

.DESCRIPTION
    Adds two migrations:
      * AddListingRequireLeaseSignatures (ListingsDbContext) — per-listing
        RequireLeaseSignatures flag (boolean, NOT NULL, default false).
      * AddDealLeaseSignatures (LeaseAgreementDbContext) — deal_lease_signatures
        table recording who signed, when, and the PDF hash they signed.

    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.EXAMPLE
    pwsh tools/scripts/db-migrate-lease-signatures.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-lease-signatures.ps1 -SkipUpdate
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{
        Name    = "AddListingRequireLeaseSignatures"
        Project = "src/Lagedra.Modules/ListingAndLocation"
        Context = "ListingsDbContext"
    },
    @{
        Name    = "AddDealLeaseSignatures"
        Project = "src/Lagedra.Modules/LeaseAgreements"
        Context = "LeaseAgreementDbContext"
    }
)

Write-Host "In-app lease signatures — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ add $($c.Name) :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef migrations add $c.Name `
            --project         $proj `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

if (-not $SkipUpdate) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ database update :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef database update `
            --project         $proj `
            --startup-project $startup `
            --context         $c.Context
        if ($LASTEXITCODE -ne 0) { throw "database update failed for $($c.Context). Aborting." }
        Write-Host ""
    }
}

Write-Host "In-app lease signatures migration complete." -ForegroundColor Green
