[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [uri] $BaseUrl,
    [Parameter(Mandatory = $true)] [guid] $AssetId,
    [ValidatePattern('^([01][0-9]|2[0-3]):[0-5][0-9]$')] [string] $DailyAt = '03:00'
)

$ErrorActionPreference = 'Stop'
if (-not $BaseUrl.IsAbsoluteUri -or $BaseUrl.Scheme -ne 'https' -or $BaseUrl.UserInfo -or $BaseUrl.Query -or $BaseUrl.Fragment) {
    throw 'Güvenlik için kullanıcı bilgisi/sorgusu olmayan, HTTPS kullanan tam uygulama adresini girin.'
}
if (-not $IsWindows -and $PSVersionTable.PSVersion.Major -ge 6) { throw 'Bu kurulum betiği Windows içindir.' }

$taskName = "EnvanterTR-KuruluYazilim-$($AssetId.ToString('N'))"
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$secureToken = Read-Host 'EnvanterTR assets:write API anahtarı (yalnız bu varlığa gerekli erişimi olan anahtar)' -AsSecureString
if ($secureToken.Length -eq 0) { throw 'API anahtarı boş olamaz.' }

$credentialDirectory = Join-Path $env:LOCALAPPDATA 'EnvanterTR\InstalledSoftware'
$credentialPath = Join-Path $credentialDirectory "$($AssetId.ToString('N')).dpapi"
$logPath = Join-Path $credentialDirectory "$($AssetId.ToString('N')).log"
$collectorPath = Join-Path $PSScriptRoot 'Collect-InstalledSoftware.ps1'
if (-not (Test-Path -LiteralPath $collectorPath -PathType Leaf)) { throw "Tarama betiği bulunamadı: $collectorPath" }
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) { throw "Bu varlık için zamanlanmış görev zaten var: $taskName" }

New-Item -ItemType Directory -Path $credentialDirectory -Force | Out-Null
$userSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
& icacls.exe $credentialDirectory /inheritance:r /grant:r "${userSid}:(OI)(CI)F" '*S-1-5-18:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'API anahtar klasörünün Windows erişim izinleri güvenli biçimde ayarlanamadı.' }

try {
    $secureToken | ConvertFrom-SecureString | Set-Content -LiteralPath $credentialPath -Encoding ASCII
    $powerShell = (Get-Process -Id $PID).Path
    $arguments = '-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "{0}" -BaseUrl "{1}" -AssetId "{2}" -TokenFile "{3}" -LogPath "{4}"' -f `
        $collectorPath, $BaseUrl.AbsoluteUri.TrimEnd('/'), $AssetId, $credentialPath, $logPath
    $action = New-ScheduledTaskAction -Execute $powerShell -Argument $arguments
    $runAt = [datetime]::Today.Add([TimeSpan]::ParseExact($DailyAt, 'hh\:mm', [Globalization.CultureInfo]::InvariantCulture))
    $dailyTrigger = New-ScheduledTaskTrigger -Daily -At $runAt
    $logonTrigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
    $principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 15)
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger @($dailyTrigger, $logonTrigger) -Principal $principal -Settings $settings `
        -Description "EnvanterTR varlık $AssetId için Windows kurulu yazılım taraması (günlük ve kullanıcı oturum açtığında)." | Out-Null
} catch {
    Remove-Item -LiteralPath $credentialPath -Force -ErrorAction SilentlyContinue
    throw
} finally {
    $secureToken.Dispose()
}

Write-Output "Görev kuruldu: $taskName"
Write-Output "Tetikleme: her gün $DailyAt ve kullanıcı Windows'ta oturum açtığında. Görev kullanıcı oturumu açıkken çalışır."
Write-Output "Durum günlüğü: $logPath"
Write-Output 'Anahtar Windows DPAPI ile bu kullanıcıya şifrelenir; görevi çalıştıran aynı Windows hesabından başka kullanıcı okuyamaz.'
