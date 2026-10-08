$ErrorActionPreference = "Stop"
$modulePath = Join-Path (Split-Path $PSScriptRoot -Parent) "WindowsSecrets.psm1"
$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile($modulePath,[ref]$tokens,[ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "WindowsSecrets.psm1 has PowerShell parse errors: $($parseErrors[0].Message)" }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-windows-secrets-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
$secretPath = Join-Path $tempRoot "database-connection.dpapi"
$configPath = Join-Path $tempRoot "appsettings.Production.json"
$expected = "Host=127.0.0.1;Database=envanter;Username=app;Password=Test-only-secret-765!"
$plainBytes = [Text.Encoding]::UTF8.GetBytes($expected)
try {
    $protectedBytes = [Security.Cryptography.ProtectedData]::Protect(
        $plainBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
    [IO.File]::WriteAllBytes($secretPath,$protectedBytes)
} finally { [Array]::Clear($plainBytes,0,$plainBytes.Length) }

try {
    [ordered]@{ WindowsSecrets = @{ ConnectionStringPath = $secretPath }; Storage = @{ CompanyMediaPath = "media" } } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $configPath -Encoding UTF8
    $configText = Get-Content -LiteralPath $configPath -Raw
    if ($configText.Contains("Test-only-secret-765!")) { throw "FAIL: production settings expose the database password." }

    Import-Module $modulePath -Force
    $actual = Get-EnvanterTRConnectionString -ConfigurationPath $configPath
    if ($actual -cne $expected) { throw "FAIL: the LocalMachine DPAPI secret did not round trip." }
    Write-Output "PASS: database secret remains encrypted in configuration and opens through LocalMachine DPAPI."

    [IO.File]::WriteAllBytes($secretPath,[byte[]](1,2,3,4,5))
    $rejected = $false
    try { Get-EnvanterTRConnectionString -ConfigurationPath $configPath | Out-Null }
    catch { $rejected = $true }
    if (-not $rejected) { throw "FAIL: tampered DPAPI secret was accepted." }
    Write-Output "PASS: tampered or undecryptable database secrets fail closed."

    [ordered]@{ ConnectionStrings = @{ EnvanterTR = $expected } } |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $configPath -Encoding UTF8
    $legacy = Get-EnvanterTRConnectionString -ConfigurationPath $configPath
    if ($legacy -cne $expected) { throw "FAIL: legacy Windows installations cannot read their existing connection string." }
    Write-Output "PASS: existing pre-DPAPI installations remain readable for safe migration."
} finally {
    Remove-Module WindowsSecrets -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
