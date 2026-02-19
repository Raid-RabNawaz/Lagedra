#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Start all Lagedra services locally via Docker Compose, then migrate and seed.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\..\.."

Push-Location $root

Write-Host "Starting Lagedra services..." -ForegroundColor Cyan
docker compose up -d --build

Write-Host "Waiting for postgres to be healthy..." -ForegroundColor Cyan
$retries = 30
do {
    $health = docker inspect --format "{{.State.Health.Status}}" lagedra-pg 2>$null
    if ($health -eq "healthy") { break }
    Start-Sleep -Seconds 2
    $retries--
} while ($retries -gt 0)

if ($retries -eq 0) {
    Write-Host "PostgreSQL did not become healthy in time." -ForegroundColor Red
    exit 1
}

Write-Host "Running EF Core migrations..." -ForegroundColor Cyan
& "$PSScriptRoot\db-migrate.ps1"

Write-Host "Seeding reference data..." -ForegroundColor Cyan
& "$PSScriptRoot\db-seed.ps1"

Write-Host ""
Write-Host "All services are up:" -ForegroundColor Green
Write-Host "  API (Swagger)   -> http://localhost:8080/swagger"
Write-Host "  Web app         -> http://localhost:3000"
Write-Host "  Admin app       -> http://localhost:3001"
Write-Host "  Marketing site  -> http://localhost:3002"
Write-Host "  MinIO console   -> http://localhost:9001"

Pop-Location
