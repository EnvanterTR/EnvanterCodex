$ErrorActionPreference = "Stop"
$backupScript = Join-Path $PSScriptRoot "..\backup.ps1"
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $backupScript),[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw "backup.ps1 did not parse." }

foreach ($name in "Add-FileToHmac","Protect-Backup","Verify-And-Decrypt") {
    $definition = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name },$true)
    if (-not $definition) { throw "Missing crypto function: $name" }
    Invoke-Expression $definition.Extent.Text
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-crypto-check-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work | Out-Null
$source = Join-Path $work "source.zip"
$encrypted = Join-Path $work "backup.etbackup"
$decrypted = Join-Path $work "decrypted.zip"
$tampered = Join-Path $work "tampered.etbackup"
$script:cipherPath = Join-Path $work "cipher.bin"
$script:magic = [byte[]](0x45,0x54,0x42,0x4B,0x01)
$password = "crypto-check-passphrase-2026"
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
$sourceBytes = New-Object byte[] (1024 * 1024 + 37)
try { $random.GetBytes($sourceBytes) } finally { $random.Dispose() }
[IO.File]::WriteAllBytes($source,$sourceBytes)

try {
    Protect-Backup $source $encrypted $password 200000 $script:magic
    Verify-And-Decrypt $encrypted $decrypted $password $script:magic
    if ((Get-FileHash $source -Algorithm SHA256).Hash -ne (Get-FileHash $decrypted -Algorithm SHA256).Hash) {
        throw "Encrypted backup round trip changed the input bytes."
    }

    $wrongPasswordRejected = $false
    try { Verify-And-Decrypt $encrypted $decrypted "incorrect-passphrase-2026" $script:magic }
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
    try { Verify-And-Decrypt $tampered $decrypted $password $script:magic }
    catch { $tamperRejected = $true }
    if (-not $tamperRejected) { throw "Tampered archive was not rejected." }
    Write-Output "Backup crypto check passed: round trip, wrong-password rejection, and tamper rejection."
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    [Array]::Clear($sourceBytes,0,$sourceBytes.Length)
}
