param([string]$Filter)

$ErrorActionPreference = "Stop"
$composeFile = Join-Path $PSScriptRoot "compose.security-acceptance.yaml"
$projectName = "envantertr-security-acceptance-$([Guid]::NewGuid().ToString('N').Substring(0,10))"
$exitCode = 1
try {
    if ([string]::IsNullOrWhiteSpace($Filter)) {
        & docker compose --project-name $projectName --file $composeFile up --build --abort-on-container-exit --exit-code-from acceptance
    } else {
        & docker compose --project-name $projectName --file $composeFile run --build --rm --entrypoint dotnet acceptance test --no-build --no-restore --filter $Filter --logger "console;verbosity=normal"
    }
    $exitCode = $LASTEXITCODE
} finally {
    & docker compose --project-name $projectName --file $composeFile down --volumes --remove-orphans
}
exit $exitCode
