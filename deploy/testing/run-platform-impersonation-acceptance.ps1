$ErrorActionPreference = 'Stop'
$compose = Join-Path $PSScriptRoot 'compose.security-acceptance.yaml'
$project = 'envantertr-impersonation-' + [Guid]::NewGuid().ToString('N').Substring(0,10)
$result = 1
try {
    & docker compose -p $project -f $compose run --build --rm acceptance --filter 'FullyQualifiedName~Impersonation_|FullyQualifiedName~Every_api_endpoint_declares_an_explicit_authentication_boundary'
    $result = $LASTEXITCODE
} finally {
    & docker compose -p $project -f $compose down --volumes --remove-orphans
}
exit $result
