param([Parameter(Mandatory)][ValidateRange(0,3)][int]$ShardIndex)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_RUN_ID -notmatch '^\d+$' -or $env:GITHUB_RUN_ATTEMPT -notmatch '^\d+$' -or
    -not (Test-Path -LiteralPath $env:RUNNER_TEMP -PathType Container)) { throw 'An isolated GitHub runner context is required.' }
$project = "envantertr-ci-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT-$ShardIndex"
$compose = Join-Path $PSScriptRoot 'compose.security-acceptance.yaml'
function Compose([string[]]$Arguments) {
    & docker compose --project-name $project --file $compose @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Compose failed ($LASTEXITCODE)." }
}
try {
    Compose @('build','acceptance')
    $discovery = @(& docker compose --project-name $project --file $compose run --rm --entrypoint dotnet acceptance test --no-build --no-restore --list-tests)
    if ($LASTEXITCODE -ne 0) { throw 'Test discovery failed.' }
    $path = Join-Path $env:RUNNER_TEMP "security-discovery-$ShardIndex.txt"
    $discovery | Set-Content -LiteralPath $path
    $plan = & (Join-Path $PSScriptRoot 'Get-SecurityTestShard.ps1') -DiscoveryPath $path -ShardIndex $ShardIndex
    $plan | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $env:RUNNER_TEMP "security-shard-$ShardIndex.json")
    Write-Output "Shard $ShardIndex executes $($plan.SelectedCases)/$($plan.TotalCases) discovered cases."
    Compose @('run','--rm','--entrypoint','dotnet','acceptance','test','--no-build','--no-restore','--filter',$plan.Filter,'--logger','console;verbosity=normal')
} finally {
    & docker compose --project-name $project --file $compose down --volumes --remove-orphans
    if ($LASTEXITCODE -ne 0) { throw 'Disposable shard cleanup failed.' }
}
