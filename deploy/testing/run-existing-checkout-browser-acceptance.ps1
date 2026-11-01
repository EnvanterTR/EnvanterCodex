param([string]$EvidencePath = 'tests/reports/existing-company-browser-20261024')
$ErrorActionPreference = 'Stop'
$project = 'envantertr-checkout-browser-' + [Guid]::NewGuid().ToString('N').Substring(0,10)
$previousEvidence = $env:CHECKOUT_BROWSER_EVIDENCE
$result = 1
try {
    New-Item -ItemType Directory -Force -Path $EvidencePath | Out-Null
    $env:CHECKOUT_BROWSER_EVIDENCE = (Resolve-Path -LiteralPath $EvidencePath).Path
    & docker compose -p $project -f deploy/testing/compose.security-acceptance.yaml -f deploy/testing/compose.existing-checkout-browser.yaml run --build --rm checkout-browser
    $result = $LASTEXITCODE
} finally {
    & docker compose -p $project -f deploy/testing/compose.security-acceptance.yaml -f deploy/testing/compose.existing-checkout-browser.yaml down --volumes --remove-orphans
    $env:CHECKOUT_BROWSER_EVIDENCE = $previousEvidence
}
exit $result
