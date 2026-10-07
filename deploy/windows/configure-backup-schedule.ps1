param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin",
    [ValidatePattern('^(?:[01][0-9]|2[0-3]):[0-5][0-9]$')][string]$DailyAt = "02:30",
    [ValidateRange(1,3650)][int]$RetentionDays = 30,
    [string]$TaskName = "EnvanterTR Daily Encrypted Backup"
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
if ($PSVersionTable.PSVersion.Major -lt 5) { throw "Windows PowerShell 5.1 or PowerShell 7 is required." }

$backupScript = Join-Path (Join-Path $InstallRoot "app") "backup.ps1"
$configPath = Join-Path (Join-Path $InstallRoot "app") "appsettings.Production.json"
if (-not (Test-Path -LiteralPath $backupScript -PathType Leaf) -or -not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
    throw "An installed EnvanterTR service is required before scheduling backups."
}
if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) { throw "EnvanterTR service '$ServiceName' was not found." }

$mediaPath = Join-Path $DataRoot "company-media"
$keysPath = Join-Path $DataRoot "data-protection-keys"
$secretDirectory = Join-Path $DataRoot "backup-secrets"
$secretPath = Join-Path $secretDirectory "backup-password.dpapi"
$taskScript = $backupScript
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

$triggerTime = [DateTime]::Today.Add([TimeSpan]::ParseExact($DailyAt,"hh\:mm",[Globalization.CultureInfo]::InvariantCulture))
$trigger = New-ScheduledTaskTrigger -Daily -At $triggerTime
$actionArguments = @(
    "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", ('"' + $taskScript + '"'),
    "-InstallRoot", ('"' + $InstallRoot + '"'), "-DataRoot", ('"' + $DataRoot + '"'),
    "-ServiceName", ('"' + $ServiceName + '"'), "-PgBinDirectory", ('"' + $PgBinDirectory + '"'),
    "-PassphraseFile", ('"' + $secretPath + '"'), "-RetentionDays", [string]$RetentionDays
) -join " "
$powerShellExe = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
if (-not (Test-Path -LiteralPath $powerShellExe -PathType Leaf)) { throw "Could not resolve the active PowerShell executable." }
$action = New-ScheduledTaskAction -Execute $powerShellExe -Argument $actionArguments
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Hours 12) -MultipleInstances IgnoreNew
$task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings
Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

Write-Output "Scheduled encrypted EnvanterTR backup daily at $DailyAt."
Write-Output "Retention: $RetentionDays days. Backup directory: $(Join-Path $DataRoot 'backups')"
Write-Output "The encryption passphrase is protected with Windows DPAPI and is not saved in task arguments or configuration."
Write-Output "Use Get-ScheduledTaskInfo -TaskName '$TaskName' to inspect the last result; run the task once manually to confirm PostgreSQL and restore preview prerequisites."
