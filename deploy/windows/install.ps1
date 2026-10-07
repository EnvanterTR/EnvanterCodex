param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [int]$HttpsPort = 8443
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
if ($PSVersionTable.PSVersion.Major -lt 5) { throw "Windows PowerShell 5.1 or PowerShell 7 is required." }
if ($HttpsPort -lt 1 -or $HttpsPort -gt 65535) { throw "HttpsPort must be between 1 and 65535." }

$package = (Resolve-Path $PackagePath).Path
$exe = Join-Path $package "EnvanterTR.Web.exe"
if (-not (Test-Path $exe)) { throw "Package is missing EnvanterTR.Web.exe: $package" }
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) { throw "Service '$ServiceName' already exists. Use update.ps1 for updates; this installer never overwrites an installation." }

$subject = (Read-Host "HTTPS certificate subject as shown in Local Computer > Personal (example: CN=inventory.example.com)").Trim()
if ([string]::IsNullOrWhiteSpace($subject)) { throw "A certificate subject is required." }
$store = [System.Security.Cryptography.X509Certificates.X509Store]::new("My", "LocalMachine")
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
try {
    $matches = @($store.Certificates | Where-Object {
        $_.Subject -eq $subject -and $_.HasPrivateKey -and $_.NotBefore -lt (Get-Date) -and $_.NotAfter -gt (Get-Date)
    })
    if ($matches.Count -ne 1) { throw "Expected exactly one valid LocalMachine\My certificate with a private key and subject '$subject'; found $($matches.Count)." }
} finally { $store.Close() }

$connectionSecure = Read-Host "PostgreSQL connection string for EnvanterTR (input hidden)" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($connectionSecure)
try { $connectionString = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr); $connectionSecure.Dispose() }
if ([string]::IsNullOrWhiteSpace($connectionString) -or $connectionString -notmatch "(?i)(Host|Server)=.+(Database|Initial Catalog)=.+") {
    throw "A PostgreSQL connection string containing Host and Database is required."
}

$appPath = Join-Path $InstallRoot "app"
$mediaPath = Join-Path $DataRoot "company-media"
$keysPath = Join-Path $DataRoot "data-protection-keys"
New-Item -ItemType Directory -Path $appPath,$mediaPath,$keysPath -Force | Out-Null
if (-not (Get-ChildItem -LiteralPath $appPath -Force | Select-Object -First 1)) {
    Copy-Item -Path (Join-Path $package "*") -Destination $appPath -Recurse -Force
} else { throw "Install directory is not empty: $appPath" }

# Restrict the tree before writing any database or bootstrap secrets into Production configuration.
& icacls.exe $InstallRoot /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not set application directory permissions." }
& icacls.exe $DataRoot /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)RX" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not set data directory permissions." }
foreach ($writableDataPath in @($mediaPath,$keysPath)) {
    & icacls.exe $writableDataPath /grant:r "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not grant the service access to $writableDataPath" }
}

$tokenBytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($tokenBytes) } finally { $random.Dispose() }
$bootstrapToken = [Convert]::ToBase64String($tokenBytes)
$tokenBytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $random.GetBytes($tokenBytes) } finally { $random.Dispose() }
$platformBootstrapToken = [Convert]::ToBase64String($tokenBytes)

$config = [ordered]@{
    ConnectionStrings = @{ EnvanterTR = $connectionString }
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
$configPath = Join-Path $appPath "appsettings.Production.json"
$config | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $configPath -Encoding UTF8

New-Service -Name $ServiceName -DisplayName "EnvanterTR" -Description "EnvanterTR inventory management web application" `
    -BinaryPathName "`"$(Join-Path $appPath 'EnvanterTR.Web.exe')`"" -StartupType Automatic -User "NT AUTHORITY\LocalService" | Out-Null
& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/15000/restart/30000 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not configure service recovery actions." }
Start-Service -Name $ServiceName
Write-Output "Installed and started $ServiceName. HTTPS listens on port $HttpsPort; the installer did not change Windows Firewall rules."
Write-Output "For a new empty database, run .\migrate.ps1 -InitialSetup from this package, then check /api/status."
Write-Output "One-time company setup token (save securely): $($config.Setup.BootstrapToken)"
Write-Output "One-time platform setup token (save securely): $($config.Setup.PlatformBootstrapToken)"
Write-Warning "After creating both initial accounts, run .\disable-bootstrap.ps1 to remove these tokens from the live configuration."
Write-Warning "Grant the LocalService account read access to the certificate's private key in certlm.msc if the service cannot load it. Verify /api/status from a client before opening the firewall port."
