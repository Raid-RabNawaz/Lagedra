param(
    [Parameter(Mandatory=$true)][string]$InputPath,
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [Parameter(Mandatory=$true)][string]$ParamArn
)

$json = Get-Content -Raw -Path $InputPath | ConvertFrom-Json

$strip = @(
    'taskDefinitionArn','revision','status','requiresAttributes',
    'compatibilities','registeredAt','registeredBy','deregisteredAt'
)
foreach ($prop in $strip) {
    if ($json.PSObject.Properties.Name -contains $prop) {
        $json.PSObject.Properties.Remove($prop)
    }
}

foreach ($container in $json.containerDefinitions) {
    $secrets = @()
    if ($container.secrets) { $secrets = @($container.secrets) }

    $existing = $secrets | Where-Object { $_.name -eq 'Encryption__Key' }
    if ($existing) {
        $existing.valueFrom = $ParamArn
    } else {
        $secrets += [pscustomobject]@{ name = 'Encryption__Key'; valueFrom = $ParamArn }
    }

    $container.secrets = $secrets
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Resolve-Path -LiteralPath (Split-Path $OutputPath)).Path + [System.IO.Path]::DirectorySeparatorChar + (Split-Path $OutputPath -Leaf), ($json | ConvertTo-Json -Depth 50), $utf8NoBom)
Write-Host "Wrote $OutputPath"
