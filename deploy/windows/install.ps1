param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [int]$HttpsPort = 8443,
    [string]$CertificateSubject = "",
    [string]$CertificateThumbprint = "",
    [Security.SecureString]$DatabaseConnectionStringSecure = $null
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
if ($PSVersionTable.PSVersion.Major -lt 5) { throw "Windows PowerShell 5.1 or PowerShell 7 is required." }
if ($HttpsPort -lt 1 -or $HttpsPort -gt 65535) { throw "HttpsPort must be between 1 and 65535." }

$package = (Resolve-Path $PackagePath).Path
$exe = Join-Path $package "EnvanterTR.Web.exe"
if (-not (Test-Path $exe)) { throw "Package is missing EnvanterTR.Web.exe: $package" }
$installRootFull = [IO.Path]::GetFullPath($InstallRoot).TrimEnd('\') + '\'
$dataRootFull = [IO.Path]::GetFullPath($DataRoot).TrimEnd('\') + '\'
if ($installRootFull.StartsWith($dataRootFull,[StringComparison]::OrdinalIgnoreCase) -or
    $dataRootFull.StartsWith($installRootFull,[StringComparison]::OrdinalIgnoreCase)) {
    throw "InstallRoot and DataRoot must be separate, non-nested directories."
}
foreach ($rootPath in @($InstallRoot,$DataRoot)) {
    if ((Test-Path -LiteralPath $rootPath -PathType Container) -and
        ((Get-Item -LiteralPath $rootPath).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "InstallRoot and DataRoot cannot be reparse points: $rootPath"
    }
}
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) { throw "Service '$ServiceName' already exists. Use update.ps1 for updates; this installer never overwrites an installation." }

$subject = $CertificateSubject.Trim()
if ([string]::IsNullOrWhiteSpace($subject)) {
    $subject = (Read-Host "HTTPS certificate subject as shown in Local Computer > Personal (example: CN=inventory.example.com)").Trim()
}
if ([string]::IsNullOrWhiteSpace($subject)) { throw "A certificate subject is required." }
$store = [System.Security.Cryptography.X509Certificates.X509Store]::new("My", "LocalMachine")
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
try {
    $matches = @($store.Certificates | Where-Object {
        $subjectMatches = $_.Subject -eq $subject
        $thumbprintMatches = [string]::IsNullOrWhiteSpace($CertificateThumbprint) -or $_.Thumbprint -eq $CertificateThumbprint
        $subjectMatches -and $thumbprintMatches -and $_.HasPrivateKey -and $_.NotBefore -lt (Get-Date) -and $_.NotAfter -gt (Get-Date)
    })
    if ($matches.Count -ne 1) { throw "Expected exactly one valid LocalMachine\My certificate with a private key and subject '$subject'; found $($matches.Count)." }
} finally { $store.Close() }

$connectionSecure = $DatabaseConnectionStringSecure
if (-not $connectionSecure) { $connectionSecure = Read-Host "PostgreSQL connection string for EnvanterTR (input hidden)" -AsSecureString }
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($connectionSecure)
try { $connectionString = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr); $connectionSecure.Dispose() }
if ([string]::IsNullOrWhiteSpace($connectionString) -or $connectionString -notmatch "(?i)(Host|Server)=.+(Database|Initial Catalog)=.+") {
    throw "A PostgreSQL connection string containing Host and Database is required."
}

$appPath = Join-Path $InstallRoot "app"
$mediaPath = Join-Path $DataRoot "company-media"
$keysPath = Join-Path $DataRoot "data-protection-keys"
$secretsPath = Join-Path $DataRoot "secrets"
$connectionSecretPath = Join-Path $secretsPath "database-connection.dpapi"
$configPath = Join-Path $appPath "appsettings.Production.json"
$connectionSecretCreated = $false
if (Test-Path -LiteralPath $connectionSecretPath) { throw "A protected database connection secret already exists; the installer will not overwrite DataRoot data." }
$appPathWasPresent = Test-Path -LiteralPath $appPath -PathType Container
if ($appPathWasPresent) {
    if ((Get-Item -LiteralPath $appPath).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Install directory cannot be a reparse point: $appPath"
    }
    if (Get-ChildItem -LiteralPath $appPath -Force | Select-Object -First 1) { throw "Install directory is not empty: $appPath" }
}
if (Test-Path -LiteralPath $InstallRoot -PathType Container) {
    $unrelatedInstallFiles = @(Get-ChildItem -LiteralPath $InstallRoot -Force | Where-Object { $_.FullName -ne [IO.Path]::GetFullPath($appPath) })
    if ($unrelatedInstallFiles.Count -gt 0) { throw "InstallRoot must be dedicated to EnvanterTR and cannot contain unrelated files: $InstallRoot" }
}
$appInstallStarted = $false
$serviceCreated = $false

try {
    $appInstallStarted = $true
    New-Item -ItemType Directory -Path $appPath,$mediaPath,$keysPath,$secretsPath -Force | Out-Null
    Copy-Item -Path (Join-Path $package "*") -Destination $appPath -Recurse -Force

    # Restrict configuration and the DPAPI secret location before saving installation secrets.
    & icacls.exe $InstallRoot /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set application directory permissions." }
    & icacls.exe $DataRoot /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set data directory permissions." }
    foreach ($writableDataPath in @($mediaPath,$keysPath)) {
        & icacls.exe $writableDataPath /grant:r "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M" /T /C | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Could not grant the service access to $writableDataPath" }
    }
    & icacls.exe $secretsPath /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not restrict protected secret directory permissions." }

$tokenBytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($tokenBytes) } finally { $random.Dispose() }
$bootstrapToken = [Convert]::ToBase64String($tokenBytes)
$tokenBytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($tokenBytes) } finally { $random.Dispose() }
$platformBootstrapToken = [Convert]::ToBase64String($tokenBytes)

$config = [ordered]@{
    WindowsSecrets = @{ ConnectionStringPath = $connectionSecretPath }
    DataProtection = @{ KeysPath = $keysPath }
    Storage = @{ CompanyMediaPath = $mediaPath }
    Setup = @{
        BootstrapToken = $bootstrapToken
        PlatformBootstrapToken = $platformBootstrapToken
    }
    Kestrel = @{ Endpoints = @{ Https = @{
        Url = "https://0.0.0.0:$HttpsPort"
        Certificate = @{ Subject = $subject; Store = "My"; Location = "LocalMachine"; AllowInvalid = $false }
    } } }
}
$connectionBytes = [Text.Encoding]::UTF8.GetBytes($connectionString)
try {
    $protectedConnectionBytes = [Security.Cryptography.ProtectedData]::Protect(
        $connectionBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
    $connectionSecretCreated = $true
    [IO.File]::WriteAllBytes($connectionSecretPath,$protectedConnectionBytes)
} finally {
    [Array]::Clear($connectionBytes,0,$connectionBytes.Length)
    $connectionString = $null
}
$config | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $configPath -Encoding UTF8

    New-Service -Name $ServiceName -DisplayName "EnvanterTR" -Description "EnvanterTR inventory management web application" `
        -BinaryPathName "`"$(Join-Path $appPath 'EnvanterTR.Web.exe')`"" -StartupType Automatic -User "NT AUTHORITY\LocalService" | Out-Null
    $serviceCreated = $true
    & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/15000/restart/30000 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not configure service recovery actions." }
    Start-Service -Name $ServiceName

    $deadline = (Get-Date).AddMinutes(2)
    $apiReady = $false
    do {
        Start-Sleep -Seconds 3
        try {
            $statusUri = "https://localhost:$HttpsPort/api/status"
            if ($PSVersionTable.PSVersion.Major -ge 7) {
                $status = Invoke-RestMethod -Uri $statusUri -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
            } else {
                $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
                [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
                try { $status = Invoke-RestMethod -Uri $statusUri -TimeoutSec 5 -ErrorAction Stop }
                finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
            }
            if ($status.product -eq "EnvanterTR" -and $status.status -notin @("database-unavailable","database-not-configured")) {
                $apiReady = $true
                break
            }
        } catch { }
    } while ((Get-Date) -lt $deadline)
    if (-not $apiReady) { throw "The HTTPS service did not become reachable with a working PostgreSQL connection within two minutes." }

    Write-Output "Installed and started $ServiceName. HTTPS listens on port $HttpsPort; the installer did not change Windows Firewall rules."
    Write-Output "For a new empty database, run .\migrate.ps1 -InitialSetup from this package, then check /api/status."
    Write-Output "One-time company setup token (save securely): $($config.Setup.BootstrapToken)"
    Write-Output "One-time platform setup token (save securely): $($config.Setup.PlatformBootstrapToken)"
    Write-Warning "After creating both initial accounts, run .\disable-bootstrap.ps1 to remove these tokens from the live configuration."
    Write-Warning "Grant the LocalService account read access to the certificate's private key in certlm.msc. Verify /api/status from a client before opening the firewall port."
} catch {
    $installFailure = $_
    $serviceRemovalConfirmed = -not $serviceCreated
    if ($serviceCreated) {
        try {
            if ((Get-Service -Name $ServiceName -ErrorAction SilentlyContinue).Status -eq "Running") {
                Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
                (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
            }
            & sc.exe delete $ServiceName | Out-Null
            $deleteDeadline = (Get-Date).AddSeconds(30)
            do {
                if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) { $serviceRemovalConfirmed = $true; break }
                Start-Sleep -Seconds 1
            } while ((Get-Date) -lt $deleteDeadline)
        } catch { Write-Warning "Could not fully remove the failed EnvanterTR service '$ServiceName'. $($_.Exception.Message)" }
    }
    if ($appInstallStarted -and $serviceRemovalConfirmed -and (Test-Path -LiteralPath $appPath -PathType Container) -and
        -not ((Get-Item -LiteralPath $appPath).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        Get-ChildItem -LiteralPath $appPath -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        if (-not $appPathWasPresent) { Remove-Item -LiteralPath $appPath -Force -ErrorAction SilentlyContinue }
    } elseif ($appInstallStarted -and -not $serviceRemovalConfirmed) {
        Write-Warning "The failed service is still registered; application files were retained at '$appPath' so its binary path remains valid."
    }
    if ($connectionSecretCreated -and $serviceRemovalConfirmed) {
        Remove-Item -LiteralPath $connectionSecretPath -Force -ErrorAction SilentlyContinue
    }
    throw "Windows installation failed; rollback was attempted and DataRoot was retained. $($installFailure.Exception.Message)"
}
