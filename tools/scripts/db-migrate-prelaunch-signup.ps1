#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for the pre-launch / founding-partner
    sign-up flow.

    One DbContext changes:
      - AuthDbContext (auth schema):
          adds sign-up metadata columns to AspNetUsers —
            CompanyName, SignupType, PortfolioSize, HousingType,
            PlacementsPerYear (all nullable strings) and
            IsPreLaunchSignup (bool, default false).

    The pre-launch feature flag itself ("prelaunch.enabled") is a platform
    setting seeded idempotently at application start-up, so it does NOT require
    a migration.

    All new columns are nullable / defaulted, so applying this migration is
    behaviour-neutral for existing accounts.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddPreLaunchSignupFields).

.EXAMPLE
    pwsh tools/scripts/db-migrate-prelaunch-signup.ps1

.EXAMPLE
    # Re-apply only (e.g. on another environment), migration already committed
    pwsh tools/scripts/db-migrate-prelaunch-signup.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddPreLaunchSignupFields"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Auth"
$context = "AuthDbContext"

Write-Host "Pre-launch sign-up fields — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host "Migrations proj : $project" -ForegroundColor DarkGray
Write-Host "DbContext       : $context" -ForegroundColor DarkGray
Write-Host ""

# Build once so the EF tooling reads the latest model snapshot.
Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "→ add $Name :: $context" -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project         $project `
        --startup-project $startup `
        --context         $context
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed for $context. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: $context" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $project `
        --startup-project $startup `
        --context         $context
    if ($LASTEXITCODE -ne 0) { throw "database update failed for $context. Aborting." }
    Write-Host ""
}

Write-Host "Pre-launch sign-up migration complete." -ForegroundColor Green
