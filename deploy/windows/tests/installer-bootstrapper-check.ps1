param([Parameter(Mandatory = $true)][string]$InstallerPath)

$ErrorActionPreference = "Stop"
$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
if (-not (Test-Path -LiteralPath $installer -PathType Leaf) -or [System.IO.Path]::GetFileName($installer) -cne "Setup.exe") {
    throw "InstallerPath must point to the built Setup.exe."
}

$checksumPath = "$installer.sha256"
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) { throw "Setup.exe SHA-256 sidecar is missing." }
$line = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
if ($line -notmatch '^(?<hash>[0-9a-fA-F]{64})\s+\*?Setup\.exe$') { throw "Setup.exe SHA-256 sidecar has an invalid format." }
$expectedHash = $Matches.hash.ToLowerInvariant()
$actualHash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) { throw "Setup.exe SHA-256 verification failed." }

$existingTempDirectories = @(
    Get-ChildItem -LiteralPath ([System.IO.Path]::GetTempPath()) -Directory -Filter "EnvanterTR-Setup-*" -ErrorAction SilentlyContinue |
        ForEach-Object { $_.FullName }
)
$verifyOutput = @(& $installer --verify 2>&1)
$verifyExitCode = $LASTEXITCODE
if ($verifyExitCode -ne 0) { $verifyOutput | ForEach-Object { Write-Error $_ }; throw "Setup.exe --verify failed with exit code $verifyExitCode." }
$verifyText = $verifyOutput -join "`n"
$fileCountMatch = [regex]::Match($verifyText, 'Kurulum paketi doğrulandı: (?<count>[1-9][0-9]*) dosya, [0-9,.]+ bayt açılmış içerik\.')
if (-not $fileCountMatch.Success -or [int]$fileCountMatch.Groups["count"].Value -lt 8) {
    throw "Setup.exe verification output did not report a valid embedded payload."
}

$invalidArgumentOutput = @(& $installer --invalid-installer-check 2>&1)
$invalidArgumentExitCode = $LASTEXITCODE
if ($invalidArgumentExitCode -ne 2) { throw "Setup.exe did not reject an unsupported argument with exit code 2." }

$newTempDirectories = @(
    Get-ChildItem -LiteralPath ([System.IO.Path]::GetTempPath()) -Directory -Filter "EnvanterTR-Setup-*" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notin $existingTempDirectories } |
        ForEach-Object { $_.FullName }
)
if ($newTempDirectories.Count -gt 0) { throw "Verification unexpectedly created installer extraction directories: $($newTempDirectories -join ', ')" }

Write-Output "Windows Setup.exe verified: $installer"
Write-Output "SHA-256: $actualHash"
$verifyOutput | ForEach-Object { Write-Output $_ }
Write-Output "Unsupported arguments rejected; verification created no install files."
