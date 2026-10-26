#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the EF Core migration that records whether a member
    has become a host.

    AuthDbContext:
      AspNetUsers.HostingEnabled (bool, default false).

    After the column exists, accounts that joined through the old host door
    (SignupType = Host) are marked hosting-enabled so they keep the
    travelling / hosting switch. New Member signups stay false until they
    choose Become a host.

    Does NOT hand-write the schema change — uses `dotnet ef migrations add`.
    The generated migration also marks existing SignupType = Host accounts
    as hosting-enabled, so they keep the travelling / hosting switch.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply the existing migration.

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it.

.PARAMETER Name
    Migration name (default: AddHostingEnabled).

.EXAMPLE
    pwsh tools/scripts/db-migrate-hosting-enabled.ps1 -SkipUpdate
#>

[CmdletBinding()]
param(
    [switch]$SkipAdd,
    [switch]$SkipUpdate,
    [string]$Name = "AddHostingEnabled"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$proj    = Join-Path $root "src/Lagedra.Auth"

Write-Host "Hosting enabled — EF Core migration (AuthDbContext)" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building solution..." -ForegroundColor DarkGray
dotnet build (Join-Path $root "Lagedra.sln") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching the database." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "→ add $Name :: AuthDbContext" -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project         $proj `
        --startup-project $startup `
        --context         AuthDbContext
    if ($LASTEXITCODE -ne 0) { throw "migrations add failed for AuthDbContext. Aborting." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "→ database update :: AuthDbContext" -ForegroundColor Cyan
    dotnet ef database update `
        --project         $proj `
        --startup-project $startup `
        --context         AuthDbContext
    if ($LASTEXITCODE -ne 0) { throw "database update failed for AuthDbContext. Aborting." }
    Write-Host ""
}

Write-Host "Hosting enabled migration complete." -ForegroundColor Green
Write-Host "Existing SignupType = Host accounts are marked hosting-enabled by the migration." -ForegroundColor DarkGray
