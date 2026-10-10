param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$Commit = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $Commit) {
    $Commit = (& git -C $repoRoot rev-parse --verify HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Could not determine the source commit for this Windows package." }
}
if ($Commit -notmatch '^[0-9a-fA-F]{12,64}$') { throw "Commit must be a hexadecimal Git commit ID with at least 12 characters." }

$package = (Resolve-Path -LiteralPath $PackagePath).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path -LiteralPath $package -PathType Container)) { throw "Windows publish directory does not exist: $package" }
if ($package -eq $output -or $output.StartsWith($package + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Release output cannot be inside the publish directory."
}

$requiredFiles = @(
    "EnvanterTR.Web.exe", "Setup.cmd", "setup.ps1", "install.ps1", "preflight.ps1", "protect-connection-string.ps1", "configure-email.ps1", "prepare-postgresql.ps1",
    "migrate.ps1", "disable-bootstrap.ps1", "backup.ps1", "BackupFormat.psm1", "WindowsSecrets.psm1",
    "restore.ps1", "run-scheduled-backup.ps1", "send-backup-alert.ps1", "configure-backup-schedule.ps1",
    "check-backup-health.ps1", "update.ps1", "README.md"
)
foreach ($name in $requiredFiles) {
    $path = Join-Path $package $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Windows release is missing required file: $name" }
}

$forbiddenFiles = Get-ChildItem -LiteralPath $package -File -Force -Recurse | Where-Object {
    $_.Name -match '^(\.env($|\.)|appsettings\.Production\.json$|server\.env$|backup\.pass$)' -or
    $_.Extension -eq ".dpapi"
}
if ($forbiddenFiles) { throw "Windows release contains a private configuration or secret file: $($forbiddenFiles[0].FullName)" }

New-Item -ItemType Directory -Path $output -Force | Out-Null
$shortCommit = $Commit.Substring(0, 12).ToLowerInvariant()
$archiveName = "envantertr-windows-x64-$shortCommit.zip"
$archivePath = Join-Path $output $archiveName
$checksumPath = "$archivePath.sha256"
$temporaryArchive = "$archivePath.tmp-$([guid]::NewGuid().ToString('N'))"
$temporaryChecksum = "$checksumPath.tmp-$([guid]::NewGuid().ToString('N'))"

try {
    Compress-Archive -Path (Join-Path $package "*") -DestinationPath $temporaryArchive -CompressionLevel Optimal
    if (-not (Test-Path -LiteralPath $temporaryArchive -PathType Leaf) -or (Get-Item -LiteralPath $temporaryArchive).Length -le 0) {
        throw "Windows release ZIP was not created."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($temporaryArchive)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })
        foreach ($name in $requiredFiles) {
            if ($name -notin $entries) { throw "Windows release ZIP is missing required file: $name" }
        }
        $privateEntry = $entries | Where-Object { $_ -match '(^|/)(\.env($|\.)|appsettings\.Production\.json$|server\.env$|backup\.pass$)|\.dpapi$' } | Select-Object -First 1
        if ($privateEntry) { throw "Windows release ZIP contains private configuration or secret material: $privateEntry" }
    } finally { $zip.Dispose() }

    $hash = (Get-FileHash -LiteralPath $temporaryArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($temporaryChecksum, "$hash  $archiveName`n", [System.Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporaryArchive -Destination $archivePath -Force
    Move-Item -LiteralPath $temporaryChecksum -Destination $checksumPath -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "verify-release.ps1") -Destination (Join-Path $output "verify-release.ps1") -Force

    $recordedHash = ((Get-Content -LiteralPath $checksumPath -Raw).Trim() -split '\s+')[0]
    $actualHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($recordedHash -ne $actualHash) { throw "Windows release checksum verification failed." }
    Write-Output "Windows release: $archivePath"
    Write-Output "SHA-256: $actualHash"
} finally {
    Remove-Item -LiteralPath $temporaryArchive, $temporaryChecksum -Force -ErrorAction SilentlyContinue
}
