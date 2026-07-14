#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply EF Core migrations for the Reviews module and review
    platform settings.

    DbContexts:
      - ReviewsDbContext (reviews schema): stay_reviews, stay_review_windows,
        partner_service_reviews
      - PlatformSettingsDbContext: seeds
          review.window_days = 14
          review.reminder_interval_days = 3

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations.

.PARAMETER SkipUpdate
    Only generate migrations; do not apply them.

.PARAMETER Name
    Reviews DbContext migration name (default: InitialCreateReviews).

.PARAMETER SettingsName
    PlatformSettings migration name (default: AddReviewSettings).

.EXAMPLE
    pwsh tools/scripts/db-migrate-reviews.ps1
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "InitialCreateReviews",
    [string]$SettingsName = "AddReviewSettings"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

$contexts = @(
    @{ Project = "src/Lagedra.Modules/Reviews"; Context = "ReviewsDbContext"; MigrationName = $Name }
    @{ Project = "src/Lagedra.Infrastructure"; Context = "PlatformSettingsDbContext"; MigrationName = $SettingsName }
)

Write-Host "Reviews module — EF Core migrations" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    foreach ($c in $contexts) {
        $proj = Join-Path $root $c.Project
        Write-Host "→ add $($c.MigrationName) :: $($c.Context)" -ForegroundColor Cyan
        dotnet ef migrations add $c.MigrationName `
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

Write-Host "Reviews migrations complete." -ForegroundColor Green
