$ErrorActionPreference = "Stop"
$restorePath = Join-Path $PSScriptRoot "..\restore.ps1"
$publishPath = Join-Path $PSScriptRoot "..\publish.ps1"
$tokens = $null
$errors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $restorePath),[ref]$tokens,[ref]$errors) | Out-Null
if ($errors.Count -gt 0) { throw "restore.ps1 has PowerShell parse errors: $($errors.Message -join '; ')" }
$source = Get-Content -LiteralPath $restorePath -Raw

function Assert-Contains([string]$Name,[string]$Pattern) {
    if ($source -notmatch $Pattern) { throw "FAIL: $Name" }
    Write-Output "PASS: $Name"
}

Assert-Contains "HMAC is verified before any restore flow" '(?s)Verify-And-Decrypt\s+\$backup\s+\$plainZip.*?manifest\.json'
Assert-Contains "restored manifest files are checked against SHA-256 and size" '(?s)\$actual\.Length\s+-ne\s+\[long\]\$file\.length.*?\$hash\s+-ne\s+\[string\]\$file\.sha256'
Assert-Contains "database is restored into a separate staging name" '(?s)\$createdbSql\s*=.*?\$stageDatabase.*?Invoke-PgTool\s+\$pgRestore.*?--dbname=\$stageDatabase'
Assert-Contains "staged restore requires migration history before live switch" '(?s)public\."__EFMigrationsHistory".*?parsedMigrationCount\s+-lt\s+1'
Assert-Contains "pre-restore verified backup happens before live service is stopped" '(?s)&\s+\$backupScript.*?Stop-Service\s+-Name\s+\$ServiceName'
Assert-Contains "old database is retained before stage database becomes active" '(?s)RENAME\s+TO\s+\$\(Quote-SqlIdentifier\s+\$oldDatabase\).*?RENAME\s+TO\s+\$quotedDatabase'
Assert-Contains "health failure rolls back database, media and key paths" '(?s)if\s*\(\$databaseSwitched\s+-or\s+\$databaseOldRenamed\).*?if\s*\(\$keysSwitched\s+-or\s+\$keysOldMoved\).*?if\s*\(\$mediaSwitched\s+-or\s+\$mediaOldMoved\)'
Assert-Contains "pre-mutation errors leave the service running and remove staged artifacts" '(?s)if\s*\(\$serviceStopped\s+-and.*?if\s*\(-not\s+\$liveMutationStarted\).*?DROP DATABASE'
Assert-Contains "restore takes the shared maintenance lock and releases it in finally" '(?s)Global\\EnvanterTR_Maintenance.*?WaitOne\(0\).*?ReleaseMutex\(\)'
Assert-Contains "staging database preserves the live encoding and locale provider" '(?s)datlocprovider.*?datcollate.*?daticulocale.*?LOCALE_PROVIDER'
Write-Output "PASS: restore PowerShell syntax and safety ordering."
$publish = Get-Content -LiteralPath $publishPath -Raw
if ($publish -notmatch 'Copy-Item.*BackupFormat\.psm1' -or $publish -notmatch 'Copy-Item.*restore\.ps1') {
    throw "FAIL: Windows publish package must include the shared backup format module and restore script."
}
Write-Output "PASS: Windows package includes restore.ps1 and BackupFormat.psm1."
