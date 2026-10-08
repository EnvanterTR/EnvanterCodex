$ErrorActionPreference = 'Stop'
$installPath = Join-Path $PSScriptRoot '..\install.ps1'
$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $installPath),[ref]$tokens,[ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "install.ps1 has PowerShell parse errors: $($parseErrors.Message -join '; ')" }
$source = Get-Content -LiteralPath $installPath -Raw

function Assert-Pattern([string]$Name,[string]$Pattern) {
    if ($source -notmatch $Pattern) { throw "FAIL: $Name" }
    Write-Output "PASS: $Name"
}

Assert-Pattern 'nested install and data roots are rejected' '(?s)InstallRoot and DataRoot must be separate, non-nested directories'
Assert-Pattern 'install and data roots cannot be reparse points' '(?s)foreach \(\$rootPath in @\(\$InstallRoot,\$DataRoot\)\).*?cannot be reparse points'
Assert-Pattern 'a pre-existing app path must be empty and cannot be a reparse point before installation' '(?s)if\s*\(\$appPathWasPresent\).*?ReparsePoint.*?Install directory is not empty'
Assert-Pattern 'InstallRoot must not contain files that installer ACL updates would affect' '(?s)\$unrelatedInstallFiles\s*=.*?InstallRoot must be dedicated to EnvanterTR'
Assert-Pattern 'installation mutations are inside a rollback boundary' '(?s)\$appInstallStarted\s*=\s*\$false.*?try\s*\{\s*\$appInstallStarted\s*=\s*\$true\s*New-Item'
Assert-Pattern 'service and application rollback only removes a service created by this install' '(?s)if\s*\(\$serviceCreated\).*?sc\.exe delete \$ServiceName.*?serviceRemovalConfirmed'
Assert-Pattern 'service files stay in place if service deletion cannot be confirmed' '(?s)if\s*\(\$appInstallStarted\s+-and\s+\$serviceRemovalConfirmed.*?elseif\s*\(\$appInstallStarted\s+-and\s+-not\s+\$serviceRemovalConfirmed\).*?application files were retained'
Assert-Pattern 'failure rollback retains the DataRoot and its user data' '(?s)Windows installation failed.*?DataRoot was retained'
Assert-Pattern 'setup tokens are not shown until HTTPS API and PostgreSQL connectivity pass' '(?s)\$status\.product\s+-eq\s+"EnvanterTR"\s+-and\s+\$status\.status\s+-notin\s+@\("database-unavailable","database-not-configured"\).*?if\s*\(-not\s+\$apiReady\).*?One-time company setup token'
Assert-Pattern 'database credentials use LocalMachine DPAPI protection' '(?s)ProtectedData\]::Protect\(\s*\$connectionBytes,\$null,\[Security\.Cryptography\.DataProtectionScope\]::LocalMachine\)'
Assert-Pattern 'production settings store the protected secret path instead of the plaintext connection string' 'WindowsSecrets = @\{ ConnectionStringPath = \$connectionSecretPath \}'
Assert-Pattern 'the Windows service can read only the protected secret directory' '(?s)\$secretsPath /inheritance:r /grant:r "SYSTEM:\(OI\)\(CI\)F" "Administrators:\(OI\)\(CI\)F" "NT AUTHORITY\\LOCAL SERVICE:\(OI\)\(CI\)RX'
if ($source -match '(?s)ConnectionStrings\s*=\s*@\{\s*EnvanterTR\s*=\s*\$connectionString') { throw 'FAIL: database credentials must not be written in appsettings.' }

$apiReadyIndex = $source.IndexOf('if ($status.product -eq "EnvanterTR"')
$companyTokenIndex = $source.IndexOf('One-time company setup token')
$platformTokenIndex = $source.IndexOf('One-time platform setup token')
if ($apiReadyIndex -lt 0 -or $companyTokenIndex -lt $apiReadyIndex -or $platformTokenIndex -lt $apiReadyIndex) {
    throw 'FAIL: bootstrap tokens must be emitted only after the service readiness check.'
}
if ($source -match 'Remove-Item[^\r\n]*\$DataRoot') { throw 'FAIL: failed initial install must not remove DataRoot.' }
Write-Output 'PASS: Windows installer PowerShell syntax and rollback ordering.'
