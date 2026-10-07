param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $BackupDirectory
)

$ErrorActionPreference = 'Stop'

function Invoke-Docker([string[]] $Arguments) {
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker komutu başarısız oldu (kod $LASTEXITCODE)." }
}

if (-not (Test-Path -LiteralPath $BackupDirectory -PathType Container)) {
    throw 'Yedek klasörü bulunamadı. Önce şifreli arşivi yeni bir klasöre açın.'
}
$backupRoot = (Resolve-Path -LiteralPath $BackupDirectory).Path
$verifyScript = Join-Path $PSScriptRoot 'verify-backup.ps1'
& $verifyScript $backupRoot

$manifest = Get-Content -LiteralPath (Join-Path $backupRoot 'manifest.json') -Raw | ConvertFrom-Json
$dumpPath = Join-Path $backupRoot ([string]$manifest.databaseDump)
$mediaPath = Join-Path $backupRoot ([string]$manifest.companyMediaArchive)
$containerName = 'envantertr-restore-preview-' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$password = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$containerCreated = $false

try {
    # No published port, host volume, or network: restored data exists only in this disposable container.
    Invoke-Docker @('create', '--name', $containerName, '--network', 'none', '--env', "POSTGRES_PASSWORD=$password", 'postgres:17-alpine')
    $containerCreated = $true
    Invoke-Docker @('start', $containerName)

    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker exec $containerName pg_isready -U postgres *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Yalıtılmış PostgreSQL konteyneri 60 saniyede hazır olmadı.' }

    Invoke-Docker @('cp', $dumpPath, "${containerName}:/tmp/restore.dump")
    Invoke-Docker @('cp', $mediaPath, "${containerName}:/tmp/company-media.tar.gz")
    Invoke-Docker @('exec', $containerName, 'pg_restore', '--exit-on-error', '--no-owner', '--no-acl', '-U', 'postgres', '-d', 'postgres', '/tmp/restore.dump')

    $migrationTable = (& docker exec $containerName psql -X -A -t -U postgres -d postgres -c "SELECT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname='public' AND tablename='__EFMigrationsHistory')" | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Geri yüklenen veritabanında migration geçmişi sorgulanamadı.' }
    $migrationCount = 'yok'
    if ($migrationTable -eq 't') {
        $migrationCount = (& docker exec $containerName psql -X -A -t -U postgres -d postgres -c 'SELECT count(*) FROM "__EFMigrationsHistory"' | Out-String).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Migration geçmişi okunamadı.' }
    }

    $entries = @(& docker exec $containerName tar -tzf /tmp/company-media.tar.gz)
    if ($LASTEXITCODE -ne 0) { throw 'Medya arşivinin dizini okunamadı.' }
    foreach ($entry in $entries) {
        $normalized = ([string]$entry).Replace('\', '/')
        if ($normalized.StartsWith('/') -or $normalized -match '^[A-Za-z]:' -or $normalized.Split('/') -contains '..') {
            throw 'Medya arşivinde hedef klasör dışına çıkabilecek bir yol bulundu.'
        }
    }
    Invoke-Docker @('exec', $containerName, 'mkdir', '-p', '/tmp/media-preview')
    Invoke-Docker @('exec', $containerName, 'tar', '-xzf', '/tmp/company-media.tar.gz', '-C', '/tmp/media-preview', '--no-same-owner', '--no-same-permissions')
    $mediaFileCount = (& docker exec $containerName sh -c 'find /tmp/media-preview -type f | wc -l' | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Geçici medya açılımı doğrulanamadı.' }

    Write-Output 'Kurtarma önizlemesi başarılı: PostgreSQL dökümü izole konteynere geri yüklendi; medya arşivi geçici alana açıldı.'
    Write-Output "Migration kaydı: $migrationCount; medya dosyası: $mediaFileCount."
    Write-Output "Üretim/yerel uygulama veritabanına veya kalıcı Docker volume'una dokunulmadı."
}
finally {
    if ($containerCreated) {
        & docker rm --force $containerName *> $null
    }
}
