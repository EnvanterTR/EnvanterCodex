$ErrorActionPreference = "Stop"
$windowsRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scripts = @(
    "send-backup-alert.ps1",
    "run-scheduled-backup.ps1",
    "check-backup-health.ps1",
    "configure-backup-schedule.ps1",
    "publish.ps1"
) | ForEach-Object { Join-Path $windowsRoot $_ }
foreach ($scriptPath in $scripts) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath,[ref]$tokens,[ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) { throw "PowerShell parse failed for $($scriptPath): $($parseErrors[0].Message)" }
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-alert-check-" + [guid]::NewGuid().ToString("N"))
$secretDirectory = Join-Path $tempRoot "backup-secrets"
$secretPath = Join-Path $secretDirectory "backup-alert-webhook.dpapi"
$alertScript = Join-Path $windowsRoot "send-backup-alert.ps1"
$wrapperSource = Join-Path $windowsRoot "run-scheduled-backup.ps1"
$healthScript = Join-Path $windowsRoot "check-backup-health.ps1"
$capturedRequests = @()

function global:Invoke-RestMethod {
    param(
        [string]$Method,
        [Uri]$Uri,
        [string]$ContentType,
        [string]$Body,
        [int]$TimeoutSec,
        [int]$MaximumRedirection,
        [switch]$UseBasicParsing
    )
    $global:EnvanterTRAlertCapturedRequests += [pscustomobject]@{
        Method = $Method; Uri = $Uri; ContentType = $ContentType; Body = $Body
        TimeoutSec = $TimeoutSec; MaximumRedirection = $MaximumRedirection; UseBasicParsing = $UseBasicParsing.IsPresent
    }
    return [pscustomobject]@{ accepted = $true }
}

function Save-WebhookSecret([string]$Path,[string]$Value) {
    Add-Type -AssemblyName System.Security
    New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
    $plain = [Text.Encoding]::Unicode.GetBytes($Value)
    $protected = $null
    try {
        $protected = [Security.Cryptography.ProtectedData]::Protect(
            $plain,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        [IO.File]::WriteAllBytes($Path,$protected)
    } finally {
        [Array]::Clear($plain,0,$plain.Length)
        if ($protected) { [Array]::Clear($protected,0,$protected.Length) }
    }
}

try {
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    $global:EnvanterTRAlertCapturedRequests = @()
    $secretValue = "https://hooks.example.invalid/private-token-for-test"
    Save-WebhookSecret $secretPath $secretValue
    & $alertScript -DataRoot $tempRoot -Event "backup_health_failed" -Source "health-check-fixture"
    if ($global:EnvanterTRAlertCapturedRequests.Count -ne 1) { throw "A configured health failure did not produce exactly one webhook request." }
    $healthRequest = $global:EnvanterTRAlertCapturedRequests[0]
    $healthPayload = $healthRequest.Body | ConvertFrom-Json
    if ($healthRequest.Method -ne "Post" -or $healthRequest.ContentType -ne "application/json" -or
        $healthRequest.MaximumRedirection -ne 0 -or $healthPayload.event -ne "backup_health_failed" -or
        $healthPayload.source -ne "health-check-fixture") { throw "The health failure webhook request did not have the expected safe JSON fields." }
    $alertLog = Get-Content -LiteralPath (Join-Path $tempRoot "backup-alert.log") -Raw
    if ($alertLog.Contains($secretValue)) { throw "The protected webhook URL appeared in the alert log." }

    Save-WebhookSecret $secretPath "http://hooks.example.invalid/must-not-send"
    & $alertScript -DataRoot $tempRoot -Event "backup_health_failed" -Source "invalid-url-fixture"
    if ($global:EnvanterTRAlertCapturedRequests.Count -ne 1) { throw "A non-HTTPS webhook URL was sent." }

    $disabledRoot = Join-Path $tempRoot "disabled"
    & $alertScript -DataRoot $disabledRoot -Event "backup_health_failed" -Source "disabled-fixture"
    if ($global:EnvanterTRAlertCapturedRequests.Count -ne 1) { throw "A disabled webhook attempted network delivery." }

    $wrapperRoot = Join-Path $tempRoot "wrapper-fixture"
    New-Item -ItemType Directory -Path $wrapperRoot -Force | Out-Null
    Copy-Item -LiteralPath $wrapperSource -Destination (Join-Path $wrapperRoot "run-scheduled-backup.ps1")
    Copy-Item -LiteralPath $alertScript -Destination (Join-Path $wrapperRoot "send-backup-alert.ps1")
    @'
param([string]$InstallRoot,[string]$DataRoot,[string]$ServiceName,[string]$OutputDirectory,[string]$PgBinDirectory,[string]$PassphraseFile,[string]$MirrorDirectory,[int]$RetentionDays)
throw "Synthetic scheduled backup failure."
'@ | Set-Content -LiteralPath (Join-Path $wrapperRoot "backup.ps1") -Encoding UTF8
    Save-WebhookSecret $secretPath $secretValue
    $wrapperFailed = $false
    try {
        & (Join-Path $wrapperRoot "run-scheduled-backup.ps1") -DataRoot $tempRoot -PassphraseFile "fixture.dpapi" -TaskName "scheduled-fixture"
    } catch { $wrapperFailed = $true }
    if (-not $wrapperFailed -or $global:EnvanterTRAlertCapturedRequests.Count -ne 2) {
        throw "A scheduled backup failure was not both reported and returned as a failed task."
    }
    $backupPayload = $global:EnvanterTRAlertCapturedRequests[1].Body | ConvertFrom-Json
    if ($backupPayload.event -ne "scheduled_backup_failed" -or $backupPayload.source -ne "scheduled-fixture") {
        throw "The scheduled backup failure event was incorrectly classified."
    }

    $healthFailed = $false
    try {
        & $healthScript -DataRoot $tempRoot -OutputDirectory (Join-Path $tempRoot "missing-backups") `
            -HealthLogPath (Join-Path $tempRoot "fixture-health.log")
    } catch { $healthFailed = $true }
    if (-not $healthFailed -or $global:EnvanterTRAlertCapturedRequests.Count -ne 3) {
        throw "A stale/missing backup health failure was not both reported and returned as a failed task."
    }
    $healthPayload = $global:EnvanterTRAlertCapturedRequests[2].Body | ConvertFrom-Json
    if ($healthPayload.event -ne "backup_health_failed") { throw "The health failure event was incorrectly classified." }

    $blockedLogParent = Join-Path $tempRoot "not-a-directory"
    Set-Content -LiteralPath $blockedLogParent -Value "fixture" -Encoding UTF8
    $healthLogFailureReturned = $false
    try {
        & $healthScript -DataRoot $tempRoot -OutputDirectory (Join-Path $tempRoot "missing-backups") `
            -HealthLogPath (Join-Path $blockedLogParent "health.log")
    } catch { $healthLogFailureReturned = $true }
    if (-not $healthLogFailureReturned -or $global:EnvanterTRAlertCapturedRequests.Count -ne 4) {
        throw "A backup health failure did not alert when its local health log was unwritable."
    }

    Write-Output "Backup webhook acceptance passed: DPAPI secret, HTTPS-only policy, redirection disabled, no-secret silence, scheduled backup failure, health failure, alert despite health-log failure, task failure propagation, and secret-free log."
} finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item Function:\Invoke-RestMethod -ErrorAction SilentlyContinue
    Remove-Variable EnvanterTRAlertCapturedRequests -Scope Global -ErrorAction SilentlyContinue
}
