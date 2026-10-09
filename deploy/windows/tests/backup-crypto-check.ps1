$ErrorActionPreference = "Stop"
$modulePath = Join-Path $PSScriptRoot "..\BackupFormat.psm1"
$backupScript = Join-Path $PSScriptRoot "..\backup.ps1"
$restoreScript = Join-Path $PSScriptRoot "..\restore.ps1"
foreach ($path in @($modulePath,$backupScript,$restoreScript)) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $path),[ref]$tokens,[ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) { throw "PowerShell parse failed for $path`: $($parseErrors.Message -join '; ')" }
}
Import-Module (Resolve-Path $modulePath) -Force -DisableNameChecking

$work = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-crypto-check-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work | Out-Null
$source = Join-Path $work "source.zip"
$fixture = Join-Path $work "fixture"
$encrypted = Join-Path $work "backup.etbackup"
$decrypted = Join-Path $work "decrypted.zip"
$tampered = Join-Path $work "tampered.etbackup"
$cipher = Join-Path $work "cipher.bin"
$magic = [byte[]](0x45,0x54,0x42,0x4B,0x01)
$password = "crypto-check-passphrase-2026"
$mediaRoot = Join-Path $fixture "company-media/1"
$userPhoto = Join-Path $mediaRoot "users/42/profile.png"
$assetPhoto = Join-Path $mediaRoot "assets/PRN-004/photo.jpg"
$keyFile = Join-Path $fixture "data-protection-keys/key-2026.xml"
New-Item -ItemType Directory -Path (Split-Path $userPhoto), (Split-Path $assetPhoto), (Split-Path $keyFile) -Force | Out-Null
[IO.File]::WriteAllBytes($userPhoto,[byte[]](0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x70,0x72,0x6F,0x66,0x69,0x6C,0x65))
[IO.File]::WriteAllBytes($assetPhoto,[byte[]](0xFF,0xD8,0xFF,0xE0,0x61,0x73,0x73,0x65,0x74,0xFF,0xD9))
Set-Content -LiteralPath $keyFile -Value '<key id="fixture">data-protection-key</key>' -Encoding UTF8
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipFile = [IO.File]::Create($source)
$archive = [IO.Compression.ZipArchive]::new($zipFile,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $fixture -File -Recurse) {
        $entryName = $file.FullName.Substring($fixture.Length + 1).Replace('\','/')
        $entry = $archive.CreateEntry($entryName,[IO.Compression.CompressionLevel]::Optimal)
        $input = [IO.File]::OpenRead($file.FullName)
        $output = $entry.Open()
        try { $input.CopyTo($output) } finally { $output.Dispose(); $input.Dispose() }
    }
} finally { $archive.Dispose(); $zipFile.Dispose() }

# Keep the actual Windows backup path from silently dropping either class of files.
$backupSource = Get-Content -LiteralPath $backupScript -Raw
if ($backupSource -notmatch 'Copy-Item[^\r\n]+\$mediaSource[^\r\n]+company-media') { throw "Windows backup no longer includes company media." }
if ($backupSource -notmatch 'Copy-Item[^\r\n]+\$keysSource[^\r\n]+data-protection-keys') { throw "Windows backup no longer includes Data Protection keys." }

try {
    Protect-Backup $source $encrypted $password 200000 $magic $cipher
    Verify-And-Decrypt $encrypted $decrypted $password $magic $cipher
    if ((Get-FileHash $source -Algorithm SHA256).Hash -ne (Get-FileHash $decrypted -Algorithm SHA256).Hash) {
        throw "Encrypted backup round trip changed the input bytes."
    }
    $roundTrip = Join-Path $work "round-trip"
    [IO.Compression.ZipFile]::ExtractToDirectory($decrypted,$roundTrip)
    foreach ($relativePath in @("company-media/1/users/42/profile.png","company-media/1/assets/PRN-004/photo.jpg","data-protection-keys/key-2026.xml")) {
        if (-not (Test-Path -LiteralPath (Join-Path $roundTrip $relativePath) -PathType Leaf)) { throw "Encrypted backup round trip omitted $relativePath." }
    }

    $wrongPasswordRejected = $false
    try { Verify-And-Decrypt $encrypted $decrypted "incorrect-passphrase-2026" $magic $cipher }
    catch { $wrongPasswordRejected = $true }
    if (-not $wrongPasswordRejected) { throw "Wrong passphrase was not rejected." }

    Copy-Item -LiteralPath $encrypted -Destination $tampered
    $stream = [IO.File]::Open($tampered,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try {
        $stream.Position = $stream.Length - 1
        $lastByte = $stream.ReadByte()
        $stream.Position = $stream.Length - 1
        $stream.WriteByte(($lastByte -bxor 0x01))
    } finally { $stream.Dispose() }
    $tamperRejected = $false
    try { Verify-And-Decrypt $tampered $decrypted $password $magic $cipher }
    catch { $tamperRejected = $true }
    if (-not $tamperRejected) { throw "Tampered archive was not rejected." }
    Write-Output "Backup crypto checks passed: PowerShell syntax, media/key inclusion, encrypted round trip, wrong-passphrase rejection, and tamper rejection."
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
