param(
    [string]$ComposeProject = "envantertr-product-dev"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$composeFile = (Resolve-Path (Join-Path $repoRoot "deploy\local\compose.yaml")).Path
$mediaRoot = "/var/lib/envantertr/company-media"

function Invoke-DockerChecked {
    param([Parameter(Mandatory)][string[]]$DockerArguments)
    $output = & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker komutu başarısız oldu: docker $($DockerArguments -join ' ')"
    }
    return $output
}

$query = @'
SELECT c."Id"::text || '|' || a."Id"::text || '|' || x."Id"::text
FROM "Companies" c
JOIN "Assets" a ON a."CompanyId"=c."Id" AND a."AssetTag"='SNT-SERVER-013'
JOIN "Accessories" x ON x."CompanyId"=c."Id" AND x."ItemNumber"='SNT-ACC-004'
WHERE c."Name"='Santa';
'@
$databaseContainer = (Invoke-DockerChecked @('compose', '--project-name', $ComposeProject, '--file', $composeFile, 'ps', '-q', 'database') | Select-Object -First 1).Trim()
$webContainer = (Invoke-DockerChecked @('compose', '--project-name', $ComposeProject, '--file', $composeFile, 'ps', '-q', 'web') | Select-Object -First 1).Trim()
if (-not $databaseContainer -or -not $webContainer) { throw "'$ComposeProject' Compose projesinin database ve web servisleri çalışır durumda olmalıdır." }
$encodedQuery = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($query))
$queryCommand = "printf '%s' '$encodedQuery' | base64 -d | psql -U envantertr -d envantertr -At"
$record = Invoke-DockerChecked @('exec', $databaseContainer, 'sh', '-c', $queryCommand)
$parts = (($record | Select-Object -First 1).Trim() -split '\|')
if ($parts.Count -ne 3 -or $parts.Where({ $_ -notmatch '^[0-9a-fA-F-]{36}$' }).Count -gt 0) {
    throw "Santa şirketi, SNT-SERVER-013 sunucusu veya SNT-ACC-004 aksesuarı veritabanında bulunamadı. Önce tools/seed-santa-demo.sql dosyasını uygulayın."
}

$companyPath = $parts[0].Replace('-', '').ToLowerInvariant()
$assetPath = $parts[1].Replace('-', '').ToLowerInvariant()
$accessoryPath = $parts[2].Replace('-', '').ToLowerInvariant()
$serverSource = Join-Path $PSScriptRoot "demo-media\santa-server.jpg"
$accessorySource = Join-Path $PSScriptRoot "demo-media\santa-accessory-bag.jpg"
foreach ($file in @($serverSource, $accessorySource)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Demo görseli bulunamadı: $file" }
}

$serverDirectory = "$mediaRoot/$companyPath/assets/$assetPath"
$accessoryDirectory = "$mediaRoot/$companyPath/accessories/$accessoryPath/photo"
$serverTarget = "$serverDirectory/9f31a1c1e39e4f36ab1234cd56ef7890.jpg"
$accessoryTarget = "$accessoryDirectory/4c6d0102a36d452ea183013191f04f72.jpg"
Invoke-DockerChecked @('exec', '--user', '0', $webContainer, 'mkdir', '-p', $serverDirectory, $accessoryDirectory) | Out-Null
Invoke-DockerChecked @('cp', $serverSource, "${webContainer}:$serverTarget") | Out-Null
Invoke-DockerChecked @('cp', $accessorySource, "${webContainer}:$accessoryTarget") | Out-Null
Invoke-DockerChecked @('exec', '--user', '0', $webContainer, 'chown', 'app:app', $serverTarget, $accessoryTarget) | Out-Null

foreach ($pair in @(
    @{ Source = $serverSource; Target = $serverTarget },
    @{ Source = $accessorySource; Target = $accessoryTarget }
)) {
    $expected = (Get-FileHash -LiteralPath $pair.Source -Algorithm SHA256).Hash.ToLowerInvariant()
    $actualLine = Invoke-DockerChecked @('exec', $webContainer, 'sha256sum', $pair.Target)
    $actual = ($actualLine -split '\s+')[0].ToLowerInvariant()
    if ($actual -ne $expected) { throw "Santa demo görseli doğrulanamadı: $($pair.Target)" }
}

Write-Output "Santa sunucu ve aksesuar görselleri doğrulandı; şirket medya volume'unda erişilebilir."
