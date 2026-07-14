#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that changes the unique index on
    notifications.notification_templates from TemplateId alone to
    (TemplateId, Channel), so Email + SMS templates can share a TemplateId.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations (database update).

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: NotificationTemplateChannelUnique).

.EXAMPLE
    pwsh tools/scripts/db-migrate-notification-template-channel-unique.ps1

.EXAMPLE
    pwsh tools/scripts/db-migrate-notification-template-channel-unique.ps1 -SkipAdd
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "NotificationTemplateChannelUnique"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/Notifications"
$context = "NotificationDbContext"

Write-Host "Notification template (TemplateId, Channel) unique index — EF Core migration" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host "Project         : $project" -ForegroundColor DarkGray
Write-Host ""

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
    if ($LASTEXITCODE -ne 0) { throw "database update failed for $context." }
    Write-Host ""
}

Write-Host "Notification template channel-unique migration complete." -ForegroundColor Green
