$ErrorActionPreference = 'Stop'
$installPath = Join-Path $PSScriptRoot '..\install.ps1'
$setupPath = Join-Path $PSScriptRoot '..\setup.ps1'
$preflightPath = Join-Path $PSScriptRoot '..\preflight.ps1'
$localhostCertificatePath = Join-Path $PSScriptRoot '..\ensure-localhost-certificate.ps1'
$postgresPath = Join-Path $PSScriptRoot '..\prepare-postgresql.ps1'
$postgresInstallerPath = Join-Path $PSScriptRoot '..\install-postgresql.ps1'
$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $installPath),[ref]$tokens,[ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "install.ps1 has PowerShell parse errors: $($parseErrors.Message -join '; ')" }
$source = Get-Content -LiteralPath $installPath -Raw
$setupSource = Get-Content -LiteralPath $setupPath -Raw
$preflightSource = Get-Content -LiteralPath $preflightPath -Raw
$localhostCertificateSource = Get-Content -LiteralPath $localhostCertificatePath -Raw
$postgresSource = Get-Content -LiteralPath $postgresPath -Raw
$postgresInstallerSource = Get-Content -LiteralPath $postgresInstallerPath -Raw

$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $postgresInstallerPath),[ref]$tokens,[ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "install-postgresql.ps1 has PowerShell parse errors: $($parseErrors.Message -join '; ')" }

function Assert-Pattern([string]$Name,[string]$Pattern) {
    if ($source -notmatch $Pattern) { throw "FAIL: $Name" }
    Write-Output "PASS: $Name"
}

Assert-Pattern 'personal installs bind only to localhost while team server installs keep the network binding' '(?s)\[switch\]\$LoopbackOnly.*?\$listenHost\s*=\s*if\s*\(\$LoopbackOnly\)\s*\{\s*"localhost"\s*\}\s*else\s*\{\s*"0\.0\.0\.0"\s*\}.*?Url\s*=\s*"https://\$\{listenHost\}:\$HttpsPort"'

Assert-Pattern 'nested install and data roots are rejected' '(?s)InstallRoot and DataRoot must be separate, non-nested directories'
Assert-Pattern 'install and data roots cannot be reparse points' '(?s)foreach \(\$rootPath in @\(\$InstallRoot,\$DataRoot\)\).*?cannot be reparse points'
Assert-Pattern 'all existing ancestors of install and protected data paths reject reparse points before mutation' '(?s)function Assert-NoReparsePointInPath.*?GetPathRoot.*?FileAttributes\]::ReparsePoint.*?foreach \(\$guardedPath in @\(\$InstallRoot,\$DataRoot,\$appPath,\$mediaPath,\$keysPath,\$secretsPath\)\).*?Assert-NoReparsePointInPath -Path \$guardedPath.*?\$appInstallStarted\s*=\s*\$false'
Assert-Pattern 'newly created install, media, key, and secret directories are checked before package files or secrets are written' '(?s)New-Item -ItemType Directory -Path \$appPath,\$mediaPath,\$keysPath,\$secretsPath -Force.*?foreach \(\$guardedPath in @\(\$InstallRoot,\$DataRoot,\$appPath,\$mediaPath,\$keysPath,\$secretsPath\)\).*?Assert-NoReparsePointInPath -Path \$guardedPath.*?Copy-Item'
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
Assert-Pattern 'the temporary setup-resume secret remains administrator-only after data-root ACL setup' '(?s)foreach \(\$pendingSetupFile in @\("setup-pending\.dpapi","setup-pending\.json"\)\).*?\$pendingSetupPath /inheritance:r /grant:r "SYSTEM:F" "Administrators:F".*?Could not restrict pending first-install file permissions'
if ($source -match '(?s)ConnectionStrings\s*=\s*@\{\s*EnvanterTR\s*=\s*\$connectionString') { throw 'FAIL: database credentials must not be written in appsettings.' }

if ($setupSource -notmatch '(?s)Read-PendingConnection.*?DataProtectionScope\]::LocalMachine') { throw 'FAIL: interrupted Windows setup must resume from a machine-protected connection secret.' }
if ($setupSource -notmatch '(?s)if \(\$existingService -and \$pendingSecretExists\).*?migrate\.ps1.*?Remove-PendingSetup') { throw 'FAIL: a service installed before migration failure must resume migration without reinstalling.' }
if ($setupSource -notmatch '(?s)PostgreSqlPort.*?CertificateThumbprint.*?Yarım kurulum farklı DNS, port veya TLS sertifikasıyla sürdürülemez') { throw 'FAIL: resumed setup must reject changed database, HTTPS, DNS, or certificate settings.' }
if ($setupSource -notmatch '(?s)param\(\[switch\]\$PersonalSinglePc\).*?Windows 11 x64 iş istasyonunda.*?DnsName = ''localhost''.*?Read-Host "Devam etmek için INSTALL yazın".*?ensure-localhost-certificate\.ps1.*?LoopbackOnly') { throw 'FAIL: personal setup must be Windows 11-only, require explicit install confirmation before certificate trust changes, and bind to loopback.' }
if ($setupSource -notmatch '(?s)\$isX64\s*=\s*\[string\]\$os\.OSArchitecture -match ''\^64\[- \]\?bit\$''.*?if \(\$PersonalSinglePc\).*?ProductType -ne 1.*?PersonalSinglePc\s*=\s*\[bool\]\$PersonalSinglePc') { throw 'FAIL: personal setup must normalize Windows x64 reporting, reject server OS builds, and persist its mode for safe resume.' }
if ($preflightSource -notmatch '(?s)\[switch\]\$PersonalSinglePc.*?\$isX64\s*=\s*\[string\]\$os\.OSArchitecture -match ''\^64\[- \]\?bit\$''.*?Windows 11.*?ProductType -eq 1.*?Local TLS trust.*?Network exposure') { throw 'FAIL: personal preflight must normalize x64 reporting and validate Windows 11, local TLS trust, and localhost-only exposure.' }
if ($localhostCertificateSource -notmatch '(?s)New-SelfSignedCertificate -DnsName ''localhost''.*?Cert:\\LocalMachine\\My.*?NonExportable.*?NT AUTHORITY\\LOCAL SERVICE:R.*?rootStore\.Add') { throw 'FAIL: personal certificate setup must create a non-exportable localhost certificate, grant LocalService read access, and trust its public certificate locally.' }
foreach ($scriptPath in @($setupPath,$preflightPath,$localhostCertificatePath)) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $scriptPath),[ref]$tokens,[ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) { throw "FAIL: $([IO.Path]::GetFileName($scriptPath)) has PowerShell parse errors: $($parseErrors.Message -join '; ')" }
}
if ($postgresSource -notmatch '(?s)PendingConnectionPath.*?ProtectedData\]::Protect.*?LocalMachine.*?DROP DATABASE.*?DROP ROLE') { throw 'FAIL: PostgreSQL setup must persist the DPAPI secret before returning and roll back a newly created database/role on failure.' }
if ($setupSource -notmatch '(?s)AllowMissingPostgreSql.*?INSTALL POSTGRESQL.*?install-postgresql\.ps1.*?prepare-postgresql\.ps1') { throw 'FAIL: guided server setup must separately confirm and run PostgreSQL installation before creating the EnvanterTR database.' }
if ($postgresInstallerSource -notmatch 'https://get\.enterprisedb\.com/postgresql/postgresql-17\.11-1-windows-x64\.exe') { throw 'FAIL: PostgreSQL installer must use the pinned official EDB HTTPS release URL.' }
if ($postgresInstallerSource -notmatch 'SignatureStatus\]::Valid' -or $postgresInstallerSource -notmatch "\`$expectedPublisher\s*=\s*'EnterpriseDB Corporation'") { throw 'FAIL: PostgreSQL installer must have a valid Authenticode signature from EDB.' }
if ($postgresInstallerSource -notmatch '(?s)--mode qt.*?Start-Process.*?-Wait') { throw 'FAIL: PostgreSQL installation must stay interactive so its administrator password is entered in the signed installer UI.' }
if ($postgresInstallerSource -match '(?i)--superpassword|--servicepassword|PGPASSWORD') { throw 'FAIL: PostgreSQL administrator passwords must not be passed on a process command line or stored in environment variables.' }
if ($postgresInstallerSource -notmatch '(?s)A PostgreSQL 17 installation or service already exists.*?will not upgrade, repair, or replace') { throw 'FAIL: PostgreSQL helper must not replace an existing installation.' }
Write-Output 'PASS: Windows initial setup can resume without losing its protected database secret.'
Write-Output 'PASS: PostgreSQL prerequisite uses a pinned, Authenticode-verified EDB installer without passing passwords to the process command line.'

$apiReadyIndex = $source.IndexOf('if ($status.product -eq "EnvanterTR"')
$companyTokenIndex = $source.IndexOf('One-time company setup token')
$platformTokenIndex = $source.IndexOf('One-time platform setup token')
if ($apiReadyIndex -lt 0 -or $companyTokenIndex -lt $apiReadyIndex -or $platformTokenIndex -lt $apiReadyIndex) {
    throw 'FAIL: bootstrap tokens must be emitted only after the service readiness check.'
}
if ($source -match 'Remove-Item[^\r\n]*\$DataRoot') { throw 'FAIL: failed initial install must not remove DataRoot.' }
Write-Output 'PASS: Windows installer PowerShell syntax and rollback ordering.'
