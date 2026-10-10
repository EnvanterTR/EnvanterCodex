param(
    [Parameter(Mandatory = $true)][string]$ArchivePath,
    [string]$ChecksumPath = ""
)

$ErrorActionPreference = "Stop"
$archive = (Resolve-Path -LiteralPath $ArchivePath).Path
if (-not (Test-Path -LiteralPath $archive -PathType Leaf)) { throw "Windows release ZIP does not exist: $archive" }
if (-not $ChecksumPath) { $ChecksumPath = "$archive.sha256" }
$checksum = (Resolve-Path -LiteralPath $ChecksumPath).Path
if (-not (Test-Path -LiteralPath $checksum -PathType Leaf)) { throw "SHA-256 sidecar does not exist: $checksum" }

$line = (Get-Content -LiteralPath $checksum -Raw).Trim()
if ($line -notmatch '^(?<hash>[0-9a-fA-F]{64})\s+\*?(?<name>[^\r\n]+)$') { throw "SHA-256 sidecar has an invalid format." }
if ($Matches.name -cne [System.IO.Path]::GetFileName($archive)) { throw "SHA-256 sidecar names a different archive." }
$actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
if ($Matches.hash.ToLowerInvariant() -ne $actualHash) { throw "Windows release SHA-256 verification failed." }

$requiredFiles = @(
    "EnvanterTR.Web.exe", "Setup.cmd", "setup.ps1", "preflight.ps1", "install-postgresql.ps1", "install.ps1", "protect-connection-string.ps1", "prepare-postgresql.ps1",
    "migrate.ps1", "disable-bootstrap.ps1", "backup.ps1", "BackupFormat.psm1", "WindowsSecrets.psm1",
    "restore.ps1", "run-scheduled-backup.ps1", "send-backup-alert.ps1", "configure-backup-schedule.ps1",
    "check-backup-health.ps1", "update.ps1", "README.md"
)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    $entries = @($zip.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName })
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $entries) {
        $normalized = $entry.Replace([string][char]92, '/')
        if ($normalized.StartsWith('/') -or $normalized -match '(^|/)\.\.?(/|$)') { throw "Release ZIP contains an unsafe path: $entry" }
        if (-not $seen.Add($normalized)) { throw "Release ZIP contains a duplicate path: $entry" }
        if ($normalized -match '(^|/)(\.env($|\.)|appsettings\.Production\.json$|server\.env$|backup\.pass$)|\.dpapi$') {
            throw "Release ZIP contains private configuration or secret material: $entry"
        }
    }
    foreach ($name in $requiredFiles) {
        if ($name -notin $entries) { throw "Release ZIP is missing required file: $name" }
    }
} finally { $zip.Dispose() }

Write-Output "Windows release verified: $archive"
Write-Output "SHA-256: $actualHash"
