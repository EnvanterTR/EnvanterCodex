[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$NuGetPackagesPath = (Join-Path $env:USERPROFILE '.nuget/packages'),
    [string]$OutputPath = 'docs/THIRD-PARTY-DEPENDENCIES.csv'
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path $RepositoryRoot).Path
if (-not [IO.Path]::IsPathRooted($OutputPath)) { $OutputPath = Join-Path $repository $OutputPath }
if (-not [IO.Path]::IsPathRooted($NuGetPackagesPath)) { $NuGetPackagesPath = Join-Path $repository $NuGetPackagesPath }

$projects = @(
    @{ Name = 'EnvanterTR.Web'; Path = 'src/EnvanterTR.Web/obj/project.assets.json' },
    @{ Name = 'EnvanterTR.SecurityAcceptance'; Path = 'tests/EnvanterTR.SecurityAcceptance/obj/project.assets.json' },
    @{ Name = 'EnvanterTR.Backup'; Path = 'tools/EnvanterTR.Backup/obj/project.assets.json' }
)
$rows = [Collections.Generic.List[object]]::new()

foreach ($project in $projects) {
    $assetsPath = Join-Path $repository $project.Path
    if (-not (Test-Path -LiteralPath $assetsPath -PathType Leaf)) { throw "NuGet restore assets missing: $($project.Path)" }
    $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
    $direct = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($framework in $assets.project.frameworks.PSObject.Properties) {
        foreach ($dependency in $framework.Value.dependencies.PSObject.Properties) { [void]$direct.Add($dependency.Name) }
    }

    foreach ($library in $assets.libraries.PSObject.Properties) {
        if ($library.Value.type -ne 'package') { continue }
        $parts = $library.Name -split '/', 2
        if ($parts.Count -ne 2) { throw "Unexpected package identity in $($project.Name): $($library.Name)" }
        $packageId, $version = $parts
        $packageRoot = Join-Path (Join-Path $NuGetPackagesPath $packageId.ToLowerInvariant()) $version.ToLowerInvariant()
        $nuspec = Get-ChildItem -LiteralPath $packageRoot -Filter '*.nuspec' -File | Select-Object -First 1
        if (-not $nuspec) { throw "NuGet nuspec not found: $($project.Name) $($library.Name)" }
        [xml]$metadata = Get-Content -LiteralPath $nuspec.FullName -Raw
        $license = $metadata.SelectSingleNode("//*[local-name()='license']")
        $legacyLicense = $metadata.SelectSingleNode("//*[local-name()='licenseUrl']")
        $licenseExpression = ''
        $licenseType = ''
        $licenseFile = ''
        $licenseUrl = ''
        if ($license) {
            $licenseType = $license.GetAttribute('type')
            if ($licenseType -eq 'expression') { $licenseExpression = $license.InnerText.Trim() }
            elseif ($licenseType -eq 'file') {
                $licenseFile = $license.InnerText.Trim()
                if (-not (Test-Path -LiteralPath (Join-Path $packageRoot $licenseFile) -PathType Leaf)) {
                    throw "Declared license file missing for $($library.Name): $licenseFile"
                }
            } else { $licenseExpression = $license.InnerText.Trim() }
        } elseif ($legacyLicense) {
            $licenseType = 'url'
            $licenseUrl = $legacyLicense.InnerText.Trim()
        }
        if (-not $licenseUrl -and $licenseExpression) {
            $licenseUrl = 'https://licenses.nuget.org/' + [uri]::EscapeDataString($licenseExpression)
        }
        $rows.Add([pscustomobject]@{
            Project = $project.Name
            Package = $packageId
            Version = $version
            Scope = if ($direct.Contains($packageId)) { 'direct' } else { 'transitive' }
            LicenseExpression = $licenseExpression
            LicenseType = $licenseType
            LicenseUrl = $licenseUrl
            LicenseFile = $licenseFile
            MetadataSource = "NuGet package nuspec: $($nuspec.Name)"
        })
    }
}

$vendorManifestPath = Join-Path $repository 'tools/third-party-vendors.json'
foreach ($vendor in (Get-Content -LiteralPath $vendorManifestPath -Raw | ConvertFrom-Json)) {
    $vendorPath = Join-Path $repository $vendor.LicenseFile.Split(' ')[0]
    if (-not (Test-Path -LiteralPath $vendorPath -PathType Leaf)) { throw "Vendored license/source file missing: $($vendor.LicenseFile)" }
    $rows.Add([pscustomobject]@{
        Project = 'EnvanterTR.Web'
        Package = $vendor.Package
        Version = $vendor.Version
        Scope = 'vendored'
        LicenseExpression = $vendor.LicenseExpression
        LicenseType = 'notice'
        LicenseUrl = $vendor.Source
        LicenseFile = $vendor.LicenseFile
        MetadataSource = 'tools/third-party-vendors.json and retained vendor notice'
    })
}

$rows = @($rows | Sort-Object -Property Project, Package, Version -Culture 'en-US')
$missing = @($rows | Where-Object { -not $_.LicenseExpression -and -not $_.LicenseFile -and -not $_.LicenseUrl })
if ($missing.Count) { throw "License metadata is missing for $($missing.Count) dependencies." }
$rows | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding utf8
Write-Output "Wrote $($rows.Count) dependency records to $OutputPath ($($projects.Count) NuGet projects plus vendored web assets)."
