$ErrorActionPreference = 'Stop'
$updatePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'update.ps1'
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($updatePath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) {
    throw "update.ps1 has PowerShell parse errors: $($parseErrors.Message -join '; ')"
}

$source = Get-Content -LiteralPath $updatePath -Raw
$checks = @(
    @{ Name = 'every update requires a running service for a fresh verified backup'; Pattern = '(?s)\$wasRunning = \$service\.Status -eq "Running".*?if \(-not \$wasRunning\).*?must be running before an update' },
    @{ Name = 'encrypted backup and isolated restore preview run before app files are changed'; Pattern = '(?s)\$backupScript = Join-Path \$PSScriptRoot "backup\.ps1".*?\$backupOutput = @\(& \$backupScript @backupArguments\).*?Encrypted backup created and verified:.*?\$snapshotReady = \$true.*?--migrate' },
    @{ Name = 'update requires the backup archive and SHA-256 sidecar to match'; Pattern = '(?s)\$verifiedDigest = \(Get-FileHash.*?\$sidecar = .*?does not match its SHA-256 sidecar' },
    @{ Name = 'migration confirmation describes a fresh verified encrypted backup'; Pattern = '(?s)creates and verifies a fresh encrypted backup.*?Type MIGRATE' },
    @{ Name = 'migration attempt is recorded before executing migration'; Pattern = '(?s)\$migrationAttempted\s*=\s*\$true\s*Push-Location.*?--migrate' },
    @{ Name = 'migration failure explicitly avoids automatic app rollback'; Pattern = '(?s)if\s*\(\$migrationAttempted\).*?updated application files remain in place.*?restore the verified backup' },
    @{ Name = 'pre-migration rollback only occurs after a complete snapshot'; Pattern = '(?s)if\s*\(\$snapshotReady\).*?Copy-Item\s+-Path\s+\(Join-Path\s+\$releasePath\s+"\*"\)' },
    @{ Name = 'a stopped service is started for health validation'; Pattern = '(?s)\n\s*Start-Service\s+-Name\s+\$ServiceName\s*\n\s*\$deadline' },
    @{ Name = 'service originally stopped is stopped again after health succeeds'; Pattern = '(?s)if\s*\(\$status\.status\s+-eq\s+"ready"\).*?if\s*\(-not\s+\$wasRunning\)\s*\{\s*Stop-Service' },
    @{ Name = 'rollback path includes collision-resistant suffix'; Pattern = '\$releasePath\s*=.*?rollback-.*?NewGuid\(\)' }
)

foreach ($check in $checks) {
    if ($source -notmatch $check.Pattern) { throw "FAIL: $($check.Name)" }
    Write-Output "PASS: $($check.Name)"
}
if ($source -match '\$BackupPath|separately verified database backup') { throw 'FAIL: update still accepts an unverified operator-supplied dump path.' }
Write-Output 'PASS: update no longer relies on an unverified dump path.'
if ($source -notmatch '(?s)Global\\EnvanterTR_Maintenance.*?WaitOne\(0\).*?ReleaseMutex\(\)') { throw 'FAIL: update does not hold the shared maintenance lock through cleanup.' }
Write-Output 'PASS: update takes and releases the shared maintenance lock.'

$lockName = "Global\EnvanterTR_UpdateBackup_LockTest_$PID"
$outerLock = [Threading.Mutex]::new($false,$lockName)
if (-not $outerLock.WaitOne(0)) { throw 'FAIL: could not acquire test maintenance mutex.' }
try {
    $nestedLock = [Threading.Mutex]::new($false,$lockName)
    try {
        if (-not $nestedLock.WaitOne(0)) { throw 'FAIL: backup invoked by update cannot recursively share the maintenance mutex.' }
        Write-Output 'PASS: in-process backup invocation can share update maintenance lock without opening a race window.'
        $nestedLock.ReleaseMutex()
    } finally { $nestedLock.Dispose() }
} finally { $outerLock.ReleaseMutex(); $outerLock.Dispose() }
Write-Output 'PASS: update.ps1 parses successfully.'
