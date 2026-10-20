[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [uri] $BaseUrl,
    [Parameter(Mandatory = $true)] [guid] $AssetId,
    [string] $TokenFile,
    [string] $LogPath
)

$ErrorActionPreference = 'Stop'
if (-not $BaseUrl.IsAbsoluteUri -or $BaseUrl.Scheme -ne 'https') {
    throw 'Güvenlik için HTTPS kullanan tam uygulama adresini girin.'
}

$secureToken = if ($TokenFile) {
    if (-not (Test-Path -LiteralPath $TokenFile -PathType Leaf)) { throw 'Şifreli API anahtarı dosyası bulunamadı.' }
    Get-Content -LiteralPath $TokenFile -Raw | ConvertTo-SecureString
} else {
    Read-Host 'EnvanterTR assets:write API anahtarı' -AsSecureString
}
$tokenPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
try { $token = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($tokenPointer) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($tokenPointer) }
if ([string]::IsNullOrWhiteSpace($token)) { throw 'API anahtarı boş olamaz.' }

$registryPaths = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
)
$items = foreach ($path in $registryPaths) {
    Get-ItemProperty -Path $path -ErrorAction SilentlyContinue | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_.DisplayName)
    } | ForEach-Object {
        $installDate = $null
        if ($_.InstallDate -match '^\d{8}$') {
            try { $installDate = [datetime]::ParseExact($_.InstallDate, 'yyyyMMdd', [Globalization.CultureInfo]::InvariantCulture).ToString('yyyy-MM-dd') }
            catch { $installDate = $null }
        }
        $architecture = if ($path -like '*WOW6432Node*') { 'x86' } else { 'unknown' }
        [ordered]@{
            name = ([string]$_.DisplayName).Trim()
            publisher = if ($_.Publisher) { ([string]$_.Publisher).Trim() } else { $null }
            version = if ($_.DisplayVersion) { ([string]$_.DisplayVersion).Trim() } else { $null }
            architecture = $architecture
            installDate = $installDate
        }
    }
}
$items = @($items | Sort-Object name,publisher,version -Unique)
if ($items.Count -gt 3000) { throw "Tarama $($items.Count) uygulama buldu; güvenli sınır 3.000. Eşitleme yapılmadı." }

$body = [ordered]@{ isComplete = $true; source = 'windows-registry'; items = $items } | ConvertTo-Json -Depth 5 -Compress
$headers = @{ Authorization = "Bearer $token" }
$endpoint = ([uri]::new($BaseUrl, "/api/v1/assets/$AssetId/installed-software")).AbsoluteUri
try {
    $result = Invoke-RestMethod -Uri $endpoint -Method Put -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
    $message = "Yazılım envanteri eşitlendi: $($result.itemCount) uygulama."
    Write-Output $message
    if ($LogPath) {
        $logDirectory = Split-Path -Parent $LogPath
        if ($logDirectory) { New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null }
        Add-Content -LiteralPath $LogPath -Encoding UTF8 -Value "$(Get-Date -Format o) OK count=$($result.itemCount)"
    }
} catch {
    if ($LogPath) {
        $logDirectory = Split-Path -Parent $LogPath
        if ($logDirectory) { New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null }
        $safeMessage = ([string]$_.Exception.Message -replace '[\r\n]+', ' ')
        if ($safeMessage.Length -gt 500) { $safeMessage = $safeMessage.Substring(0, 500) }
        Add-Content -LiteralPath $LogPath -Encoding UTF8 -Value "$(Get-Date -Format o) ERROR $safeMessage"
    }
    throw
} finally {
    $token = $null
    if ($secureToken) { $secureToken.Dispose() }
}
