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
$encrypted = Join-Path $work "backup.etbackup"
$decrypted = Join-Path $work "decrypted.zip"
$tampered = Join-Path $work "tampered.etbackup"
$cipher = Join-Path $work "cipher.bin"
$magic = [byte[]](0x45,0x54,0x42,0x4B,0x01)
$password = "crypto-check-passphrase-2026"
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
$sourceBytes = New-Object byte[] (1024 * 1024 + 37)
try { $random.GetBytes($sourceBytes) } finally { $random.Dispose() }
[IO.File]::WriteAllBytes($source,$sourceBytes)

try {
    Protect-Backup $source $encrypted $password 200000 $magic $cipher
    Verify-And-Decrypt $encrypted $decrypted $password $magic $cipher
    if ((Get-FileHash $source -Algorithm SHA256).Hash -ne (Get-FileHash $decrypted -Algorithm SHA256).Hash) {
        throw "Encrypted backup round trip changed the input bytes."
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
    Write-Output "Backup crypto checks passed: PowerShell syntax, round trip, wrong-passphrase rejection, and tamper rejection."
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    [Array]::Clear($sourceBytes,0,$sourceBytes.Length)
}
