param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [switch]$InitialSetup,
    [string]$BackupPath
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
if ($InitialSetup -and $BackupPath) { throw "Choose -InitialSetup for a new empty database, or -BackupPath for an existing database; do not combine them." }
if (-not $InitialSetup -and [string]::IsNullOrWhiteSpace($BackupPath)) {
    throw "For an existing database, provide -BackupPath pointing to a separately verified backup. Use -InitialSetup only for a new empty database."
}
if ($BackupPath -and -not (Test-Path -LiteralPath $BackupPath -PathType Leaf)) { throw "Backup file does not exist: $BackupPath" }
$confirmation = Read-Host "This runs database schema migrations. Type MIGRATE to confirm that the database is new or the supplied backup is verified"
if ($confirmation -cne "MIGRATE") { throw "Migration cancelled." }

$appPath = Join-Path $InstallRoot "app"
$exe = Join-Path $appPath "EnvanterTR.Web.exe"
if (-not (Test-Path -LiteralPath $exe)) { throw "Installed application is missing: $exe" }
if (-not (Test-Path -LiteralPath (Join-Path $appPath "appsettings.Production.json"))) { throw "Production configuration is missing." }
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$wasRunning = $service -and $service.Status -eq "Running"
if ($wasRunning) {
    Stop-Service -Name $ServiceName
    (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60))
}
try {
    Push-Location $appPath
    try {
        & $exe --migrate
        if ($LASTEXITCODE -ne 0) { throw "Database migration process failed with exit code $LASTEXITCODE. Restore the verified backup before retrying if database state is uncertain." }
    } finally { Pop-Location }
} finally {
    if ($wasRunning) { Start-Service -Name $ServiceName }
}
Write-Output "Migration command completed. Check https://localhost:<configured-port>/api/status for ready before serving users."
