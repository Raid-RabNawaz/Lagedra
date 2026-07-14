#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate EF Core migrations for Lease Agreements + related profile/listing fields.
    Does NOT hand-write migration C# — uses `dotnet ef migrations add`.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."
$startup = Join-Path $root "src/Lagedra.ApiGateway"

Write-Host "Adding LeaseAgreement InitialCreate migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreateLeaseAgreements `
    --project (Join-Path $root "src/Lagedra.Modules/LeaseAgreements") `
    --startup-project $startup `
    --context LeaseAgreementDbContext `
    --output-dir Migrations

Write-Host "Adding Auth lease-profile fields migration..." -ForegroundColor Cyan
dotnet ef migrations add AddLeasePartyProfileFields `
    --project (Join-Path $root "src/Lagedra.Auth") `
    --startup-project $startup `
    --context AuthDbContext `
    --output-dir Infrastructure/Persistence/Migrations

Write-Host "Adding Listing lease terms migration..." -ForegroundColor Cyan
dotnet ef migrations add AddListingLeaseTerms `
    --project (Join-Path $root "src/Lagedra.Modules/ListingAndLocation") `
    --startup-project $startup `
    --context ListingsDbContext `
    --output-dir Migrations

Write-Host "Done. Run tools/scripts/db-migrate.ps1 to apply." -ForegroundColor Green
