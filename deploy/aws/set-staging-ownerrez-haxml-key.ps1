# Writes the OwnerRez HA-XML sandbox key to /lagedra/staging/ownerrez-haxml-key.
# Does not touch /lagedra/prod/* and does not print the key.
#
# Usage (from repo root):
#   .\deploy\aws\set-staging-ownerrez-haxml-key.ps1 -Key "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
#
# Also restarts the running staging API with the HA-XML feed settings:
#   .\deploy\aws\set-staging-ownerrez-haxml-key.ps1 -Key "..." -Apply

param(
    [Parameter(Mandatory = $true)]
    [string] $Key,

    [switch] $Apply,

    [string] $Region = "us-west-1",
    [string] $Prefix = "/lagedra/staging",
    [string] $Cluster = "lagedra-prod",
    [string] $Service = "lagedra-api-staging"
)

$ErrorActionPreference = "Stop"

if ($Key -notmatch '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') {
    throw "Key must be the OwnerRez channel key (a GUID from the partner email)."
}

$parameterName = "$Prefix/ownerrez-haxml-key"
aws ssm put-parameter `
    --name $parameterName `
    --value $Key `
    --type SecureString `
    --overwrite `
    --region $Region | Out-Null
Write-Host "Wrote $parameterName"

if (-not $Apply) {
    Write-Host ""
    Write-Host "SSM is updated. The running staging API will not see the key until you apply."
    Write-Host "Re-run with -Apply to attach Channels__OwnerRez__HaXmlKey and restart $Service."
    exit 0
}

$accountId = aws sts get-caller-identity --query Account --output text
$secretArn = "arn:aws:ssm:${Region}:${accountId}:parameter${parameterName}"

$taskArn = aws ecs describe-services `
    --cluster $Cluster `
    --services $Service `
    --region $Region `
    --query "services[0].taskDefinition" `
    --output text

if (-not $taskArn -or $taskArn -eq "None") {
    throw "Could not find ECS service $Service on cluster $Cluster. SSM is stored; apply the task definition manually."
}

$raw = aws ecs describe-task-definition `
    --task-definition $taskArn `
    --region $Region `
    --query taskDefinition `
    --output json | ConvertFrom-Json

$container = $raw.containerDefinitions | Where-Object { $_.name -eq "api" } | Select-Object -First 1
if (-not $container) {
    throw "Task definition $taskArn has no container named api."
}

$haXmlEnv = [ordered]@{
    "Channels__OwnerRez__OpenApiEnabled" = "false"
    "Channels__OwnerRez__HaXmlBaseUrl"   = "https://faststage.ownerrez.com"
    "Channels__OwnerRez__HaXmlUsername"  = "HaXmlSandbox"
    "Channels__OwnerRez__HaXmlUserAgent" = "Lagedra/1.0 (staging)"
}

$container.environment = @(
    @($container.environment | Where-Object { -not $haXmlEnv.Contains($_.name) })
)
foreach ($entry in $haXmlEnv.GetEnumerator()) {
    $container.environment += [pscustomobject]@{ name = $entry.Key; value = $entry.Value }
}

$kept = @($container.secrets | Where-Object { $_.name -ne "Channels__OwnerRez__HaXmlKey" })
$kept += [pscustomobject]@{ name = "Channels__OwnerRez__HaXmlKey"; valueFrom = $secretArn }
$container.secrets = $kept

$register = [ordered]@{
    family                  = $raw.family
    networkMode             = $raw.networkMode
    requiresCompatibilities = $raw.requiresCompatibilities
    cpu                     = $raw.cpu
    memory                  = $raw.memory
    executionRoleArn        = $raw.executionRoleArn
    taskRoleArn             = $raw.taskRoleArn
    containerDefinitions    = $raw.containerDefinitions
}
if ($raw.volumes) { $register.volumes = $raw.volumes }
if ($raw.runtimePlatform) { $register.runtimePlatform = $raw.runtimePlatform }

$tempFile = Join-Path $env:TEMP "lagedra-api-staging-ownerrez-haxml.json"
$register | ConvertTo-Json -Depth 20 | Set-Content -Path $tempFile -Encoding utf8

aws ecs register-task-definition `
    --cli-input-json "file://$tempFile" `
    --region $Region | Out-Null

aws ecs update-service `
    --cluster $Cluster `
    --service $Service `
    --task-definition $raw.family `
    --force-new-deployment `
    --region $Region | Out-Null

Remove-Item $tempFile -ErrorAction SilentlyContinue
Write-Host "Registered a new $($raw.family) revision and forced a deployment of $Service."
