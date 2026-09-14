#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply identity.screening_orders via `dotnet ef`.
    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.

.PARAMETER SkipAdd
    Skip "migrations add" and only apply existing migrations.

.PARAMETER SkipUpdate
    Only generate the migration; do not apply it to the database.

.PARAMETER Name
    Migration name (default: AddScreeningOrders).
#>

[CmdletBinding()]
param(
    [switch] $SkipAdd,
    [switch] $SkipUpdate,
    [string] $Name = "AddScreeningOrders"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/IdentityAndVerification"

Write-Host "Tenant screening — EF Core migration (IdentityDbContext)" -ForegroundColor Cyan
Write-Host "Startup project : $startup" -ForegroundColor DarkGray
Write-Host ""

Write-Host "Building IdentityAndVerification..." -ForegroundColor DarkGray
dotnet build $project --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching migrations." }
Write-Host ""

if (-not $SkipAdd) {
    Write-Host "Adding IdentityDbContext migration $Name..." -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project $project `
        --startup-project $startup `
        --context IdentityDbContext `
        --output-dir Migrations

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef migrations add failed."
    }

    Write-Host "Rebuilding IdentityAndVerification with the new migration..." -ForegroundColor DarkGray
    dotnet build $project --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Build failed after migrations add." }
    Write-Host ""
}

if (-not $SkipUpdate) {
    Write-Host "Applying IdentityDbContext migrations..." -ForegroundColor Cyan
    $devSettingsPath = Join-Path $startup "appsettings.Development.json"
    $connection = $null
    if (Test-Path $devSettingsPath) {
        $connection = (Get-Content $devSettingsPath -Raw | ConvertFrom-Json).ConnectionStrings.Default
    }

    if ($connection) {
        dotnet ef database update `
            --project $project `
            --startup-project $startup `
            --context IdentityDbContext `
            --connection $connection
    }
    else {
        dotnet ef database update `
            --project $project `
            --startup-project $startup `
            --context IdentityDbContext
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef database update failed."
    }
}

Write-Host "Done. identity.screening_orders is ready." -ForegroundColor Green
