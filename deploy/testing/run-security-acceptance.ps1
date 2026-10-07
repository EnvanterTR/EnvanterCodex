$ErrorActionPreference = "Stop"
$composeFile = Join-Path $PSScriptRoot "compose.security-acceptance.yaml"
$projectName = "envantertr-security-acceptance-$([Guid]::NewGuid().ToString('N').Substring(0,10))"
$exitCode = 1
try {
    & docker compose --project-name $projectName --file $composeFile up --build --abort-on-container-exit --exit-code-from acceptance
    $exitCode = $LASTEXITCODE
} finally {
    & docker compose --project-name $projectName --file $composeFile down --volumes --remove-orphans
}
exit $exitCode
