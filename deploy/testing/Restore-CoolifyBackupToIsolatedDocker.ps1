param(
    [Parameter(Mandatory = $true)]
    [string] $BackupPath,

    [string] $ManifestPath,
    [string] $ContainerName = 'envantertr-f1-restore-20261102',
    [string] $DatabaseName = 'envantertr_restorecheck',
    [string] $DatabaseUser = 'envantertr_f1'
)

$ErrorActionPreference = 'Stop'

function Invoke-DockerArgs {
    param([Parameter(Mandatory = $true)][string[]] $DockerArguments)
    $output = & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker operation failed (exit $LASTEXITCODE): docker $($DockerArguments -join ' ')"
    }
    return $output
}

$backupFile = (Resolve-Path -LiteralPath $BackupPath).Path
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path.TrimEnd('\') + '\'
if ($backupFile.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Keep production backup files outside the Git worktree.'
}
if (-not $ManifestPath) {
    $ManifestPath = "$backupFile.sha256"
}
$manifestFile = (Resolve-Path -LiteralPath $ManifestPath).Path
if ($manifestFile.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Keep backup checksum manifests outside the Git worktree.'
}

$manifestText = (Get-Content -LiteralPath $manifestFile -Raw).Trim()
if ($manifestText -notmatch '^([a-fA-F0-9]{64})\s+') {
    throw 'The SHA-256 manifest format is invalid.'
}
$expectedHash = $Matches[1].ToLowerInvariant()
$actualHash = (Get-FileHash -LiteralPath $backupFile -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) {
    throw 'The downloaded backup does not match its SHA-256 manifest.'
}

$containerJson = Invoke-DockerArgs -DockerArguments @('inspect', '--format', '{{json .}}', $ContainerName) | ConvertFrom-Json
$portBindingCount = @($containerJson.HostConfig.PortBindings.PSObject.Properties).Count
if ($containerJson.State.Status -ne 'running' -or
    $containerJson.Config.Image -ne 'postgres:17-alpine' -or
    $containerJson.HostConfig.NetworkMode -ne 'none' -or
    $portBindingCount -ne 0 -or
    @($containerJson.Mounts).Count -ne 0) {
    throw 'The target must be the expected running, network-isolated, unmounted PostgreSQL container.'
}

$ready = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'pg_isready', '-U', $DatabaseUser, '-d', $DatabaseName)
if ($ready -notmatch 'accepting connections') {
    throw 'The isolated PostgreSQL target is not ready.'
}
$existingTables = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'psql', '-X', '-At', '-U', $DatabaseUser, '-d', $DatabaseName, '-c', "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'")
if ([int]($existingTables | Select-Object -Last 1) -ne 0) {
    throw 'The target database is not empty; refusing to overwrite it.'
}

$containerBackup = '/tmp/envantertr-f1-production.dump'
Invoke-DockerArgs -DockerArguments @('cp', $backupFile, "${ContainerName}:$containerBackup") | Out-Null
try {
    Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'pg_restore', '--list', $containerBackup) | Out-Null
    Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'pg_restore', '--exit-on-error', '--no-owner', '--no-privileges', '-U', $DatabaseUser, '-d', $DatabaseName, $containerBackup) | Out-Null

    $publicTables = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'psql', '-X', '-At', '-U', $DatabaseUser, '-d', $DatabaseName, '-c', "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'")
    $migrationRows = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'psql', '-X', '-At', '-U', $DatabaseUser, '-d', $DatabaseName, '-c', 'SELECT count(*) FROM public."__EFMigrationsHistory"')
    $latestMigration = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'psql', '-X', '-At', '-U', $DatabaseUser, '-d', $DatabaseName, '-c', 'SELECT max("MigrationId") FROM public."__EFMigrationsHistory"')
    $companies = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'psql', '-X', '-At', '-U', $DatabaseUser, '-d', $DatabaseName, '-c', 'SELECT count(*) FROM public."Companies"')
    $assets = Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'psql', '-X', '-At', '-U', $DatabaseUser, '-d', $DatabaseName, '-c', 'SELECT count(*) FROM public."Assets"')

    Write-Output 'ISOLATED_COOLIFY_BACKUP_RESTORE_OK'
    Write-Output "SHA256: $actualHash"
    Write-Output "Public tables: $($publicTables | Select-Object -Last 1)"
    Write-Output "Migration rows: $($migrationRows | Select-Object -Last 1)"
    Write-Output "Latest migration: $($latestMigration | Select-Object -Last 1)"
    Write-Output "Companies: $($companies | Select-Object -Last 1)"
    Write-Output "Assets: $($assets | Select-Object -Last 1)"
}
finally {
    Invoke-DockerArgs -DockerArguments @('exec', $ContainerName, 'rm', '-f', $containerBackup) | Out-Null
}
