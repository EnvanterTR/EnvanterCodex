$ErrorActionPreference = "Stop"
$composeFile = Join-Path $PSScriptRoot "compose.ui-acceptance.yaml"
$projectName = "envantertr-platform-mfa-browser-$([Guid]::NewGuid().ToString('N').Substring(0,10))"
$exitCode = 1
try {
    & docker compose --project-name $projectName --profile browser --file $composeFile run --build --rm platform-mfa-browser
    $exitCode = $LASTEXITCODE
} finally {
    & docker compose --project-name $projectName --profile browser --file $composeFile down --volumes --remove-orphans
}
exit $exitCode
