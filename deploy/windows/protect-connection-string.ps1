param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [ValidateRange(1,65535)][int]$HttpsPort = 8443
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }

$appPath = Join-Path $InstallRoot "app"
$configPath = Join-Path $appPath "appsettings.Production.json"
$secretsPath = Join-Path $DataRoot "secrets"
$secretPath = Join-Path $secretsPath "database-connection.dpapi"
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) { throw "Production configuration is missing." }
$service = Get-Service -Name $ServiceName -ErrorAction Stop
if ($service.Status -ne "Running") { throw "The EnvanterTR service must be running before secret migration." }

Import-Module (Join-Path $PSScriptRoot "WindowsSecrets.psm1") -Force
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if (-not [string]::IsNullOrWhiteSpace([string]$config.WindowsSecrets.ConnectionStringPath)) {
    Get-EnvanterTRConnectionString -ConfigurationPath $configPath | Out-Null
    Write-Output "The database connection secret is already protected with Windows DPAPI."
    return
}
$connectionString = [string]$config.ConnectionStrings.EnvanterTR
if ([string]::IsNullOrWhiteSpace($connectionString)) { throw "Legacy PostgreSQL connection string is missing." }
if (Test-Path -LiteralPath $secretPath) { throw "A protected database secret already exists; inspect it before migrating." }
$originalConfigBytes = [IO.File]::ReadAllBytes($configPath)
$plainBytes = [Text.Encoding]::UTF8.GetBytes($connectionString)
$protectedBytes = $null
$temporarySecret = Join-Path $secretsPath ("database-connection." + [guid]::NewGuid().ToString("N") + ".tmp")
$temporaryConfig = Join-Path $appPath ("appsettings.Production." + [guid]::NewGuid().ToString("N") + ".tmp")
$secretCreated = $false
$configChanged = $false
$serviceStopped = $false

try {
    $protectedBytes = [Security.Cryptography.ProtectedData]::Protect(
        $plainBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)

    New-Item -ItemType Directory -Path $secretsPath -Force | Out-Null
    & icacls.exe $secretsPath /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not restrict protected secret directory permissions." }

    Stop-Service -Name $ServiceName
    (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
    $serviceStopped = $true
    [IO.File]::WriteAllBytes($temporarySecret,$protectedBytes)
    [IO.File]::Move($temporarySecret,$secretPath)
    $secretCreated = $true

    $config.PSObject.Properties.Remove("ConnectionStrings")
    if ($null -eq $config.WindowsSecrets) {
        if ($config.PSObject.Properties["WindowsSecrets"]) { $config.WindowsSecrets = [pscustomobject]@{} }
        else { $config | Add-Member -MemberType NoteProperty -Name WindowsSecrets -Value ([pscustomobject]@{}) }
    }
    $config.WindowsSecrets | Add-Member -MemberType NoteProperty -Name ConnectionStringPath -Value $secretPath -Force
    $json = $config | ConvertTo-Json -Depth 32
    [IO.File]::WriteAllText($temporaryConfig,$json,[Text.UTF8Encoding]::new($false))
    [IO.File]::Replace($temporaryConfig,$configPath,$null)
    $configChanged = $true

    Start-Service -Name $ServiceName
    $serviceStopped = $false
    $deadline = (Get-Date).AddMinutes(2)
    $ready = $false
    do {
        Start-Sleep -Seconds 3
        try {
            if ($PSVersionTable.PSVersion.Major -ge 7) {
                $status = Invoke-RestMethod -Uri "https://localhost:$HttpsPort/api/status" -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
            } else {
                $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
                [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
                try { $status = Invoke-RestMethod -Uri "https://localhost:$HttpsPort/api/status" -TimeoutSec 5 -ErrorAction Stop }
                finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
            }
            if ($status.product -eq "EnvanterTR" -and $status.status -eq "ready") { $ready = $true; break }
        } catch { }
    } while ((Get-Date) -lt $deadline)
    if (-not $ready) { throw "The service did not become ready after protecting the database secret." }

    Write-Output "PostgreSQL connection secret is now DPAPI protected in the restricted DataRoot secrets directory."
    Write-Output "The service passed its HTTPS readiness check; the original plaintext connection string was removed from appsettings."
} catch {
    $migrationFailure = $_
    try {
        if ((Get-Service -Name $ServiceName -ErrorAction SilentlyContinue).Status -eq "Running") {
            Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
            (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
            $serviceStopped = $true
        }
        if ($configChanged) { [IO.File]::WriteAllBytes($configPath,$originalConfigBytes) }
        if ($secretCreated) { Remove-Item -LiteralPath $secretPath -Force -ErrorAction SilentlyContinue }
        if ($serviceStopped) { Start-Service -Name $ServiceName; $serviceStopped = $false }
    } catch { Write-Warning "Automatic rollback needs attention: $($_.Exception.Message). Original configuration bytes remain in memory only until this process exits." }
    throw "Database-secret migration failed; rollback was attempted. $($migrationFailure.Exception.Message)"
} finally {
    foreach ($temporaryPath in @($temporarySecret,$temporaryConfig)) {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    }
    [Array]::Clear($plainBytes,0,$plainBytes.Length)
    [Array]::Clear($originalConfigBytes,0,$originalConfigBytes.Length)
    if ($null -ne $protectedBytes) { [Array]::Clear($protectedBytes,0,$protectedBytes.Length) }
    $connectionString = $null
}
