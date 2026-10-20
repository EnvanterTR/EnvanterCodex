param([string]$Image = 'envantertr-web-v1-candidate:20261020')
$ErrorActionPreference = 'Stop'
$composeFile = Join-Path $PSScriptRoot 'compose.release-smoke.yaml'
$projectName = 'envantertr-release-smoke-' + [Guid]::NewGuid().ToString('N').Substring(0,10)
$previousImage = $env:ENVANTERTR_CANDIDATE_IMAGE
$previousMigrationCount = $env:ENVANTERTR_EXPECTED_MIGRATIONS
$exitCode = 1
try {
    $env:ENVANTERTR_CANDIDATE_IMAGE = $Image
    $migrationDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../src/EnvanterTR.Web/Data/Migrations'))
    $env:ENVANTERTR_EXPECTED_MIGRATIONS = [string]@(Get-ChildItem -LiteralPath $migrationDirectory -File |
        Where-Object Name -Match '^\d{14}_.+\.cs$' | Where-Object Name -NotMatch '\.Designer\.cs$').Count
    & docker compose --project-name $projectName --file $composeFile run --rm checks
    $exitCode = $LASTEXITCODE
} finally {
    & docker compose --project-name $projectName --file $composeFile down --volumes --remove-orphans
    $env:ENVANTERTR_CANDIDATE_IMAGE = $previousImage
    $env:ENVANTERTR_EXPECTED_MIGRATIONS = $previousMigrationCount
}
exit $exitCode
