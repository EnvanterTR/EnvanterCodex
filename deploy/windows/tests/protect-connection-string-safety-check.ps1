$ErrorActionPreference = "Stop"
$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) "protect-connection-string.ps1"
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptPath,[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw "protect-connection-string.ps1 has PowerShell parse errors: $($parseErrors[0].Message)" }
$source = Get-Content -LiteralPath $scriptPath -Raw
function Assert-Pattern([string]$Name,[string]$Pattern) {
    if ($source -notmatch $Pattern) { throw "FAIL: $Name" }
    Write-Output "PASS: $Name"
}

Assert-Pattern "DPAPI uses machine scope" '(?s)ProtectedData\]::Protect\(.*?DataProtectionScope\]::LocalMachine'
Assert-Pattern "secret is kept under DataRoot and protected by ACL" '(?s)\$secretsPath = Join-Path \$DataRoot "secrets".*?icacls\.exe \$secretsPath /inheritance:r.*?LOCAL SERVICE:\(OI\)\(CI\)RX'
Assert-Pattern "service is stopped before replacing production settings" '(?s)Stop-Service -Name \$ServiceName.*?\$config\.PSObject\.Properties\.Remove\("ConnectionStrings"\).*?\[IO\.File\]::Replace\(\$temporaryConfig,\$configPath,\$null\)'
Assert-Pattern "the new setting is accepted only after HTTPS reports ready" '(?s)status\.status -eq "ready".*?The service did not become ready'
Assert-Pattern "failed migration restores the old settings and removes its new secret" '(?s)\$configChanged\).*?WriteAllBytes\(\$configPath,\$originalConfigBytes\).*?\$secretCreated\).*?Remove-Item.*?\$serviceStopped\).*?Start-Service'
Assert-Pattern "migration cleans temporary secret and settings files" '(?s)finally\s*\{.*?\$temporarySecret,\$temporaryConfig.*?ZeroMemory|Array\]::Clear\(\$originalConfigBytes'

if ($source -match 'Write-Output[^\r\n]*\$connectionString') { throw "FAIL: migration must not print the database connection string." }
Write-Output "PASS: secret migration does not print credential material."

$publishPath = Join-Path (Split-Path $PSScriptRoot -Parent) "publish.ps1"
$publish = Get-Content -LiteralPath $publishPath -Raw
if ($publish -notmatch 'Copy-Item.*protect-connection-string\.ps1') { throw "FAIL: Windows package omits the legacy secret migration command." }
Write-Output "PASS: Windows package includes the legacy secret migration command."
