[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$DependencyFile,
    [string]$InventoryPath = 'docs/THIRD-PARTY-DEPENDENCIES.csv',
    [Parameter(Mandatory = $true)][string]$Project = 'EnvanterTR.Web'
)

$ErrorActionPreference = 'Stop'
$repository = (Split-Path -Parent $PSScriptRoot)
if (-not [IO.Path]::IsPathRooted($InventoryPath)) { $InventoryPath = Join-Path $repository $InventoryPath }
$dependencyPath = (Resolve-Path $DependencyFile).Path
$inventoryFile = (Resolve-Path $InventoryPath).Path
$dependencyManifest = Get-Content -LiteralPath $dependencyPath -Raw | ConvertFrom-Json
if (-not $dependencyManifest.libraries) { throw "Published dependency manifest has no libraries: $dependencyPath" }

$publishedPackages = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($library in $dependencyManifest.libraries.PSObject.Properties) {
    if ($library.Value.type -ne 'package') { continue }
    $identity = $library.Name -split '/', 2
    if ($identity.Count -ne 2 -or [string]::IsNullOrWhiteSpace($identity[0]) -or [string]::IsNullOrWhiteSpace($identity[1])) {
        throw "Invalid published package identity: $($library.Name)"
    }
    [void]$publishedPackages.Add("$($identity[0])/$($identity[1])")
}
if ($publishedPackages.Count -eq 0) { throw "No published NuGet package libraries found in: $dependencyPath" }

$inventoryPackages = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($row in (Import-Csv -LiteralPath $inventoryFile)) {
    if ($row.Project -ne $Project -or $row.Scope -eq 'vendored') { continue }
    if ([string]::IsNullOrWhiteSpace($row.Package) -or [string]::IsNullOrWhiteSpace($row.Version)) {
        throw "Inventory contains an incomplete package identity for project $Project."
    }
    $identity = "$($row.Package)/$($row.Version)"
    if (-not $inventoryPackages.TryAdd($identity, $row)) { throw "Duplicate inventory package identity for ${Project}: $identity" }
}

$missing = @(
    foreach ($identity in $publishedPackages) {
        if (-not $inventoryPackages.ContainsKey($identity)) { $identity; continue }
        $row = $inventoryPackages[$identity]
        if (-not $row.LicenseExpression -and -not $row.LicenseFile -and -not $row.LicenseUrl) {
            throw "Published package has no recorded license reference: $identity"
        }
    }
)
if ($missing.Count) { throw "Published $Project dependencies are absent from the third-party inventory: $($missing -join ', ')" }

Write-Output "Verified $($publishedPackages.Count) published NuGet packages against the $Project third-party inventory."
