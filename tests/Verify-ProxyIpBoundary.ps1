$ErrorActionPreference = 'Stop'
$fixture = Join-Path $PSScriptRoot 'proxy-ip/compose.yaml'
$project = 'envantertr-ip-boundary-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
try {
    docker compose -p $project -f $fixture up -d --wait --wait-timeout 60
    if ($LASTEXITCODE -ne 0) { throw 'Proxy fixture startup failed.' }
    $binding = docker compose -p $project -f $fixture port edge 8080
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:\d+$') { throw 'Fixture must bind loopback only.' }
    $probe = Invoke-RestMethod "http://$binding/" -Headers @{
        'X-Forwarded-For' = '198.51.100.77, 173.245.48.12'
        'CF-Connecting-IP' = '198.51.100.99'
    } -TimeoutSec 15
    $hops = $probe.forwarded.Split(',').Trim()
    if ($hops.Count -ne 2) { throw "Expected exactly two observed hops; got $($hops.Count)." }
    if ($hops -contains '198.51.100.77' -or $hops -contains '173.245.48.12') { throw 'Forged forwarded prefix reached application boundary.' }
    if ($probe.cloudflare -ne '198.51.100.99') { throw 'Probe did not transmit the forged Cloudflare header.' }
    [pscustomobject]@{ result = 'PASS'; forwardedHopCount = $hops.Count; forgedForwardedPrefixRemoved = $true; cloudflareHeaderStillRequiresResolverValidation = $true } | ConvertTo-Json
}
finally {
    docker compose -p $project -f $fixture down --remove-orphans
    if ($LASTEXITCODE -ne 0) { Write-Warning "Fixture cleanup failed: $project" }
}
