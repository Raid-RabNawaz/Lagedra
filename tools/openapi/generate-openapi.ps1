#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates the Lagedra OpenAPI YAML specification from the running Swagger endpoint.
.DESCRIPTION
    1. Starts the API Gateway in the background
    2. Waits for it to become healthy
    3. Downloads /swagger/v1/swagger.json
    4. Converts JSON to YAML
    5. Saves as tools/openapi/lagedra.openapi.yaml
.NOTES
    Requires: dotnet CLI, PowerShell 7+
    Optional: Install-Module -Name powershell-yaml (for JSON-to-YAML conversion)
#>

param(
    [string]$ApiUrl = "http://localhost:5000",
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path "$scriptDir/../..").Path
$outputDir = "$scriptDir"
$outputJson = "$outputDir/lagedra.openapi.json"
$outputYaml = "$outputDir/lagedra.openapi.yaml"

Write-Host "Fetching OpenAPI spec from $ApiUrl/swagger/v1/swagger.json ..."

$elapsed = 0
$interval = 2
$fetched = $false

while ($elapsed -lt $TimeoutSeconds) {
    try {
        $response = Invoke-RestMethod -Uri "$ApiUrl/swagger/v1/swagger.json" -TimeoutSec 5
        $response | ConvertTo-Json -Depth 100 | Set-Content -Path $outputJson -Encoding UTF8
        $fetched = $true
        break
    } catch {
        Write-Host "  Waiting for API to start ($elapsed s / $TimeoutSeconds s)..."
        Start-Sleep -Seconds $interval
        $elapsed += $interval
    }
}

if (-not $fetched) {
    Write-Error "Could not reach $ApiUrl within $TimeoutSeconds seconds. Make sure the API is running."
    exit 1
}

Write-Host "OpenAPI JSON saved to: $outputJson"

# Convert JSON to YAML if powershell-yaml module is available
if (Get-Module -ListAvailable -Name powershell-yaml) {
    Import-Module powershell-yaml
    $obj = Get-Content $outputJson -Raw | ConvertFrom-Json
    $yaml = ConvertTo-Yaml $obj
    Set-Content -Path $outputYaml -Value $yaml -Encoding UTF8
    Write-Host "OpenAPI YAML saved to: $outputYaml"
} else {
    Write-Host ""
    Write-Host "NOTE: Install 'powershell-yaml' module for automatic YAML conversion:"
    Write-Host "  Install-Module -Name powershell-yaml -Scope CurrentUser"
    Write-Host ""
    Write-Host "For now, you can convert manually:"
    Write-Host "  npx swagger2openapi $outputJson -o $outputYaml --yaml"
}

Write-Host "Done."
