#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migrations for host kind
    (individual vs property-management company).

    Two DbContext changes, both additive:

      - IdentityDbContext:
          identity_profiles gains HostKind (default Individual),
          LegalBusinessName, BusinessRegistrationNumber, BusinessJurisdiction.
          Existing profiles stay individuals, so their ID/KYC path is unchanged.

      - AuthDbContext:
          AspNetUsers gains HostKind (default Individual) so profiles, listings,
          and readiness copy can tell a company from a person.

    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migrations; do not apply them to the database.

.EXAMPLE
    pwsh tools/scripts/db-migrate-host-kind.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-host-kind.ps1 -SkipUpdate
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
        Name    = "AddHostVerificationKind"
        Project = Join-Path $root "src/Lagedra.Modules/IdentityAndVerification"
        Context = "IdentityDbContext"
    },
    @{
        Name    = "AddHostKindToUsers"
        Project = Join-Path $root "src/Lagedra.Auth"
        Context = "AuthDbContext"
    }
)

Write-Host "Host kind — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

foreach ($migration in $contexts) {
    if (-not $SkipAdd) {
        Write-Host "→ add $($migration.Name) :: $($migration.Context)" -ForegroundColor Cyan
        dotnet ef migrations add $migration.Name `
            --project         $migration.Project `
            --startup-project $startup `
            --context         $migration.Context
        if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $($migration.Context). Aborting." }
        Write-Host ""
    }

    if (-not $SkipUpdate) {
        Write-Host "→ database update :: $($migration.Context)" -ForegroundColor Cyan
        dotnet ef database update `
            --project         $migration.Project `
            --startup-project $startup `
            --context         $migration.Context
        if ($LASTEXITCODE -ne 0) { throw "database update failed for $($migration.Context). Aborting." }
        Write-Host ""
    }
}

Write-Host "Host kind migrations complete." -ForegroundColor Green
