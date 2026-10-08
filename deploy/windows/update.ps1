param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [switch]$ApplyMigrations,
    [string]$BackupPath
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
if ($ApplyMigrations -and [string]::IsNullOrWhiteSpace($BackupPath)) { throw "-ApplyMigrations requires -BackupPath to a separately verified database backup." }
if ($ApplyMigrations -and -not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) { throw "Backup file does not exist: $BackupPath" }
$migrationConfirmation = $null
if ($ApplyMigrations) {
    $migrationConfirmation = Read-Host "This applies database migrations. Type MIGRATE to confirm the supplied backup is verified"
    if ($migrationConfirmation -cne "MIGRATE") { throw "Update cancelled before files or database were changed." }
}
$production = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$httpsUrl = [uri]$production.Kestrel.Endpoints.Https.Url
$releasePath = Join-Path $InstallRoot ("rollback-" + (Get-Date -Format "yyyyMMdd-HHmmss") + "-" + [guid]::NewGuid().ToString("N").Substring(0,8))
$wasRunning = $service.Status -eq "Running"
$snapshotReady = $false
$migrationAttempted = $false
$maintenanceMutex = [Threading.Mutex]::new($false,"Global\EnvanterTR_Maintenance")
$maintenanceMutexAcquired = $false
try { $maintenanceMutexAcquired = $maintenanceMutex.WaitOne(0) }
catch [Threading.AbandonedMutexException] { $maintenanceMutexAcquired = $true; Write-Warning "An earlier maintenance process ended unexpectedly; continuing with the abandoned operation lock." }
if (-not $maintenanceMutexAcquired) { $maintenanceMutex.Dispose(); throw "Another EnvanterTR backup, restore or update operation is in progress." }
try {
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
