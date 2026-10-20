$ErrorActionPreference = 'Stop'
$taskFixture = Join-Path ([IO.Path]::GetTempPath()) ('envantertr-inventory-test-' + [Guid]::NewGuid().ToString('N'))
$taskGenerator = Join-Path (Split-Path $PSScriptRoot -Parent) 'Generate-ThirdPartyInventory.ps1'
$taskUtf8 = [Text.UTF8Encoding]::new($false)
$taskBaseline = $null
try {
    foreach ($taskVariant in @('MixedCase.Package.nuspec', 'mixedcase.package.nuspec')) {
        $taskRoot = Join-Path $taskFixture ([Guid]::NewGuid().ToString('N'))
        $taskPackages = Join-Path $taskRoot 'packages'
        $taskPackage = Join-Path $taskPackages 'mixedcase.package/1.0.0'
        [void][IO.Directory]::CreateDirectory($taskPackage)
        [void][IO.Directory]::CreateDirectory((Join-Path $taskRoot 'tools'))
        [IO.File]::WriteAllText((Join-Path $taskRoot 'tools/third-party-vendors.json'), '[]', $taskUtf8)
        [IO.File]::WriteAllText((Join-Path $taskPackage $taskVariant), '<package><metadata><license type="expression">MIT</license></metadata></package>', $taskUtf8)
        $taskAssets = @{
            project = @{ frameworks = @{ net10 = @{ dependencies = @{ 'MixedCase.Package' = @{} } } } }
            libraries = @{ 'MixedCase.Package/1.0.0' = @{ type = 'package' } }
        } | ConvertTo-Json -Depth 10
        foreach ($taskProject in @('src/EnvanterTR.Web', 'tests/EnvanterTR.SecurityAcceptance', 'tools/EnvanterTR.Backup')) {
            $taskObj = Join-Path $taskRoot ($taskProject + '/obj')
            [void][IO.Directory]::CreateDirectory($taskObj)
            [IO.File]::WriteAllText((Join-Path $taskObj 'project.assets.json'), $taskAssets, $taskUtf8)
        }
        $taskOutput = Join-Path $taskRoot 'inventory.csv'
        & $taskGenerator -RepositoryRoot $taskRoot -NuGetPackagesPath $taskPackages -OutputPath $taskOutput
        $taskActual = [IO.File]::ReadAllText($taskOutput)
        if ($taskActual.Contains("`r")) { throw 'Inventory output must use LF on every operating system.' }
        $taskBytes = [IO.File]::ReadAllBytes($taskOutput)
        if ($taskBytes[0] -eq 239) { throw 'Inventory must not contain a UTF-8 byte order mark.' }
        $taskRows = @(Import-Csv -LiteralPath $taskOutput)
        if ($taskRows.Count -ne 3) { throw 'Each NuGet project must remain represented.' }
        foreach ($taskRow in $taskRows) {
            if ($taskRow.Package -ne 'MixedCase.Package' -or $taskRow.Version -ne '1.0.0' -or
                $taskRow.Scope -ne 'direct' -or $taskRow.LicenseExpression -ne 'MIT' -or
                $taskRow.LicenseUrl -ne 'https://licenses.nuget.org/MIT' -or
                $taskRow.MetadataSource -ne 'NuGet package nuspec: mixedcase.package.nuspec') {
                throw 'Canonicalization must preserve package identity, version, scope and license evidence.'
            }
        }
        if ($null -eq $taskBaseline) { $taskBaseline = $taskActual }
        elseif ($taskBaseline -cne $taskActual) { throw 'NuGet cache filename casing changed inventory bytes.' }
    }
    Write-Output 'PASS: NuGet filename casing and host line endings cannot change inventory bytes; package and license evidence preserved.'
} finally {
    $taskResolved = [IO.Path]::GetFullPath($taskFixture)
    $taskParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $taskResolved.StartsWith($taskParent, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($taskResolved) -notmatch '^envantertr-inventory-test-[a-f0-9]{32}$') {
        throw 'Refusing cleanup outside this temporary test fixture.'
    }
    if (Test-Path -LiteralPath $taskResolved) { Remove-Item -LiteralPath $taskResolved -Recurse -Force }
}
