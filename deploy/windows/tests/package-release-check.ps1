$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$scratchRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("envantertr-package-check-" + [guid]::NewGuid().ToString("N"))
$fixture = Join-Path $scratchRoot "publish"
$output = Join-Path $scratchRoot "release"
$requiredFiles = @(
    "EnvanterTR.Web.exe", "install.ps1", "protect-connection-string.ps1", "prepare-postgresql.ps1",
    "migrate.ps1", "disable-bootstrap.ps1", "backup.ps1", "BackupFormat.psm1", "WindowsSecrets.psm1",
    "restore.ps1", "run-scheduled-backup.ps1", "send-backup-alert.ps1", "configure-backup-schedule.ps1",
    "check-backup-health.ps1", "update.ps1", "README.md"
)

try {
    New-Item -ItemType Directory -Path $fixture -Force | Out-Null
    foreach ($name in $requiredFiles) {
        Set-Content -LiteralPath (Join-Path $fixture $name) -Value "fixture: $name" -NoNewline
    }

    & (Join-Path $PSScriptRoot "..\package-release.ps1") -PackagePath $fixture -OutputDirectory $output -Commit "0123456789abcdef0123456789abcdef01234567"
    $archive = Join-Path $output "envantertr-windows-x64-0123456789ab.zip"
    $checksum = "$archive.sha256"
    if (-not (Test-Path -LiteralPath $archive -PathType Leaf) -or -not (Test-Path -LiteralPath $checksum -PathType Leaf)) {
        throw "Release ZIP or checksum sidecar was not created."
    }
    $expectedHash = ((Get-Content -LiteralPath $checksum -Raw).Trim() -split '\s+')[0]
    $actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedHash -ne $actualHash) { throw "Release checksum sidecar does not match the ZIP." }

    $secretPath = Join-Path $fixture "nested"
    New-Item -ItemType Directory -Path $secretPath -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $secretPath ".env.production") -Value "should not ship" -NoNewline
    $rejected = $false
    try {
        & (Join-Path $PSScriptRoot "..\package-release.ps1") -PackagePath $fixture -OutputDirectory (Join-Path $scratchRoot "rejected") -Commit "0123456789abcdef0123456789abcdef01234567"
    } catch { $rejected = $true }
    if (-not $rejected) { throw "Release packaging accepted a forbidden hidden environment file." }

    Write-Output "Windows ZIP and SHA-256 packaging checks passed."
} finally {
    if (Test-Path -LiteralPath $scratchRoot -PathType Container) {
        Remove-Item -LiteralPath $scratchRoot -Recurse -Force
    }
}
