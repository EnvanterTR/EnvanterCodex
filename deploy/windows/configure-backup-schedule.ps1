param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin",
    [string]$MirrorDirectory,
    [ValidatePattern('^(?:[01][0-9]|2[0-3]):[0-5][0-9]$')][string]$DailyAt = "02:30",
    [ValidatePattern('^(?:[01][0-9]|2[0-3]):[0-5][0-9]$')][string]$HealthCheckAt = "04:00",
    [ValidateRange(1,3650)][int]$RetentionDays = 30,
    [ValidateRange(0,3650)][int]$MirrorRetentionDays = 90,
    [string]$TaskName = "EnvanterTR Daily Encrypted Backup",
    [string]$HealthTaskName = "EnvanterTR Daily Backup Health Check"
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
if ($PSVersionTable.PSVersion.Major -lt 5) { throw "Windows PowerShell 5.1 or PowerShell 7 is required." }
foreach ($value in @($InstallRoot,$DataRoot,$ServiceName,$PgBinDirectory)) {
    if (-not [string]::IsNullOrEmpty($value) -and
        ($value.Contains('"') -or $value.Contains("`r") -or $value.Contains("`n"))) {
        throw "Install, data, service, and PostgreSQL paths/names must not contain quotes or line breaks."
    }
}
foreach ($value in @($TaskName,$HealthTaskName)) {
    if ([string]::IsNullOrWhiteSpace($value) -or $value.Length -gt 200 -or
        $value.Contains('"') -or $value.Contains("`r") -or $value.Contains("`n")) {
        throw "Scheduled task names must be non-empty, at most 200 characters, and contain no quotes or line breaks."
    }
}
if ([string]::Equals($TaskName,$HealthTaskName,[StringComparison]::OrdinalIgnoreCase)) {
    throw "Backup and health-check scheduled task names must be different."
}
if ([string]::Equals($DailyAt,$HealthCheckAt,[StringComparison]::Ordinal)) {
    throw "HealthCheckAt must differ from DailyAt so the health check does not race the backup task."
}

$backupScript = Join-Path (Join-Path $InstallRoot "app") "backup.ps1"
$scheduledBackupScript = Join-Path (Join-Path $InstallRoot "app") "run-scheduled-backup.ps1"
$alertScript = Join-Path (Join-Path $InstallRoot "app") "send-backup-alert.ps1"
$healthScript = Join-Path (Join-Path $InstallRoot "app") "check-backup-health.ps1"
$configPath = Join-Path (Join-Path $InstallRoot "app") "appsettings.Production.json"
if (-not (Test-Path -LiteralPath $backupScript -PathType Leaf) -or
    -not (Test-Path -LiteralPath $scheduledBackupScript -PathType Leaf) -or
    -not (Test-Path -LiteralPath $alertScript -PathType Leaf) -or
    -not (Test-Path -LiteralPath $healthScript -PathType Leaf) -or
    -not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw "An installed EnvanterTR service is required before scheduling backups."
}
if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) { throw "EnvanterTR service '$ServiceName' was not found." }

$mediaPath = Join-Path $DataRoot "company-media"
$keysPath = Join-Path $DataRoot "data-protection-keys"
$secretDirectory = Join-Path $DataRoot "backup-secrets"
$secretPath = Join-Path $secretDirectory "backup-password.dpapi"
$alertSecretPath = Join-Path $secretDirectory "backup-alert-webhook.dpapi"
$taskScript = $scheduledBackupScript
New-Item -ItemType Directory -Path $secretDirectory -Force | Out-Null

# Keep the service writable only in the two data directories it needs. In particular,
# it must not be able to read, replace, or delete the scheduled-backup DPAPI secret.
& icacls.exe $DataRoot /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not restrict the EnvanterTR data directory." }
foreach ($writablePath in @($mediaPath,$keysPath)) {
    if (-not (Test-Path -LiteralPath $writablePath -PathType Container)) { throw "Required EnvanterTR data directory is missing: $writablePath" }
    & icacls.exe $writablePath /grant:r "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not grant the service access to $writablePath" }
}
& icacls.exe $secretDirectory /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not protect the backup encryption secret directory." }

Add-Type -AssemblyName System.Security
$first = Read-Host "Backup encryption passphrase (minimum 16 characters; DPAPI protected for this machine)" -AsSecureString
$second = Read-Host "Repeat backup encryption passphrase" -AsSecureString
function Convert-SecureToPlain([Security.SecureString]$Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
$firstText = Convert-SecureToPlain $first
$secondText = Convert-SecureToPlain $second
$first.Dispose()
$second.Dispose()
if ($firstText.Length -lt 16) { throw "Use a passphrase with at least 16 characters." }
if (-not [string]::Equals($firstText,$secondText,[StringComparison]::Ordinal)) { throw "Passphrases do not match." }

$plainBytes = [Text.Encoding]::Unicode.GetBytes($firstText)
$protectedBytes = $null
try {
    $protectedBytes = [Security.Cryptography.ProtectedData]::Protect(
        $plainBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
    [IO.File]::WriteAllBytes($secretPath,$protectedBytes)
} finally {
    [Array]::Clear($plainBytes,0,$plainBytes.Length)
    if ($protectedBytes) { [Array]::Clear($protectedBytes,0,$protectedBytes.Length) }
    $firstText = $null
    $secondText = $null
}

$alertSecure = Read-Host "Optional HTTPS JSON webhook URL for backup failures (press Enter to disable)" -AsSecureString
$alertText = Convert-SecureToPlain $alertSecure
$alertSecure.Dispose()
if ([string]::IsNullOrEmpty($alertText)) {
    Remove-Item -LiteralPath $alertSecretPath -Force -ErrorAction SilentlyContinue
    Write-Output "Backup failure webhook disabled."
} else {
    $alertUri = $null
    if ($alertText.Length -gt 4096 -or $alertText -match '[\r\n\t ]' -or
        -not [Uri]::TryCreate($alertText,[UriKind]::Absolute,[ref]$alertUri) -or
        $alertUri.Scheme -ne "https" -or [string]::IsNullOrWhiteSpace($alertUri.Host) -or
        -not [string]::IsNullOrEmpty($alertUri.UserInfo)) {
        $alertText = $null
        throw "The backup alert URL must be an absolute HTTPS URL without whitespace or embedded credentials."
    }
    $alertPlainBytes = [Text.Encoding]::Unicode.GetBytes($alertText)
    $alertProtectedBytes = $null
    $alertTempPath = "$alertSecretPath.$PID.tmp"
    try {
        $alertProtectedBytes = [Security.Cryptography.ProtectedData]::Protect(
            $alertPlainBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        [IO.File]::WriteAllBytes($alertTempPath,$alertProtectedBytes)
        Move-Item -LiteralPath $alertTempPath -Destination $alertSecretPath -Force
    } finally {
        [Array]::Clear($alertPlainBytes,0,$alertPlainBytes.Length)
        if ($alertProtectedBytes) { [Array]::Clear($alertProtectedBytes,0,$alertProtectedBytes.Length) }
        Remove-Item -LiteralPath $alertTempPath -Force -ErrorAction SilentlyContinue
        $alertText = $null
    }
    Write-Output "Backup failure webhook URL protected with Windows DPAPI; it is not stored in task arguments or logs."
}

$triggerTime = [DateTime]::Today.Add([TimeSpan]::ParseExact($DailyAt,"hh\:mm",[Globalization.CultureInfo]::InvariantCulture))
$trigger = New-ScheduledTaskTrigger -Daily -At $triggerTime
$actionArguments = @(
    "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", ('"' + $taskScript + '"'),
    "-InstallRoot", ('"' + $InstallRoot + '"'), "-DataRoot", ('"' + $DataRoot + '"'),
    "-ServiceName", ('"' + $ServiceName + '"'), "-PgBinDirectory", ('"' + $PgBinDirectory + '"'),
    "-PassphraseFile", ('"' + $secretPath + '"'), "-RetentionDays", [string]$RetentionDays,
    "-MirrorRetentionDays", [string]$MirrorRetentionDays,
    "-TaskName", ('"' + $TaskName + '"')
) -join " "
if (-not [string]::IsNullOrWhiteSpace($MirrorDirectory)) {
    if (-not $MirrorDirectory.StartsWith('\\',[StringComparison]::Ordinal) -or $MirrorDirectory.Contains('"') -or
        $MirrorDirectory.Contains("`r") -or $MirrorDirectory.Contains("`n")) {
        throw "MirrorDirectory must be a UNC path without quote or line-break characters."
    }
    $mirrorParts = $MirrorDirectory.TrimEnd('\').Substring(2).Split('\')
    if ($mirrorParts.Count -lt 2 -or @($mirrorParts | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -in @('.','..') -or $_ -match '[<>:"|?*]' }).Count -gt 0) {
        throw "MirrorDirectory must contain a valid UNC server and share path."
    }
    $actionArguments += ' -MirrorDirectory "' + $MirrorDirectory + '"'
}
$powerShellExe = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
if (-not (Test-Path -LiteralPath $powerShellExe -PathType Leaf)) { throw "Could not resolve the active PowerShell executable." }
$action = New-ScheduledTaskAction -Execute $powerShellExe -Argument $actionArguments
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Hours 12) -MultipleInstances IgnoreNew
$task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings
Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

$healthTime = [DateTime]::Today.Add([TimeSpan]::ParseExact($HealthCheckAt,"hh\:mm",[Globalization.CultureInfo]::InvariantCulture))
$healthTrigger = New-ScheduledTaskTrigger -Daily -At $healthTime
$healthArguments = @(
    "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", ('"' + $healthScript + '"'),
    "-DataRoot", ('"' + $DataRoot + '"'),
    "-OutputDirectory", ('"' + (Join-Path $DataRoot 'backups') + '"'),
    "-HealthLogPath", ('"' + (Join-Path $DataRoot 'backup-health.log') + '"'),
    "-MaxAgeHours", "36"
) -join " "
if (-not [string]::IsNullOrWhiteSpace($MirrorDirectory)) {
    $healthArguments += ' -MirrorDirectory "' + $MirrorDirectory + '"'
}
$healthAction = New-ScheduledTaskAction -Execute $powerShellExe -Argument $healthArguments
$healthTask = New-ScheduledTask -Action $healthAction -Trigger $healthTrigger -Principal $principal -Settings $settings
Register-ScheduledTask -TaskName $HealthTaskName -InputObject $healthTask -Force | Out-Null

Write-Output "Scheduled encrypted EnvanterTR backup daily at $DailyAt."
Write-Output "Retention: $RetentionDays days. Backup directory: $(Join-Path $DataRoot 'backups')"
if (-not [string]::IsNullOrWhiteSpace($MirrorDirectory)) {
    if ($MirrorRetentionDays -eq 0) { Write-Output "SMB mirror retention cleanup: disabled." }
    else { Write-Output "SMB mirror retention: $MirrorRetentionDays days." }
}
Write-Output "The encryption passphrase is protected with Windows DPAPI and is not saved in task arguments or configuration."
Write-Output "Use Get-ScheduledTaskInfo -TaskName '$TaskName' to inspect the last result; run the task once manually to confirm PostgreSQL and restore preview prerequisites."
Write-Output "Scheduled backup health check daily at $HealthCheckAt (36-hour freshness limit)."
Write-Output "Use Get-ScheduledTaskInfo -TaskName '$HealthTaskName' and $(Join-Path $DataRoot 'backup-health.log') to inspect health results."
Write-Output "Backup and health failures are sent to the optional HTTPS webhook; delivery status is logged in $(Join-Path $DataRoot 'backup-alert.log')."
