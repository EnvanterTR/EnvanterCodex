param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$ServiceName = "EnvanterTR"
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
$appPath = Join-Path $InstallRoot "app"
$configPath = Join-Path $appPath "appsettings.Production.json"
if (-not (Test-Path -LiteralPath $configPath)) { throw "Production configuration is missing." }
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$httpsUrl = [uri]$config.Kestrel.Endpoints.Https.Url
$url = "https://localhost:{0}" -f $httpsUrl.Port
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $companySetup = Invoke-RestMethod -Uri "$url/api/setup/status" -SkipCertificateCheck -TimeoutSec 10
    $platformSetup = Invoke-RestMethod -Uri "$url/api/platform/bootstrap/status" -SkipCertificateCheck -TimeoutSec 10
} else {
    $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
    [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    try {
        $companySetup = Invoke-RestMethod -Uri "$url/api/setup/status" -TimeoutSec 10
        $platformSetup = Invoke-RestMethod -Uri "$url/api/platform/bootstrap/status" -TimeoutSec 10
    } finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
}
if ($companySetup.setupRequired -or $platformSetup.setupRequired) {
    throw "Both the first company owner and platform administrator must be created before bootstrap tokens can be removed."
}

$service = Get-Service -Name $ServiceName -ErrorAction Stop
$wasRunning = $service.Status -eq "Running"
$original = Get-Content -LiteralPath $configPath -Raw
if ($wasRunning) {
    Stop-Service -Name $ServiceName
    (Get-Service -Name $ServiceName).WaitForStatus("Stopped", [TimeSpan]::FromSeconds(60))
}
try {
    $config.Setup.PSObject.Properties.Remove("BootstrapToken")
    $config.Setup.PSObject.Properties.Remove("PlatformBootstrapToken")
    if ($config.Setup.PSObject.Properties.Count -eq 0) { $config.PSObject.Properties.Remove("Setup") }
    $temporary = "$configPath.$([guid]::NewGuid().ToString('N')).tmp"
    $config | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $configPath -Force
} catch {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    Set-Content -LiteralPath $configPath -Value $original -Encoding UTF8
    throw
} finally {
    if ($wasRunning) { Start-Service -Name $ServiceName }
}
Write-Output "Bootstrap tokens were removed from the active configuration. Keep the production configuration ACL-restricted."
