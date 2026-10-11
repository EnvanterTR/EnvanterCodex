$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$drillRoot = Join-Path $tempRoot ('envantertr-recovery-' + [Guid]::NewGuid().ToString('N'))
$passphrasePath = Join-Path $drillRoot 'passphrase.txt'
$backupRoot = Join-Path $drillRoot 'backup'
$restoredRoot = Join-Path $drillRoot 'restored'
$backupScript = Join-Path $projectRoot 'deploy/local/run-backup.ps1'
$restorePreviewScript = Join-Path $projectRoot 'deploy/local/restore-preview.ps1'
$backupToolProject = Join-Path $projectRoot 'tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj'
$privateAclModule = Join-Path $projectRoot 'deploy/local/PrivateAcl.psm1'
$composeFile = Join-Path $projectRoot 'deploy/local/compose.yaml'
$environmentFile = Join-Path $projectRoot 'deploy/local/.env'
$drillCompleted = $false
$mediaVolume = $null
$keysVolume = $null
$mediaProbeName = $null
$keysProbeName = $null
$mediaProbeCreated = $false
$keysProbeCreated = $false

if (-not (Test-Path -LiteralPath $environmentFile -PathType Leaf)) {
    throw 'Yerel Compose gizli ayar dosyası bulunamadı; önce deploy/local yönergeleriyle geliştirme ortamını kurun.'
}
foreach ($path in @($backupScript, $restorePreviewScript, $backupToolProject, $privateAclModule, $composeFile)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Kurtarma provası dosyası bulunamadı: $path" }
}

function Get-LocalRecordCounts {
    $query = 'SELECT (SELECT count(*) FROM "Companies"),(SELECT count(*) FROM "Employees"),(SELECT count(*) FROM "Assets"),(SELECT count(*) FROM "AssetModels"),(SELECT count(*) FROM "AssetBrands"),(SELECT count(*) FROM "Accessories"),(SELECT count(*) FROM "AccessoryModels"),(SELECT count(*) FROM "SupplierCatalogEntries"),(SELECT count(*) FROM "AssetEvents");'
    $output = & docker compose --env-file $environmentFile -f $composeFile exec -T database sh -c 'psql -X -A -t -F "|" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "$1"' sh $query 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Yerel Compose kayıt sayıları okunamadı: $($output -join ' ')" }
    $line = @($output | Where-Object { $_ -match '^\d+\|\d+\|\d+\|\d+\|\d+\|\d+\|\d+\|\d+\|\d+$' } | Select-Object -Last 1)
    if ($line.Count -ne 1) { throw 'Yerel Compose kayıt sayıları beklenen biçimde dönmedi.' }
    return @($line[0].Split('|') | ForEach-Object { [long]$_ })
}

function Get-LocalVolumeName([string] $destination) {
    $container = & docker compose --env-file $environmentFile -f $composeFile ps -q web 2>&1
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($container | Out-String).Trim())) {
        throw 'Yerel Compose web konteyneri bulunamadı; kurtarma provası durduruldu.'
    }
    $mountsJson = & docker inspect --format '{{json .Mounts}}' (($container | Out-String).Trim()) 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Yerel Compose volume adları okunamadı.' }
    $mounts = ($mountsJson | Out-String | ConvertFrom-Json)
    $mount = @($mounts | Where-Object { $_.Destination -ceq $destination -and $_.Type -ceq 'volume' }) | Select-Object -First 1
    if (-not $mount -or $mount.Name -notmatch '^[A-Za-z0-9_.-]+$') {
        throw "Beklenen yerel volume bulunamadı: $destination"
    }
    return [string]$mount.Name
}

function Add-TemporaryVolumeProbe([string] $volume, [string] $name, [string] $content) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($content))
    & docker run --rm --network none --volume "${volume}:/target" postgres:17-alpine sh -c 'set -eu; test ! -e "/target/$1"; printf "%s" "$2" | base64 -d > "/target/$1"' sh $name $encoded
    if ($LASTEXITCODE -ne 0) { throw "Geçici kurtarma kanıt dosyası yazılamadı: $name" }
}

function Remove-TemporaryVolumeProbe([string] $volume, [string] $name) {
    if ([string]::IsNullOrWhiteSpace($volume) -or [string]::IsNullOrWhiteSpace($name)) { return $false }
    & docker run --rm --network none --volume "${volume}:/target" postgres:17-alpine rm -f "/target/$name" *> $null
    return ($LASTEXITCODE -eq 0)
}

function Assert-ProbeInRestoredArchive([string] $archiveName, [string] $probeName, [string] $expectedHash) {
    $mount = "type=bind,source=$restoredRoot,target=/drill,readonly"
    $entries = @(& docker run --rm --network none --mount $mount postgres:17-alpine tar -tzf "/drill/$archiveName" 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "Geri yüklenen arşiv listelenemedi: $archiveName" }
    $entry = @($entries | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_ -match "(?:^|/)$([regex]::Escape($probeName))$" } | Select-Object -First 1)
    if ($entry.Count -ne 1) { throw "Geçici kanıt dosyası geri yüklenen arşivde bulunamadı: $probeName" }
    $hashOutput = & docker run --rm --network none --mount $mount postgres:17-alpine sh -c 'set -eu; tar -xOzf "/drill/$1" "$2" | sha256sum | cut -d " " -f 1' sh $archiveName $entry[0] 2>&1
    if ($LASTEXITCODE -ne 0 -or ([string]($hashOutput | Select-Object -Last 1)).Trim() -cne $expectedHash) {
        throw "Geri yüklenen kanıt dosyasının içeriği eşleşmiyor: $probeName"
    }
}

New-Item -ItemType Directory -Path $drillRoot | Out-Null
try {
    Import-Module $privateAclModule -Force
    Set-EnvanterTRPrivateAcl -Path $drillRoot -Kind Directory
    $passphrase = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    [IO.File]::WriteAllText($passphrasePath, $passphrase, [Text.UTF8Encoding]::new($false))
    $passphrase = $null
    Set-EnvanterTRPrivateAcl -Path $passphrasePath -Kind File

    $proofId = [Guid]::NewGuid().ToString('N')
    $mediaVolume = Get-LocalVolumeName '/var/lib/envantertr/company-media'
    $keysVolume = Get-LocalVolumeName '/var/lib/envantertr/data-protection-keys'
    $mediaProbeName = ".envantertr-recovery-$proofId.txt"
    $keysProbeName = ".envantertr-recovery-$proofId.txt"
    $mediaProbeContent = "EnvanterTR isolated restore media proof $proofId"
    $keysProbeContent = "EnvanterTR isolated restore key-volume proof $proofId"
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $mediaProbeHash = [BitConverter]::ToString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($mediaProbeContent))).Replace('-', '').ToLowerInvariant()
        $sha256.Initialize()
        $keysProbeHash = [BitConverter]::ToString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($keysProbeContent))).Replace('-', '').ToLowerInvariant()
    }
    finally { $sha256.Dispose() }
    $mediaProbeCreated = $true
    Add-TemporaryVolumeProbe $mediaVolume $mediaProbeName $mediaProbeContent
    $keysProbeCreated = $true
    Add-TemporaryVolumeProbe $keysVolume $keysProbeName $keysProbeContent

    $sourceRecordCounts = @(Get-LocalRecordCounts)

    & $backupScript -PassphraseFile $passphrasePath -OutputDirectory $backupRoot -RetentionDays 1
    if ($LASTEXITCODE -ne 0) { throw 'Şifreli yerel yedek oluşturma/doğrulama başarısız oldu.' }
    $archive = @(Get-ChildItem -LiteralPath $backupRoot -File -Filter 'envantertr-*.etbackup')
    if ($archive.Count -ne 1) { throw "Tek bir şifreli deneme yedeği bekleniyordu; bulunan: $($archive.Count)." }

    $mounts = @(
        '--mount', "type=bind,source=$projectRoot,target=/source",
        '--mount', "type=bind,source=$drillRoot,target=/drill"
    )
    & docker run --rm @mounts mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /source/tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj --configuration Release --no-launch-profile -- decrypt "/drill/backup/$($archive[0].Name)" /drill/restored /drill/passphrase.txt
    if ($LASTEXITCODE -ne 0) { throw 'Şifreli yedeğin güvenli açılması başarısız oldu.' }

    $previewOutput = @(& $restorePreviewScript -BackupDirectory $restoredRoot 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "İzole geri yükleme provası başarısız oldu: $($previewOutput -join ' ')" }
    $previewOutput | ForEach-Object { Write-Output $_ }
    $summary = $previewOutput -join "`n"
    if ($summary -notmatch 'Migration kaydı: ([1-9]\d*); medya dosyası: ([1-9]\d*); anahtar dosyası: ([1-9]\d*)\.') {
        throw 'İzole geri yükleme PostgreSQL şemasını, medya ve Data Protection anahtar dosyalarını doğrulamadı.'
    }
    Assert-ProbeInRestoredArchive 'company-media.tar.gz' $mediaProbeName $mediaProbeHash
    Assert-ProbeInRestoredArchive 'data-protection-keys.tar.gz' $keysProbeName $keysProbeHash
    $countsMatch = [regex]::Match($summary, 'Kayıt sayıları: Companies=(\d+), Employees=(\d+), Assets=(\d+), AssetModels=(\d+), AssetBrands=(\d+), Accessories=(\d+), AccessoryModels=(\d+), Suppliers=(\d+), AssetEvents=(\d+)\.')
    if (-not $countsMatch.Success) { throw 'İzole veritabanı kayıt sayıları prova çıktısında bulunamadı.' }
    $restoredRecordCounts = @(1..9 | ForEach-Object { [long]$countsMatch.Groups[$_].Value })
    if (-not [System.Linq.Enumerable]::SequenceEqual([long[]]$sourceRecordCounts, [long[]]$restoredRecordCounts)) {
        throw 'İzole geri yükleme kaynak PostgreSQL kayıt adetlerini korumadı.'
    }

    Write-Output ('Kaynak ve izole geri yükleme kayıt adetleri eşleşti: Companies={0}, Employees={1}, Assets={2}, AssetModels={3}, AssetBrands={4}, Accessories={5}, AccessoryModels={6}, Suppliers={7}, AssetEvents={8}.' -f $restoredRecordCounts)
    Write-Output ("Şifreli arşiv bayt sayısı: {0}. Yedek, açma, PostgreSQL ve medya provası başarılı." -f $archive[0].Length)
    $drillCompleted = $true
}
finally {
    $probeCleanupSucceeded = $true
    if ($mediaProbeCreated -and -not (Remove-TemporaryVolumeProbe $mediaVolume $mediaProbeName)) { $probeCleanupSucceeded = $false }
    if ($keysProbeCreated -and -not (Remove-TemporaryVolumeProbe $keysVolume $keysProbeName)) { $probeCleanupSucceeded = $false }
    $resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedDrillRoot = [IO.Path]::GetFullPath($drillRoot)
    if (-not $resolvedDrillRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Kurtarma provası geçici yolu sistem geçici dizininin dışında; temizlik durduruldu.'
    }
    if (Test-Path -LiteralPath $resolvedDrillRoot -PathType Container) {
        Remove-Item -LiteralPath $resolvedDrillRoot -Recurse -Force
    }
    if (-not $probeCleanupSucceeded) {
        if ($drillCompleted) { throw 'Kurtarma provası geçti ancak geçici volume kanıt dosyalarının temizliği doğrulanamadı.' }
        Write-Warning 'Geçici volume kanıt dosyalarının temizliği doğrulanamadı; medya ve anahtar volume köklerini kontrol edin.'
    }
    if ($drillCompleted -and $probeCleanupSucceeded) { Write-Output 'Geçici parola, yedek, açılmış kopya, volume kanıt dosyaları ve prova konteyneri temizlendi.' }
}
