param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $BackupDirectory
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $BackupDirectory -PathType Container)) {
    throw "Yedek klasörü bulunamadı: $BackupDirectory"
}
$backupRoot = (Resolve-Path -LiteralPath $BackupDirectory).Path
$manifestPath = Join-Path $backupRoot 'manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'manifest.json bulunamadı; bu klasör EnvanterTR yedek paketi değil.'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.format -cne 'envantertr-local-backup' -or $manifest.schemaVersion -ne 1) {
    throw 'Yedek manifest biçimi veya sürümü desteklenmiyor.'
}

$dumpName = [string]$manifest.databaseDump
$mediaName = [string]$manifest.companyMediaArchive
if ([System.IO.Path]::GetFileName($dumpName) -cne $dumpName -or $dumpName -notmatch '^database-[a-f0-9]{8}\.dump$') {
    throw 'Manifest veritabanı dosya adı beklenen biçimde değil.'
}
if ($mediaName -cne 'company-media.tar.gz') {
    throw 'Manifest medya dosyası beklenen biçimde değil.'
}
$keysName = [string]$manifest.dataProtectionKeysArchive
if ($keysName -and $keysName -cne 'data-protection-keys.tar.gz') {
    throw 'Manifest Data Protection anahtar arşivi beklenen biçimde değil.'
}
if (-not $keysName -and $null -ne $manifest.sha256.dataProtectionKeysArchive) {
    throw 'Manifest Data Protection anahtar özeti var ancak arşiv adı yok.'
}

function Assert-Sha256([string]$Path, [string]$Expected, [string]$Label) {
    if ($Expected -notmatch '^[a-fA-F0-9]{64}$') { throw "$Label için manifest özeti geçersiz." }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label dosyası bulunamadı." }
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if (-not $actual.Equals($Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label SHA-256 özeti manifest ile eşleşmiyor. Dosya değiştirilmiş veya bozulmuş olabilir."
    }
}

$dumpPath = Join-Path $backupRoot $dumpName
$mediaPath = Join-Path $backupRoot $mediaName
Assert-Sha256 $dumpPath ([string]$manifest.sha256.databaseDump) 'Veritabanı yedeği'
Assert-Sha256 $mediaPath ([string]$manifest.sha256.companyMediaArchive) 'Şirket medya arşivi'
if ($keysName) {
    $keysPath = Join-Path $backupRoot $keysName
    Assert-Sha256 $keysPath ([string]$manifest.sha256.dataProtectionKeysArchive) 'Data Protection anahtar arşivi'
}

& tar -tzf $mediaPath *> $null
if ($LASTEXITCODE -ne 0) { throw 'Şirket medya arşivi açılamadı veya içeriği okunamıyor.' }
if ($keysName) {
    & tar -tzf $keysPath *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Data Protection anahtar arşivi açılamadı veya içeriği okunamıyor.' }
}

$mount = "type=bind,source=$backupRoot,target=/backup,readonly"
& docker run --rm --network none --mount $mount postgres:17-alpine pg_restore --list "/backup/$dumpName" *> $null
if ($LASTEXITCODE -ne 0) { throw 'Veritabanı döküm dizini PostgreSQL araçlarıyla okunamadı.' }

Write-Output 'Yedek doğrulandı: manifest, SHA-256 özetleri, medya/varsa anahtar arşivi ve PostgreSQL döküm dizini okunabilir.'
Write-Output 'Bu komut veritabanını değiştirmez ve yedeği geri yüklemez.'
