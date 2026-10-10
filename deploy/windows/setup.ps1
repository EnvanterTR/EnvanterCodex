$ErrorActionPreference = "Stop"

function Write-Step([string]$Text) { Write-Host "`n=== $Text ===" -ForegroundColor Cyan }
function Fail([string]$Text) { throw $Text }
function Read-PendingConnection([string]$Path) {
    $protectedBytes = [IO.File]::ReadAllBytes($Path)
    $clearBytes = $null
    $plain = $null
    try {
        $clearBytes = [Security.Cryptography.ProtectedData]::Unprotect(
            $protectedBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        $plain = [Text.Encoding]::UTF8.GetString($clearBytes)
        return ConvertTo-SecureString -String $plain -AsPlainText -Force
    } finally {
        [Array]::Clear($protectedBytes,0,$protectedBytes.Length)
        if ($clearBytes) { [Array]::Clear($clearBytes,0,$clearBytes.Length) }
        $plain = $null
    }
}
function Remove-PendingSetup([string]$Directory) {
    Remove-Item -LiteralPath (Join-Path $Directory 'setup-pending.dpapi'),(Join-Path $Directory 'setup-pending.json') -Force -ErrorAction SilentlyContinue
}
function Wait-ForReadyStatus([int]$Port) {
    $deadline = (Get-Date).AddMinutes(2)
    do {
        try {
            if ($PSVersionTable.PSVersion.Major -ge 7) {
                $status = Invoke-RestMethod -Uri "https://localhost:$Port/api/status" -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
            } else {
                $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
                [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
                try { $status = Invoke-RestMethod -Uri "https://localhost:$Port/api/status" -TimeoutSec 5 -ErrorAction Stop }
                finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
            }
            if ($status.product -eq 'EnvanterTR' -and $status.status -eq 'ready') { return $true }
        } catch { }
        Start-Sleep -Seconds 3
    } while ((Get-Date) -lt $deadline)
    return $false
}

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $scriptPath = $MyInvocation.MyCommand.Path.Replace("'", "''")
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes("& '$scriptPath'"))
    $hostExe = (Get-Process -Id $PID).Path
    Start-Process -FilePath $hostExe -Verb RunAs -ArgumentList "-NoProfile -NoExit -ExecutionPolicy Bypass -EncodedCommand $encoded"
    exit
}

try {
    Write-Host "EnvanterTR Windows Server kurulum yardimcisi" -ForegroundColor Green
    Write-Host "Bu paket ofis ekibi sunucusu içindir. Windows 11 tek bilgisayar kurulumu bu pakette yoktur."
    Write-Host "Bu akış yeni bir yerel PostgreSQL veritabanı hazırlar; var olan veritabanını değiştirmez."

    $os = Get-CimInstance Win32_OperatingSystem
    if ($os.Caption -notmatch 'Windows Server 2019|Windows Server 2022') {
        Fail "Desteklenen sistem Windows Server 2019/2022 x64. Bulunan: $($os.Caption)."
    }
    $packagePath = (Resolve-Path -LiteralPath $PSScriptRoot).Path
    foreach ($file in @('EnvanterTR.Web.exe','preflight.ps1','prepare-postgresql.ps1','install.ps1','migrate.ps1')) {
        if (-not (Test-Path -LiteralPath (Join-Path $packagePath $file) -PathType Leaf)) {
            Fail "Paket eksik: $file. Doğrulanmış Windows ZIP'ini yeniden açın."
        }
    }

    $pendingDirectory = Join-Path "$env:ProgramData\EnvanterTR" 'secrets'
    $pendingSecretPath = Join-Path $pendingDirectory 'setup-pending.dpapi'
    $pendingMetadataPath = Join-Path $pendingDirectory 'setup-pending.json'
    $pendingSecretExists = Test-Path -LiteralPath $pendingSecretPath -PathType Leaf
    $existingService = Get-Service -Name 'EnvanterTR' -ErrorAction SilentlyContinue
    if ($existingService -and -not $pendingSecretExists) {
        Fail "EnvanterTR servisi zaten kurulu. Mevcut veriyi korumak için ilk kurulum burada durduruldu; güncellemede update.ps1 kullanın."
    }
    if ($existingService -and $pendingSecretExists) {
        if (-not (Test-Path -LiteralPath $pendingMetadataPath -PathType Leaf)) {
            Fail "Yarım kurulum işareti eksik. Mevcut servis ve veriler korundu; destek için $pendingDirectory klasörünü saklayın."
        }
        $pendingMetadata = Get-Content -LiteralPath $pendingMetadataPath -Raw | ConvertFrom-Json
        if (-not $pendingMetadata.DnsName -or -not $pendingMetadata.HttpsPort) {
            Fail "Yarım kurulum ayarları eksik. Mevcut servis ve veriler korundu; destek için $pendingDirectory klasörünü saklayın."
        }
        $httpsPort = [int]$pendingMetadata.HttpsPort
        Write-Step "Yarım ilk kurulumdan güvenli devam"
        Write-Host "Mevcut EnvanterTR veritabanı ve servisi korunacak; ilk şema adımı yeniden deneniyor."
        & (Join-Path $packagePath 'migrate.ps1') -InitialSetup
        if (-not (Wait-ForReadyStatus -Port $httpsPort)) {
            Fail "Migration komutu bitti ancak API ready durumuna geçmedi. Yarım kurulum sırrı korundu; sorunu giderdikten sonra Setup.cmd dosyasını yeniden çalıştırın."
        }
        Remove-PendingSetup -Directory $pendingDirectory
        Write-Host "İlk kurulum tamamlandı. Adres: https://$($pendingMetadata.DnsName):$httpsPort" -ForegroundColor Green
        Write-Host "İlk yönetici anahtarları ilk kurulum denemesinde gösterildi; güvenli kayıtlı anahtarları kullanın."
        return
    }

    Write-Step "Sunucu adresi"
    do {
        $dnsName = (Read-Host "Çalışanların tarayıcıda kullanacağı DNS adı (ör. envanter.sirketiniz.com)").Trim().TrimEnd('.')
        $parsedIpAddress = $null
        $isIpAddress = [Net.IPAddress]::TryParse($dnsName,[ref]$parsedIpAddress)
        $validDns = $dnsName -match '^(?=.{1,253}$)([A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$'
        if ($dnsName -ieq 'localhost' -or $isIpAddress -or -not $validDns) {
            Write-Host "Bir DNS adı girin. localhost veya IP adresi, ekip HTTPS sertifikası için uygun değildir." -ForegroundColor Yellow
            $dnsName = ''
        }
    } while ([string]::IsNullOrWhiteSpace($dnsName))

    $pgPortInput = Read-Host "Yerel PostgreSQL 17 portu [5432]"
    $pgPort = 5432
    if ($pgPortInput -and (-not [int]::TryParse($pgPortInput,[ref]$pgPort) -or $pgPort -lt 1 -or $pgPort -gt 65535)) {
        Fail "PostgreSQL portu 1–65535 arasında bir sayı olmalıdır."
    }
    $httpsPortInput = Read-Host "EnvanterTR HTTPS portu [8443]"
    $httpsPort = 8443
    if ($httpsPortInput -and (-not [int]::TryParse($httpsPortInput,[ref]$httpsPort) -or $httpsPort -lt 1 -or $httpsPort -gt 65535)) {
        Fail "HTTPS portu 1–65535 arasında bir sayı olmalıdır."
    }

    Write-Step "TLS sertifikası"
    $store = [Security.Cryptography.X509Certificates.X509Store]::new('My','LocalMachine')
    $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
    try {
        $now = Get-Date
        $certificates = @($store.Certificates | Where-Object {
            $candidate = $_
            $eku = $candidate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' } | Select-Object -First 1
            $validEku = (-not $eku) -or (@($eku.EnhancedKeyUsages | Where-Object { $_.Value -eq '1.3.6.1.5.5.7.3.1' }).Count -gt 0)
            $candidate.HasPrivateKey -and $candidate.NotBefore -lt $now -and $candidate.NotAfter -gt $now -and $validEku
        } | Sort-Object NotAfter -Descending)
        if ($certificates.Count -eq 0) {
            Fail "Local Computer > Personal deposunda geçerli ve özel anahtarlı sunucu TLS sertifikası yok. Sertifikayı önce bu depoya yükleyin."
        }
        for ($index=0; $index -lt $certificates.Count; $index++) {
            $certificate = $certificates[$index]
            Write-Host ("[{0}] {1} | bitiş {2:yyyy-MM-dd} | {3}" -f ($index+1),$certificate.Subject,$certificate.NotAfter,$certificate.Thumbprint)
        }
        $selectionText = Read-Host "Kullanılacak sertifika numarası"
        $selection = 0
        if (-not [int]::TryParse($selectionText,[ref]$selection) -or $selection -lt 1 -or $selection -gt $certificates.Count) {
            Fail "Sertifika numarası geçerli değil."
        }
        $selectedCertificate = $certificates[$selection-1]
    } finally { $store.Close() }

    Write-Step "Önkoşul kontrolü"
    $ps51 = Get-Command powershell.exe -ErrorAction SilentlyContinue
    if (-not $ps51) { Fail "Windows PowerShell 5.1 bulunamadı." }
    $preflight = Join-Path $packagePath 'preflight.ps1'
    $preflightArgs = @(
        '-NoProfile','-ExecutionPolicy','Bypass','-File',$preflight,
        '-PackagePath',$packagePath,'-PostgreSqlHost','127.0.0.1','-PostgreSqlPort',[string]$pgPort,
        '-HttpsPort',[string]$httpsPort,'-DnsName',$dnsName,'-CertificateSubject',$selectedCertificate.Subject
    )
    & $ps51.Source @preflightArgs
    if ($LASTEXITCODE -ne 0) { Fail "Önkoşul kontrolü başarısız. Kırmızı maddeleri giderip Setup.cmd dosyasını yeniden çalıştırın." }

    $psql = Join-Path "$env:ProgramFiles\PostgreSQL\17\bin" 'psql.exe'
    if (-not (Test-Path -LiteralPath $psql -PathType Leaf)) {
        Fail "PostgreSQL 17 sunucu yazılımı bulunamadı. Resmî PostgreSQL 17 x64 sunucu kurulumunu yapın, ardından Setup.cmd dosyasını yeniden başlatın."
    }

    Write-Host "`nKurulum şu işlemleri yapacak:" -ForegroundColor Yellow
    Write-Host "• Yeni envantertr veritabanı ve envantertr_app rolü oluşturacak."
    Write-Host "• EnvanterTR servis dosyalarını yükleyip HTTPS sağlık kontrolü yapacak."
    Write-Host "• Yeni veritabanı şemasını başlatacak. Var olan veritabanı/rol üzerine yazmaz."
    Write-Host "• Windows Güvenlik Duvarı veya DNS ayarlarını değiştirmeyecek."
    $confirmation = Read-Host "Devam etmek için INSTALL yazın"
    if ($confirmation -cne 'INSTALL') { Write-Host "Kurulum iptal edildi; hiçbir veritabanı oluşturulmadı." -ForegroundColor Yellow; return }

    $databaseSetup = $null
    $resumeConnectionSecure = $null
    try {
        if ($pendingSecretExists) {
            Write-Step "Önceki kurulumdan güvenli devam"
            Write-Host "Bu makinede oluşturulmuş şifreli kurulum sırrı bulundu. Yeni veritabanı oluşturulmayacak."
            $resumeConnectionSecure = Read-PendingConnection -Path $pendingSecretPath
            $pendingMetadata = Get-Content -LiteralPath $pendingMetadataPath -Raw | ConvertFrom-Json
            if (-not $pendingMetadata.DnsName -or -not $pendingMetadata.HttpsPort -or
                -not $pendingMetadata.PostgreSqlPort -or -not $pendingMetadata.CertificateThumbprint) {
                Fail "Yarım kurulum ayarları eksik. Şifreli sır korundu; $pendingDirectory klasörünü saklayıp destek alın."
            }
            if ([int]$pendingMetadata.HttpsPort -ne $httpsPort -or [int]$pendingMetadata.PostgreSqlPort -ne $pgPort -or
                $pendingMetadata.DnsName -ine $dnsName -or $pendingMetadata.CertificateThumbprint -ne $selectedCertificate.Thumbprint) {
                Fail "Yarım kurulum farklı DNS, port veya TLS sertifikasıyla sürdürülemez. İlk denemede kullanılan ayarları seçin."
            }
        } else {
            Write-Step "Yeni EnvanterTR veritabanı"
            New-Item -ItemType Directory -Path $pendingDirectory -Force | Out-Null
            & icacls.exe $pendingDirectory /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" /C | Out-Null
            if ($LASTEXITCODE -ne 0) { Fail "Kurulum devam sırrı için güvenli klasör izinleri ayarlanamadı." }
            $pendingMetadata = [ordered]@{ DnsName=$dnsName; HttpsPort=$httpsPort; PostgreSqlPort=$pgPort; CertificateThumbprint=$selectedCertificate.Thumbprint; CreatedAt=(Get-Date).ToUniversalTime().ToString('o') }
            $metadataJson = $pendingMetadata | ConvertTo-Json -Depth 4
            Set-Content -LiteralPath $pendingMetadataPath -Value $metadataJson -Encoding UTF8
            $databaseSetup = & (Join-Path $packagePath 'prepare-postgresql.ps1') -Port $pgPort -ReturnConnectionStringSecure -PendingConnectionPath $pendingSecretPath
            if (-not $databaseSetup -or -not $databaseSetup.ConnectionStringSecure) { Fail "PostgreSQL kurulum aracı güvenli bağlantı bilgisini üretemedi." }
            $resumeConnectionSecure = $databaseSetup.ConnectionStringSecure
        }

        Write-Step "Windows servisini kurma"
        & (Join-Path $packagePath 'install.ps1') -PackagePath $packagePath -HttpsPort $httpsPort `
            -CertificateSubject $selectedCertificate.Subject -CertificateThumbprint $selectedCertificate.Thumbprint `
            -DatabaseConnectionStringSecure $resumeConnectionSecure
        if ($resumeConnectionSecure) { $resumeConnectionSecure.Dispose(); $resumeConnectionSecure = $null }
    } finally {
        if ($databaseSetup -and $databaseSetup.ConnectionStringSecure) { $databaseSetup.ConnectionStringSecure.Dispose() }
        if ($resumeConnectionSecure) { $resumeConnectionSecure.Dispose() }
    }

    Write-Step "İlk veritabanı şemasını oluşturma"
    & (Join-Path $packagePath 'migrate.ps1') -InitialSetup
    if (-not (Wait-ForReadyStatus -Port $httpsPort)) {
        Fail "Veritabanı migration'ı çalıştı ancak API iki dakika içinde ready olmadı. Şifreli devam bilgisi saklandı; nedeni giderip Setup.cmd'yi yeniden çalıştırın."
    }
    Remove-PendingSetup -Directory $pendingDirectory

    Write-Host "`nKurulum adımları tamamlandı." -ForegroundColor Green
    Write-Host "HTTPS adresi: https://$dnsName`:$httpsPort"
    Write-Host "İlk şirket sahibi için /setup.html, platform yöneticisi için /platform-setup.html sayfasını açın."
    Write-Host "Kurulum sırasında gösterilen iki başlangıç anahtarını kullanın. Hesaplar oluşturulduktan sonra disable-bootstrap.ps1 çalıştırın."
    Write-Host "İstemcilerin erişimi için TCP $httpsPort güvenlik duvarı ve DNS kaydını kurum politikanıza göre tamamlayın."
} catch {
    Write-Host "`nKurulum tamamlanamadı: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Mevcut EnvanterTR verileri otomatik silinmedi. Sorunu giderdikten sonra aynı Setup.cmd akışını yeniden başlatabilirsiniz." -ForegroundColor Yellow
    exit 1
}
