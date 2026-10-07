param(
    [Parameter(Mandatory = $true)][string]$DataRoot,
    [Parameter(Mandatory = $true)][ValidateSet("scheduled_backup_failed","backup_health_failed")][string]$Event,
    [Parameter(Mandatory = $true)][string]$Source
)

$ErrorActionPreference = "Stop"
$secretPath = Join-Path (Join-Path $DataRoot "backup-secrets") "backup-alert-webhook.dpapi"
$logPath = Join-Path $DataRoot "backup-alert.log"
$sourceLabel = (($Source -replace '[\r\n]+',' ') -replace '[\x00-\x1f]',' ')
if ($sourceLabel.Length -gt 120) { $sourceLabel = $sourceLabel.Substring(0,120) }

function Add-AlertLog([string]$Status,[string]$Message) {
    try {
        New-Item -ItemType Directory -Path (Split-Path -Parent $logPath) -Force | Out-Null
        $line = "{0} {1} {2}" -f [DateTimeOffset]::Now.ToString("o"),$Status,$Message
        [IO.File]::AppendAllText($logPath,$line + [Environment]::NewLine,[Text.Encoding]::UTF8)
    } catch { Write-Warning "Backup alert status could not be written to its protected log." }
}

if (-not (Test-Path -LiteralPath $secretPath -PathType Leaf)) {
    Add-AlertLog "SKIP" "source=$sourceLabel; no webhook is configured."
    return
}

$protectedBytes = $null
$plainBytes = $null
$webhookUrl = $null
try {
    Add-Type -AssemblyName System.Security
    $protectedBytes = [IO.File]::ReadAllBytes($secretPath)
    $plainBytes = [Security.Cryptography.ProtectedData]::Unprotect(
        $protectedBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
    $webhookUrl = [Text.Encoding]::Unicode.GetString($plainBytes)

    $uri = $null
    if ($webhookUrl.Length -gt 4096 -or $webhookUrl -match '[\r\n\t ]' -or
        -not [Uri]::TryCreate($webhookUrl,[UriKind]::Absolute,[ref]$uri) -or
        $uri.Scheme -ne "https" -or [string]::IsNullOrWhiteSpace($uri.Host) -or
        -not [string]::IsNullOrEmpty($uri.UserInfo)) {
        Add-AlertLog "FAIL" "source=$sourceLabel; the protected webhook URL is invalid or is not HTTPS."
        return
    }

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $payload = @{
        event = $Event
        source = $sourceLabel
        host = [Environment]::MachineName
        observedAtUtc = [DateTimeOffset]::UtcNow.ToString("o")
    } | ConvertTo-Json -Compress
    $delivered = $false
    for ($attempt = 0; $attempt -lt 3; $attempt++) {
        try {
            $null = Invoke-RestMethod -Method Post -Uri $uri.AbsoluteUri -ContentType "application/json" `
                -Body $payload -TimeoutSec 15 -MaximumRedirection 0 -UseBasicParsing
            $delivered = $true
            break
        } catch {
            if ($attempt -lt 2) { Start-Sleep -Seconds ($attempt + 1) }
        }
    }
    if ($delivered) { Add-AlertLog "PASS" "source=$sourceLabel; HTTPS webhook accepted the backup failure notification." }
    else { Add-AlertLog "FAIL" "source=$sourceLabel; HTTPS webhook delivery failed after three attempts." }
} catch {
    Add-AlertLog "FAIL" "source=$sourceLabel; protected secret could not be read or the HTTPS webhook request failed."
} finally {
    if ($protectedBytes) { [Array]::Clear($protectedBytes,0,$protectedBytes.Length) }
    if ($plainBytes) { [Array]::Clear($plainBytes,0,$plainBytes.Length) }
    $webhookUrl = $null
}
