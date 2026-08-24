#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate and apply the listing owner / property-manager + broker-clause migration.
    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"
$project = Join-Path $root "src/Lagedra.Modules/ListingAndLocation"

Write-Host "Adding ListingsDbContext migration AddListingManagementAndBrokerClause..." -ForegroundColor Cyan
dotnet ef migrations add AddListingManagementAndBrokerClause `
    --project $project `
    --startup-project $startup `
    --context ListingsDbContext `
    --output-dir Migrations

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef migrations add failed."
}

Write-Host "Applying ListingsDbContext migrations..." -ForegroundColor Cyan
dotnet ef database update `
    --project $project `
    --startup-project $startup `
    --context ListingsDbContext

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef database update failed."
}

Write-Host "Done. Existing listings default to Owner with the broker clause off." -ForegroundColor Green
