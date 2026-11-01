$ErrorActionPreference = 'Stop'
$compose = Join-Path $PSScriptRoot 'compose.security-acceptance.yaml'
$project = 'envantertr-release-migration-' + [Guid]::NewGuid().ToString('N').Substring(0,10)
$result = 1
try {
    & docker compose -p $project -f $compose run --build --rm acceptance --filter 'FullyQualifiedName~Current_release_preserves_all_original_fields'
    $result = $LASTEXITCODE
} finally {
    & docker compose -p $project -f $compose down --volumes --remove-orphans
}
exit $result
