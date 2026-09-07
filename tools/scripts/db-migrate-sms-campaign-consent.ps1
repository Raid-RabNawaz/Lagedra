#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration for A2P SMS campaign consent.

.DESCRIPTION
    Adds `sms_consents` on NotificationDbContext (notifications schema).
    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipApply
    Generate the migration without running `database update`.
#>

param(
    [switch]$SkipApply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/Notifications"
$name = "AddSmsCampaignConsent"

Write-Host "Adding $name to NotificationDbContext..." -ForegroundColor Cyan
dotnet ef migrations add $name `
    --project $project `
    --startup-project $startup `
    --context NotificationDbContext `
    --output-dir Migrations

if ($LASTEXITCODE -ne 0) { throw "migrations add failed for NotificationDbContext." }

if ($SkipApply) {
    Write-Host "Migration generated. Re-run without -SkipApply, or use tools/scripts/db-migrate.ps1, to apply." -ForegroundColor Yellow
    exit 0
}

Write-Host "Applying NotificationDbContext..." -ForegroundColor Cyan
dotnet ef database update `
    --project $project `
    --startup-project $startup `
    --context NotificationDbContext

if ($LASTEXITCODE -ne 0) { throw "database update failed for NotificationDbContext." }

Write-Host "Done." -ForegroundColor Green
