$ErrorActionPreference = 'Stop'
$compose = Join-Path $PSScriptRoot 'compose.security-acceptance.yaml'
$project = 'envantertr-checkout-email-' + [Guid]::NewGuid().ToString('N').Substring(0,10)
$filter = 'FullyQualifiedName~Direct_checkout|FullyQualifiedName~Checkout_email_|FullyQualifiedName~Public_checkout|FullyQualifiedName~Existing_company_checkout_form_posts|FullyQualifiedName~Existing_company_checkout_page_is|FullyQualifiedName~Platform_checkout_readiness|FullyQualifiedName~Every_api_endpoint_declares_an_explicit_authentication_boundary'
$result = 1
try {
    & docker compose -p $project -f $compose run --build --rm acceptance --filter $filter
    $result = $LASTEXITCODE
} finally {
    & docker compose -p $project -f $compose down --volumes --remove-orphans
}
exit $result
