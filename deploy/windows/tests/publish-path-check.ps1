$ErrorActionPreference = 'Stop'
$publishPath = Join-Path $PSScriptRoot '..\publish.ps1'
$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $publishPath),[ref]$tokens,[ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "publish.ps1 has PowerShell parse errors: $($parseErrors.Message -join '; ')" }
$source = Get-Content -LiteralPath $publishPath -Raw
foreach ($pattern in @(
    '\$dockerRepoRoot\s*=\s*\$repoRoot\.Replace\(''\\'',\s*''/''\)',
    '\$dockerOutput\s*=\s*\$output\.Replace\(''\\'',\s*''/''\)',
    'source=\$dockerRepoRoot,target=/source',
    'source=\$dockerOutput,target=/package'
)) {
    if ($source -notmatch $pattern) { throw "Windows Docker publisher path check failed: $pattern" }
}
Write-Output 'PASS: Docker SDK mounts use slash-normalized Windows host paths.'
