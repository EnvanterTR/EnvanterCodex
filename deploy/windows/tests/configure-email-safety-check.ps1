$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) "configure-email.ps1"
$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $scriptPath),[ref]$tokens,[ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "configure-email.ps1 has PowerShell parse errors: $($parseErrors[0].Message)" }
$source = Get-Content -LiteralPath $scriptPath -Raw

function Assert-Pattern([string]$Name,[string]$Pattern) {
    if ($source -notmatch $Pattern) { throw "FAIL: $Name" }
    Write-Output "PASS: $Name"
}

Assert-Pattern 'SMTP password is requested as hidden SecureString input' 'Read-Host "SMTP password \(hidden\)" -AsSecureString'
Assert-Pattern 'SMTP password is protected with machine-scoped DPAPI' '(?s)ProtectedData\]::Protect\(\s*\$passwordBytes,\$null,\[Security\.Cryptography\.DataProtectionScope\]::LocalMachine'
Assert-Pattern 'only the DPAPI secret path is written to production settings' 'SmtpPasswordPath -Value \$newSecretPath'
Assert-Pattern 'public email URL is restricted to HTTPS' '\$publicBaseUrl\.Scheme -ne "https"'
Assert-Pattern 'configuration changes use an atomic replace with rollback copy' '(?s)\[IO\.File\]::Replace\(\$temporaryConfig,\$configPath,\$backupConfig,\$true\).*?\[IO\.File\]::Replace\(\$backupConfig,\$configPath,\$null,\$true\)'
Assert-Pattern 'running service readiness is checked before old settings are discarded' '(?s)if \(\$status\.status -eq "ready"\).*?Remove-Item -LiteralPath \$backupConfig'
Assert-Pattern 'replacement secret is discarded if configuration fails' '(?s)catch \{\s*\$configurationFailure = \$_.*?Remove-Item -LiteralPath \$newSecretPath'

$publish = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) "publish.ps1") -Raw
$release = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) "package-release.ps1") -Raw
if ($publish -notmatch 'Copy-Item.*configure-email\.ps1' -or $release -notmatch '"configure-email\.ps1"') {
    throw "FAIL: Windows release must include configure-email.ps1."
}
Write-Output 'PASS: Windows release includes the SMTP configuration command.'
