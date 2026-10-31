param([string]$Image = 'envantertr-web-v1-candidate:20261020')
$ErrorActionPreference = 'Stop'

function Invoke-Docker([string[]]$Arguments) {
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed with exit code $LASTEXITCODE." }
}

function Get-DockerText([string[]]$Arguments) {
    $value = (& docker @Arguments | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Docker query failed with exit code $LASTEXITCODE." }
    return $value
}

$composeFile = Join-Path $PSScriptRoot 'compose.release-smoke.yaml'
$migrationDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../src/EnvanterTR.Web/Data/Migrations'))
$migrationCount = @((Get-ChildItem -LiteralPath $migrationDirectory -File |
    Where-Object Name -Match '^\d{14}_.+\.cs$' | Where-Object Name -NotMatch '\.Designer\.cs$')).Count
$project = 'envantertr-backup-drill-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$network = "${project}_acceptance"
$mediaVolume = "${project}_media"
$keysVolume = "${project}_keys"
$restoredMediaVolume = "${project}_restored_media"
$restoredKeysVolume = "${project}_restored_keys"
$restoreContainer = 'envantertr-backup-restore-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$archiveContainer = 'envantertr-backup-archive-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$extractContainer = 'envantertr-backup-extract-' + [Guid]::NewGuid().ToString('N').Substring(0, 10)
$backupDirectory = Join-Path ([IO.Path]::GetTempPath()) $project
$previousImage = $env:ENVANTERTR_CANDIDATE_IMAGE
$previousMigrationCount = $env:ENVANTERTR_EXPECTED_MIGRATIONS
$restorePassword = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
$restoreCreated = $false
$archiveCreated = $false
$extractCreated = $false
$mediaCreated = $false
$keysCreated = $false

try {
    New-Item -ItemType Directory -Path $backupDirectory | Out-Null
    $env:ENVANTERTR_CANDIDATE_IMAGE = $Image
    $env:ENVANTERTR_EXPECTED_MIGRATIONS = [string]$migrationCount
    $compose = @('compose', '--project-name', $project, '--file', $composeFile)
    Invoke-Docker ($compose + @('up', '--detach', 'database', 'migrate', 'web'))
    Invoke-Docker ($compose + @('run', '--rm', 'checks'))

    # Add only synthetic markers to these new, unique test volumes.
    Invoke-Docker @('run', '--rm', '--network', 'none', '--volume', "${mediaVolume}:/media", '--volume', "${keysVolume}:/keys", 'postgres:17-alpine', 'sh', '-ec', 'mkdir -p /media/synthetic /keys/synthetic; printf "release-media-fixture\n" > /media/synthetic/marker.txt; printf "release-key-fixture\n" > /keys/synthetic/marker.txt')

    $sourceDatabase = Get-DockerText ($compose + @('ps', '--quiet', 'database'))
    if ([string]::IsNullOrWhiteSpace($sourceDatabase)) { throw 'Synthetic source database container was not found.' }
    Invoke-Docker @('exec', $sourceDatabase, 'pg_dump', '--no-owner', '--no-acl', '--format=custom', '-U', 'release_smoke', '-d', 'release_smoke', '-f', '/tmp/release-backup.dump')
    & docker exec $sourceDatabase pg_restore --list /tmp/release-backup.dump *> $null
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL backup archive could not be listed.' }
    $dump = Join-Path $backupDirectory 'database.dump'
    Invoke-Docker @('cp', "${sourceDatabase}:/tmp/release-backup.dump", $dump)

    Invoke-Docker @('create', '--name', $archiveContainer, '--network', 'none', '--volume', "${mediaVolume}:/source-media:ro", '--volume', "${keysVolume}:/source-keys:ro", 'postgres:17-alpine', 'sh', '-ec', 'tar -czf /tmp/company-media.tar.gz -C /source-media .; tar -tzf /tmp/company-media.tar.gz >/dev/null; tar -czf /tmp/data-protection-keys.tar.gz -C /source-keys .; tar -tzf /tmp/data-protection-keys.tar.gz >/dev/null') | Out-Null
    $archiveCreated = $true
    Invoke-Docker @('start', '--attach', $archiveContainer)
    $mediaArchive = Join-Path $backupDirectory 'company-media.tar.gz'
    $keysArchive = Join-Path $backupDirectory 'data-protection-keys.tar.gz'
    Invoke-Docker @('cp', "${archiveContainer}:/tmp/company-media.tar.gz", $mediaArchive)
    Invoke-Docker @('cp', "${archiveContainer}:/tmp/data-protection-keys.tar.gz", $keysArchive)
    Invoke-Docker @('rm', $archiveContainer) | Out-Null
    $archiveCreated = $false

    Invoke-Docker @('volume', 'create', $restoredMediaVolume) | Out-Null
    $mediaCreated = $true
    Invoke-Docker @('volume', 'create', $restoredKeysVolume) | Out-Null
    $keysCreated = $true
    Invoke-Docker @('run', '--detach', '--name', $restoreContainer, '--network', $network, '--env', 'POSTGRES_USER=restore_smoke', '--env', 'POSTGRES_DB=restore_smoke', '--env', "POSTGRES_PASSWORD=$restorePassword", 'postgres:17-alpine') | Out-Null
    $restoreCreated = $true
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        & docker exec $restoreContainer pg_isready -U restore_smoke -d restore_smoke *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Disposable restore database did not become ready.' }
    Invoke-Docker @('cp', $dump, "${restoreContainer}:/tmp/release-backup.dump")
    Invoke-Docker @('exec', $restoreContainer, 'pg_restore', '--exit-on-error', '--no-owner', '--no-acl', '-U', 'restore_smoke', '-d', 'restore_smoke', '/tmp/release-backup.dump')

    $generateCounts = "SELECT string_agg(format('SELECT %L::text AS table_name, count(*)::bigint AS row_count FROM %I.%I', tablename, schemaname, tablename), ' UNION ALL ' ORDER BY tablename) FROM pg_tables WHERE schemaname = 'public'"
    $countSql = Get-DockerText @('exec', $sourceDatabase, 'psql', '-X', '-A', '-t', '-U', 'release_smoke', '-d', 'release_smoke', '-c', $generateCounts)
    if ([string]::IsNullOrWhiteSpace($countSql)) { throw 'Could not generate exact table row-count query.' }
    $countsQuery = "SELECT table_name || '|' || row_count::text FROM ($countSql) AS counts ORDER BY table_name"
    $sourceCounts = Get-DockerText @('exec', $sourceDatabase, 'psql', '-X', '-A', '-t', '-U', 'release_smoke', '-d', 'release_smoke', '-c', $countsQuery)
    $restoredCounts = Get-DockerText @('exec', $restoreContainer, 'psql', '-X', '-A', '-t', '-U', 'restore_smoke', '-d', 'restore_smoke', '-c', $countsQuery)
    if ($sourceCounts -cne $restoredCounts) { throw 'Exact source and restored table row counts differ.' }

    foreach ($archive in @($mediaArchive, $keysArchive)) {
        $entries = @(& tar -tzf $archive)
        if ($LASTEXITCODE -ne 0) { throw 'A media/key archive could not be listed.' }
        foreach ($entry in $entries) {
            $normalized = ([string]$entry).Replace('\', '/')
            if ($normalized.StartsWith('/') -or $normalized -match '^[A-Za-z]:' -or $normalized.Split('/') -contains '..') {
                throw 'A media/key archive contains a path outside its target directory.'
            }
        }
    }

    Invoke-Docker @('create', '--name', $extractContainer, '--network', 'none', '--volume', "${restoredMediaVolume}:/media", '--volume', "${restoredKeysVolume}:/keys", 'postgres:17-alpine', 'sh', '-ec', 'mkdir -p /media /keys; tar -xzf /tmp/company-media.tar.gz -C /media --no-same-owner --no-same-permissions; tar -xzf /tmp/data-protection-keys.tar.gz -C /keys --no-same-owner --no-same-permissions; printf "release-media-fixture\n" > /tmp/expected-media; printf "release-key-fixture\n" > /tmp/expected-key; cmp /tmp/expected-media /media/synthetic/marker.txt; cmp /tmp/expected-key /keys/synthetic/marker.txt') | Out-Null
    $extractCreated = $true
    Invoke-Docker @('cp', $mediaArchive, "${extractContainer}:/tmp/company-media.tar.gz")
    Invoke-Docker @('cp', $keysArchive, "${extractContainer}:/tmp/data-protection-keys.tar.gz")
    Invoke-Docker @('start', '--attach', $extractContainer)
    Invoke-Docker @('rm', $extractContainer) | Out-Null
    $extractCreated = $false

    $tableCount = @($sourceCounts -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    $hashes = @($dump, $mediaArchive, $keysArchive) | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() }
    Write-Output "PASS: synthetic Release backup restored into a separate PostgreSQL 17 container; exact row counts matched across $tableCount tables."
    Write-Output 'PASS: media and Data Protection key archives passed traversal validation and restored fixture-byte comparison.'
    Write-Output "PASS: backup artifact SHA-256: $($hashes -join ', ')."
    Write-Output 'SCOPE: disposable synthetic data only; this is not the live backup/restore or production rollback acceptance.'
}
finally {
    if ($extractCreated) { & docker rm --force $extractContainer *> $null }
    if ($archiveCreated) { & docker rm --force $archiveContainer *> $null }
    if ($restoreCreated) { & docker rm --force $restoreContainer *> $null }
    if ($mediaCreated) { & docker volume rm $restoredMediaVolume *> $null }
    if ($keysCreated) { & docker volume rm $restoredKeysVolume *> $null }
    & docker compose --project-name $project --file $composeFile down --volumes --remove-orphans *> $null
    $env:ENVANTERTR_CANDIDATE_IMAGE = $previousImage
    $env:ENVANTERTR_EXPECTED_MIGRATIONS = $previousMigrationCount
    if (Test-Path -LiteralPath $backupDirectory) { Remove-Item -LiteralPath $backupDirectory -Recurse -Force }
}
