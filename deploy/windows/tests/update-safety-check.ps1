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
Write-Output 'PASS: update.ps1 parses successfully.'
