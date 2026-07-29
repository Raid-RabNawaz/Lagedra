# Sends one test email via SendGrid Web API so the dashboard
# "Verify Integration" step can detect traffic.
# Usage (PowerShell):
#   $env:SENDGRID_API_KEY = "SG...."
#   $env:SENDGRID_TO = "you@example.com"   # optional
#   pwsh tools/scripts/sendgrid-verify-integration.ps1

param(
    [string]$ApiKey = $env:SENDGRID_API_KEY,
    [string]$From = $(if ($env:SENDGRID_FROM) { $env:SENDGRID_FROM } else { "info@lagedra.com" }),
    [string]$To = $(if ($env:SENDGRID_TO) { $env:SENDGRID_TO } else { "bfine@lagedra.com" })
)

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Error "Set SENDGRID_API_KEY (or pass -ApiKey) to your SG.… key from the SendGrid console."
    exit 1
}

$body = @{
    personalizations = @(@{ to = @(@{ email = $To }) })
    from             = @{ email = $From; name = "Lagedra" }
    subject          = "Sending with SendGrid is Fun"
    content          = @(
        @{ type = "text/plain"; value = "and easy to do anywhere, even with C#" }
        @{ type = "text/html"; value = "<strong>and easy to do anywhere, even with C#</strong>" }
    )
} | ConvertTo-Json -Depth 6

try {
    $response = Invoke-WebRequest `
        -Uri "https://api.sendgrid.com/v3/mail/send" `
        -Method POST `
        -Headers @{ Authorization = "Bearer $ApiKey"; "Content-Type" = "application/json" } `
        -Body $body `
        -UseBasicParsing

    Write-Host "OK — SendGrid accepted the message (HTTP $($response.StatusCode))."
    Write-Host "Message-Id: $($response.Headers['X-Message-Id'])"
    Write-Host "In SendGrid, check 'I've integrated the code above' then click 'Next: Verify Integration'."
}
catch {
    Write-Host "FAILED: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message }
    exit 1
}
