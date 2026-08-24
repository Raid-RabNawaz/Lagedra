#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the deal-application owner tenancy consent migration.
    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ActivationAndBilling"

Write-Host "Adding BillingDbContext migration AddOwnerTenancyConsentToDealApplication..." -ForegroundColor Cyan
dotnet ef migrations add AddOwnerTenancyConsentToDealApplication `
    --project $project `
    --startup-project $startup `
    --context BillingDbContext `
    --output-dir Migrations

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef migrations add failed."
}

Write-Host "Applying BillingDbContext migrations..." -ForegroundColor Cyan
dotnet ef database update `
    --project $project `
    --startup-project $startup `
    --context BillingDbContext

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef database update failed."
}

Write-Host "Done. Existing applications default to owner consent not required." -ForegroundColor Green
