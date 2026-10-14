$ErrorActionPreference = "Stop"
$image = "envantertr-hosted-checkout-browser-acceptance:local"
& docker build --tag $image --file deploy/testing/Dockerfile.hosted-checkout-browser .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& docker run --rm --init $image
exit $LASTEXITCODE
