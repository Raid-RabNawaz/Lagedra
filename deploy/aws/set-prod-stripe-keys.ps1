# Writes the live Stripe trio to /lagedra/prod/* only.
# Refuses sandbox keys. Does not touch /lagedra/staging/* .
#
# Use the live webhook signing secret from a destination whose URL is
# https://api.lagedra.com/v1/webhooks/stripe — not the sandbox whsec_.
#
# Usage (from repo root):
#   .\deploy\aws\set-prod-stripe-keys.ps1 `
#     -PublishableKey "pk_live_..." `
#     -SecretKey "sk_live_..." `
#     -WebhookSecret "whsec_..."
#
# Optional: retarget running prod API + worker and restart them
#   .\deploy\aws\set-prod-stripe-keys.ps1 ... -Apply

param(
    [Parameter(Mandatory = $true)]
    [string] $PublishableKey,

    [Parameter(Mandatory = $true)]
    [string] $SecretKey,

    [Parameter(Mandatory = $true)]
    [string] $WebhookSecret,

    [switch] $Apply,

    [string] $Region = "us-west-1",
    [string] $Prefix = "/lagedra/prod",
    [string] $Cluster = "lagedra-prod",
    [string[]] $Services = @("lagedra-api", "lagedra-worker")
)

$ErrorActionPreference = "Stop"

function Assert-Prefix([string] $Value, [string] $Expected, [string] $Label) {
    if (-not $Value.StartsWith($Expected)) {
        throw "$Label must start with $Expected (refusing sandbox / mismatched keys on live)."
    }
}

Assert-Prefix $PublishableKey "pk_live_" "Publishable key"
Assert-Prefix $SecretKey "sk_live_" "Secret key"
Assert-Prefix $WebhookSecret "whsec_" "Webhook secret"

$parameters = @(
    @{ Name = "stripe-publishable-key"; Value = $PublishableKey },
    @{ Name = "stripe-secret-key"; Value = $SecretKey },
    @{ Name = "stripe-webhook-secret"; Value = $WebhookSecret }
)

foreach ($parameter in $parameters) {
    $name = "$Prefix/$($parameter.Name)"
    aws ssm put-parameter `
        --name $name `
        --value $parameter.Value `
        --type SecureString `
        --overwrite `
        --region $Region | Out-Null
    Write-Host "Wrote $name"
}

if (-not $Apply) {
    Write-Host ""
    Write-Host "SSM is updated. Running prod tasks still have the previous secret/webhook until they restart."
    Write-Host "Re-run with -Apply to retarget lagedra-api and lagedra-worker and force new deployments."
    Write-Host "Also set the same pk_live_ key on the production web build as VITE_STRIPE_PUBLISHABLE_KEY"
    Write-Host "(GitHub secret STRIPE_PUBLISHABLE_KEY) and redeploy the frontend."
    exit 0
}

$accountId = aws sts get-caller-identity --query Account --output text
$arnPrefix = "arn:aws:ssm:${Region}:${accountId}:parameter${Prefix}"
$secretMap = @{
    "Stripe__PublishableKey" = "$arnPrefix/stripe-publishable-key"
    "Stripe__SecretKey"      = "$arnPrefix/stripe-secret-key"
    "Stripe__WebhookSecret"  = "$arnPrefix/stripe-webhook-secret"
}

function Update-StripeTaskDefinition([string] $ServiceName) {
    $taskArn = aws ecs describe-services `
        --cluster $Cluster `
        --services $ServiceName `
        --region $Region `
        --query "services[0].taskDefinition" `
        --output text

    if (-not $taskArn -or $taskArn -eq "None") {
        throw "Could not find ECS service $ServiceName on cluster $Cluster. SSM keys are stored."
    }

    $raw = aws ecs describe-task-definition `
        --task-definition $taskArn `
        --region $Region `
        --query taskDefinition `
        --output json | ConvertFrom-Json

    $container = $raw.containerDefinitions | Select-Object -First 1
    if (-not $container) {
        throw "Task definition $taskArn has no container."
    }

    $container.environment = @(
        $container.environment | Where-Object { $_.name -ne "Stripe__PublishableKey" }
    )

    $kept = @($container.secrets | Where-Object { -not $secretMap.ContainsKey($_.name) })
    foreach ($entry in $secretMap.GetEnumerator()) {
        $kept += [pscustomobject]@{ name = $entry.Key; valueFrom = $entry.Value }
    }
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

    $tempFile = Join-Path $env:TEMP "lagedra-$ServiceName-stripe.json"
    $register | ConvertTo-Json -Depth 20 | Set-Content -Path $tempFile -Encoding utf8

    aws ecs register-task-definition `
        --cli-input-json "file://$tempFile" `
        --region $Region | Out-Null

    aws ecs update-service `
        --cluster $Cluster `
        --service $ServiceName `
        --task-definition $raw.family `
        --force-new-deployment `
        --region $Region | Out-Null

    Remove-Item $tempFile -ErrorAction SilentlyContinue
    Write-Host "Registered a new $($raw.family) revision and forced a deployment of $ServiceName."
}

foreach ($serviceName in $Services) {
    Update-StripeTaskDefinition $serviceName
}

Write-Host "Also set the same pk_live_ key on the production web build as VITE_STRIPE_PUBLISHABLE_KEY"
Write-Host "(GitHub secret STRIPE_PUBLISHABLE_KEY) and redeploy the frontend."
