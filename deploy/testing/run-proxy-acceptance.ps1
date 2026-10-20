$ErrorActionPreference = 'Stop'
$composeFile = Join-Path $PSScriptRoot 'compose.proxy-acceptance.yaml'
$projectName = 'envantertr-proxy-acceptance-' + [Guid]::NewGuid().ToString('N').Substring(0,10)
$testExitCode = 1
try {
    & docker compose --project-name $projectName --file $composeFile run --rm checks
    $testExitCode = $LASTEXITCODE
} finally {
    & docker compose --project-name $projectName --file $composeFile down --remove-orphans
}
exit $testExitCode
