param([Parameter(Mandatory)][string]$DiscoveryPath)
$ErrorActionPreference = 'Stop'
$DiscoveryPath = (Resolve-Path -LiteralPath $DiscoveryPath).Path
$saved = @{}
foreach ($key in @('GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','RUNNER_TEMP')) { $saved[$key] = [Environment]::GetEnvironmentVariable($key) }
$priorDocker = Get-Item Function:\global:docker -ErrorAction SilentlyContinue
$savedExitCode = $global:LASTEXITCODE
$global:ShardDockerCalls = [Collections.Generic.List[string[]]]::new()
$global:ShardDiscoveryFixture = $DiscoveryPath
$global:ShardSimulateFailure = $false
try {
    $env:GITHUB_RUN_ID = '999999999'; $env:GITHUB_RUN_ATTEMPT = '1'
    $env:RUNNER_TEMP = Join-Path ([IO.Path]::GetTempPath()) ('envtr-shard-test-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($env:RUNNER_TEMP)
    function global:docker {
        param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Items)
        $global:ShardDockerCalls.Add($Items); $global:LASTEXITCODE = 0
        if ($Items -contains '--list-tests') { Get-Content -LiteralPath $global:ShardDiscoveryFixture }
        if ($global:ShardSimulateFailure -and $Items -contains '--filter') { $global:LASTEXITCODE = 17 }
    }
    & (Join-Path $PSScriptRoot 'run-security-shard.ps1') -ShardIndex 0
    if ($global:ShardDockerCalls.Count -ne 4 -or $global:ShardDockerCalls[2] -notcontains '--filter' -or
        $global:ShardDockerCalls[2] -notcontains '--no-build') { throw 'Unexpected execution/filter sequence.' }
    if (($global:ShardDockerCalls[-1] -join ' ') -notlike '*down --volumes --remove-orphans') { throw 'Cleanup absent.' }
    Write-Output 'PASS mocked runner build/discovery/execution/filter/cleanup'
    $global:ShardDockerCalls.Clear(); $global:ShardSimulateFailure = $true
    try { & (Join-Path $PSScriptRoot 'run-security-shard.ps1') -ShardIndex 0; throw 'Failed tests were accepted.' }
    catch { if ($_.Exception.Message -notlike '*Compose failed (17)*') { throw } }
    if (($global:ShardDockerCalls[-1] -join ' ') -notlike '*down --volumes --remove-orphans') { throw 'Failure cleanup absent.' }
    Write-Output 'PASS mocked test failure propagates and cleanup runs'
} finally {
    if ($priorDocker) { Set-Item Function:\global:docker $priorDocker.ScriptBlock }
    else { Remove-Item Function:\global:docker -ErrorAction SilentlyContinue }
    foreach ($key in $saved.Keys) { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
    $global:LASTEXITCODE = $savedExitCode
}
