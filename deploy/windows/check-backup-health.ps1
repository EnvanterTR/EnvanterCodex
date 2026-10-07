param(
    [string]$OutputDirectory = "$env:ProgramData\EnvanterTR\backups",
    [string]$MirrorDirectory,
    [ValidateRange(1,8760)][int]$MaxAgeHours = 36,
    [string]$HealthLogPath = "$env:ProgramData\EnvanterTR\backup-health.log"
)

$ErrorActionPreference = "Stop"

function Add-HealthLog([string]$Status, [string]$Message) {
    $logDirectory = Split-Path -Parent $HealthLogPath
    if (-not [string]::IsNullOrWhiteSpace($logDirectory)) {
        New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    }
    $line = "{0} {1} {2}" -f [DateTimeOffset]::Now.ToString("o"), $Status, $Message
    Add-Content -LiteralPath $HealthLogPath -Value $line -Encoding UTF8
}

function Get-LatestVerifiedBackup([string]$Directory, [string]$Label, [int]$AgeLimitHours) {
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        throw "$Label backup directory is not accessible: $Directory"
    }
    $latest = Get-ChildItem -LiteralPath $Directory -File -Filter "envantertr-*.etbackup" |
        Where-Object { -not ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) } |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $latest) { throw "$Label has no completed encrypted backup." }

    $age = [DateTime]::UtcNow - $latest.LastWriteTimeUtc
    if ($age.TotalHours -gt $AgeLimitHours) {
        throw "$Label latest backup is stale ({0:N1} hours old; limit {1} hours): {2}" -f $age.TotalHours,$AgeLimitHours,$latest.Name
    }

    $checksumPath = "$($latest.FullName).sha256"
    if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
        throw "$Label latest backup is missing its SHA-256 sidecar: $($latest.Name)"
    }
    $checksumText = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
    if ($checksumText -notmatch '^([A-Fa-f0-9]{64})\s+\*?([^\r\n]+)$' -or
        -not [string]::Equals($Matches[2],$latest.Name,[StringComparison]::Ordinal)) {
        throw "$Label latest backup SHA-256 sidecar is malformed or names another file: $($latest.Name)"
    }
    $expectedHash = $Matches[1].ToLowerInvariant()
    $actualHash = (Get-FileHash -LiteralPath $latest.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) { throw "$Label latest backup SHA-256 does not match: $($latest.Name)" }

    return [pscustomobject]@{ Name = $latest.Name; AgeHours = [Math]::Round($age.TotalHours,1); Hash = $actualHash }
}

try {
    $local = Get-LatestVerifiedBackup $OutputDirectory "Local" $MaxAgeHours
    $summary = "local=$($local.Name); ageHours=$($local.AgeHours); sha256=verified"
    if (-not [string]::IsNullOrWhiteSpace($MirrorDirectory)) {
        if (-not $MirrorDirectory.StartsWith('\\',[StringComparison]::Ordinal) -or
            $MirrorDirectory.Contains('"') -or $MirrorDirectory.Contains("`r") -or $MirrorDirectory.Contains("`n")) {
            throw "MirrorDirectory must be a UNC path without quote or line-break characters."
        }
        $mirror = Get-LatestVerifiedBackup $MirrorDirectory "UNC mirror" $MaxAgeHours
        $summary += "; mirror=$($mirror.Name); ageHours=$($mirror.AgeHours); sha256=verified"
    }
    Add-HealthLog "PASS" $summary
    Write-Output "Backup health check passed: $summary"
} catch {
    $failure = $_.Exception.Message -replace '[\r\n]+',' '
    try { Add-HealthLog "FAIL" $failure } catch { throw "Backup health check failed and its status log could not be written: $failure; $($_.Exception.Message)" }
    throw "Backup health check failed: $failure"
}
