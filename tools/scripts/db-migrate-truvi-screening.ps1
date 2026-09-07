#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the Truvi screening columns on insurance.policy_records.
    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.
#>

param(
    [string] $Name = "AddTruviScreeningToPolicyRecords",
    [switch] $SkipAdd,
    [switch] $SkipUpdate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$apiGateway = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/InsuranceIntegration"

Write-Host "Building InsuranceIntegration..." -ForegroundColor DarkGray
dotnet build $project --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Build failed. Aborting before touching migrations." }

# Startup is InsuranceIntegration (design-time factory) so a running ApiGateway
# process cannot lock the build. Pass the Development connection string explicitly
# because `dotnet ef` sets cwd to the startup project, not ApiGateway.
$devSettingsPath = Join-Path $apiGateway "appsettings.Development.json"
$connection = $null
if (Test-Path $devSettingsPath) {
    $connection = (Get-Content $devSettingsPath -Raw | ConvertFrom-Json).ConnectionStrings.Default
}

if (-not $SkipAdd) {
    Write-Host "Adding InsuranceDbContext migration $Name..." -ForegroundColor Cyan
    dotnet ef migrations add $Name `
        --project $project `
        --startup-project $project `
        --context InsuranceDbContext `
        --output-dir Migrations

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef migrations add failed."
    }

    Write-Host "Rebuilding InsuranceIntegration with the new migration..." -ForegroundColor DarkGray
    dotnet build $project --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Build failed after migrations add." }
}

if (-not $SkipUpdate) {
    Write-Host "Applying InsuranceDbContext migrations..." -ForegroundColor Cyan
    if ($connection) {
        dotnet ef database update `
            --project $project `
            --startup-project $project `
            --context InsuranceDbContext `
            --connection $connection
    }
    else {
        dotnet ef database update `
            --project $project `
            --startup-project $project `
            --context InsuranceDbContext
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef database update failed."
    }
}

Write-Host "Done. policy_records now stores Truvi verification id and screening status." -ForegroundColor Green
