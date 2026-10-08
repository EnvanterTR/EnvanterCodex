param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin",
    [string]$BackupOutputDirectory = "$env:ProgramData\EnvanterTR\backups",
    [string]$BackupPassphraseFile,
    [switch]$ApplyMigrations
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
$package = (Resolve-Path $PackagePath).Path
if (-not (Test-Path (Join-Path $package "EnvanterTR.Web.exe"))) { throw "Package is missing EnvanterTR.Web.exe." }
$appPath = Join-Path $InstallRoot "app"
$configPath = Join-Path $appPath "appsettings.Production.json"
$service = Get-Service -Name $ServiceName -ErrorAction Stop
if (-not (Test-Path $configPath)) { throw "Production configuration is missing; refusing update." }
$migrationConfirmation = $null
if ($ApplyMigrations) {
    $migrationConfirmation = Read-Host "This creates and verifies a fresh encrypted backup, then applies database migrations. Type MIGRATE to confirm"
    if ($migrationConfirmation -cne "MIGRATE") { throw "Update cancelled before files or database were changed." }
}
$production = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$httpsUrl = [uri]$production.Kestrel.Endpoints.Https.Url
$releasePath = Join-Path $InstallRoot ("rollback-" + (Get-Date -Format "yyyyMMdd-HHmmss") + "-" + [guid]::NewGuid().ToString("N").Substring(0,8))
$wasRunning = $false
$snapshotReady = $false
$migrationAttempted = $false
$temporaryServiceStarted = $false
$maintenanceMutex = [Threading.Mutex]::new($false,"Global\EnvanterTR_Maintenance")
$maintenanceMutexAcquired = $false
try { $maintenanceMutexAcquired = $maintenanceMutex.WaitOne(0) }
catch [Threading.AbandonedMutexException] { $maintenanceMutexAcquired = $true; Write-Warning "An earlier maintenance process ended unexpectedly; continuing with the abandoned operation lock." }
if (-not $maintenanceMutexAcquired) { $maintenanceMutex.Dispose(); throw "Another EnvanterTR backup, restore or update operation is in progress." }
try {
    $service = Get-Service -Name $ServiceName -ErrorAction Stop
    $wasRunning = $service.Status -eq "Running"
    if (-not $wasRunning) {
        throw "The EnvanterTR service must be running before an update so a fresh, verified backup can be made. Start the service and confirm /api/status is ready, then retry."
    }
    if ($wasRunning) {
        $backupScript = Join-Path $PSScriptRoot "backup.ps1"
        if (-not (Test-Path -LiteralPath $backupScript -PathType Leaf) -or
            -not (Test-Path -LiteralPath (Join-Path $PSScriptRoot "BackupFormat.psm1") -PathType Leaf)) {
            throw "The package is missing the encrypted backup tools required before updating."
        }
        if ([string]::IsNullOrWhiteSpace($BackupPassphraseFile)) {
            $scheduledPassphrase = Join-Path $DataRoot "backup-secrets\backup-password.dpapi"
            if (Test-Path -LiteralPath $scheduledPassphrase -PathType Leaf) { $BackupPassphraseFile = $scheduledPassphrase }
        }
        $backupArguments = @{
            InstallRoot = $InstallRoot
            DataRoot = $DataRoot
            ServiceName = $ServiceName
            OutputDirectory = $BackupOutputDirectory
            PgBinDirectory = $PgBinDirectory
        }
        if (-not [string]::IsNullOrWhiteSpace($BackupPassphraseFile)) { $backupArguments.PassphraseFile = $BackupPassphraseFile }
        $backupOutput = @(& $backupScript @backupArguments)
        $backupLine = @($backupOutput | Where-Object { $_ -is [string] -and $_.StartsWith("Encrypted backup created and verified: ",[StringComparison]::Ordinal) } | Select-Object -Last 1)
        if ($backupLine.Count -ne 1) { throw "The update stopped because the backup did not report a verified encrypted archive." }
        $verifiedBackupPath = $backupLine[0].Substring("Encrypted backup created and verified: ".Length)
        if (-not (Test-Path -LiteralPath $verifiedBackupPath -PathType Leaf) -or
            -not (Test-Path -LiteralPath ($verifiedBackupPath + ".sha256") -PathType Leaf)) {
            throw "The update stopped because the verified archive or its SHA-256 sidecar is missing."
        }
        $verifiedDigest = (Get-FileHash -LiteralPath $verifiedBackupPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $sidecar = (Get-Content -LiteralPath ($verifiedBackupPath + ".sha256") -Raw).Trim()
        if ($sidecar -notmatch ("^" + [regex]::Escape($verifiedDigest) + " \*" + [regex]::Escape([IO.Path]::GetFileName($verifiedBackupPath)) + "$")) {
            throw "The update stopped because the fresh encrypted backup does not match its SHA-256 sidecar."
        }
        Write-Output "Fresh encrypted backup and isolated restore preview verified: $verifiedBackupPath"
    }
    if ($wasRunning) { Stop-Service -Name $ServiceName; (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60)) }
    Copy-Item -LiteralPath $appPath -Destination $releasePath -Recurse
    $snapshotReady = $true
    Get-ChildItem -LiteralPath $appPath -Force | Remove-Item -Recurse -Force
    Copy-Item -Path (Join-Path $package "*") -Destination $appPath -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $releasePath "appsettings.Production.json") -Destination $configPath -Force
    & icacls.exe $appPath /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set application directory permissions." }
    if ($ApplyMigrations) {
        $migrationAttempted = $true
        Push-Location $appPath
        try {
            & (Join-Path $appPath "EnvanterTR.Web.exe") --migrate
            if ($LASTEXITCODE -ne 0) { throw "Database migration process failed with exit code $LASTEXITCODE. The migration may have changed the database; restore the verified backup before running the previous application." }
        } finally { Pop-Location }
    }
    Start-Service -Name $ServiceName
    $deadline = (Get-Date).AddMinutes(2)
    do {
        Start-Sleep -Seconds 3
        try {
            $url = "https://localhost:{0}/api/status" -f $httpsUrl.Port
            if ($PSVersionTable.PSVersion.Major -ge 7) {
                $status = Invoke-RestMethod -Uri $url -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
            } else {
                $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
                [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
                try { $status = Invoke-RestMethod -Uri $url -TimeoutSec 5 -ErrorAction Stop }
                finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
            }
        } catch { $status = $null }
        if ($status.status -eq "ready") {
            if (-not $wasRunning) { Stop-Service -Name $ServiceName; (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60)) }
            Write-Output "Update is healthy. Previous application files remain at $releasePath"
            if (-not $wasRunning) { Write-Output "The service was stopped before the update and has been left stopped." }
            return
        }
    } while ((Get-Date) -lt $deadline)
    throw "Health check did not report ready."
} catch {
    $updateFailure = $_
    if ($migrationAttempted) {
        try {
            if ((Get-Service -Name $ServiceName).Status -eq "Running") {
                Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
                (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60))
            }
        } catch { }
        throw "Update stopped after a database migration was started. The updated application files remain in place and the previous files are preserved at '$releasePath'. The database may have changed; restore the verified backup before running the previous application. Original error: $($updateFailure.Exception.Message)"
    }
    if ($temporaryServiceStarted) {
        try {
            if ((Get-Service -Name $ServiceName).Status -eq "Running") {
                Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
                (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60))
            }
        } catch { Write-Warning "Could not stop the temporary service after the failed update: $($_.Exception.Message)" }
    }
    if ($snapshotReady) {
        if ((Get-Service -Name $ServiceName).Status -eq "Running") {
            Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
            (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60))
        }
        Get-ChildItem -LiteralPath $appPath -Force | Remove-Item -Recurse -Force
        Copy-Item -Path (Join-Path $releasePath "*") -Destination $appPath -Recurse -Force
        & icacls.exe $appPath /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
        if ($wasRunning) { Start-Service -Name $ServiceName -ErrorAction SilentlyContinue }
    }
    throw $updateFailure
} finally {
    if ($maintenanceMutexAcquired) { $maintenanceMutex.ReleaseMutex(); $maintenanceMutexAcquired = $false }
    $maintenanceMutex.Dispose()
}
