$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\PrivateAcl.psm1'
$tokens = $null
$parseErrors = $null
[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $modulePath), [ref]$tokens, [ref]$parseErrors) | Out-Null
if ($parseErrors.Count -gt 0) { throw "PowerShell parse failed: $($parseErrors[0].Message)" }
Import-Module (Resolve-Path $modulePath).Path -Force

$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempRoot ('envantertr-private-acl-' + [guid]::NewGuid().ToString('N'))
$ownerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$expectedSids = @($ownerSid, 'S-1-5-18', 'S-1-5-32-544' | Sort-Object -Unique)

function Assert-PrivateAcl([string] $Path) {
    $acl = Get-Acl -LiteralPath $Path
    if (-not $acl.AreAccessRulesProtected) { throw "ACL inheritance remains enabled: $Path" }
    $rules = $acl.GetAccessRules($true, $false, [System.Security.Principal.SecurityIdentifier])
    $actualSids = @($rules | ForEach-Object { $_.IdentityReference.Value } | Sort-Object -Unique)
    if (@(Compare-Object $expectedSids $actualSids).Count -ne 0) { throw "Unexpected principals in private ACL: $Path" }
    foreach ($sid in $expectedSids) {
        $sidRules = @($rules | Where-Object { $_.IdentityReference.Value -eq $sid })
        if ($sidRules.Count -eq 0 -or @($sidRules | Where-Object {
            $_.AccessControlType -ne 'Allow' -or
            -not $_.FileSystemRights.HasFlag([System.Security.AccessControl.FileSystemRights]::FullControl)
        }).Count -gt 0) { throw "Expected full-control allow rule is missing for $sid on $Path" }
    }
}

try {
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
    Set-EnvanterTRPrivateAcl -Path $fixtureRoot -Kind Directory
    Assert-PrivateAcl $fixtureRoot

    $archiveFixture = Join-Path $fixtureRoot 'archive.etbackup'
    [System.IO.File]::WriteAllText($archiveFixture, 'non-secret ACL fixture')
    Set-EnvanterTRPrivateAcl -Path $archiveFixture -Kind File
    Assert-PrivateAcl $archiveFixture

    Write-Output 'PASS: local backup directory and encrypted archive DACLs are restricted and verified.'
} finally {
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixtureRoot)
    $resolvedParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetDirectoryName($resolvedFixture))
    if (-not $resolvedParent.Equals($tempRoot.TrimEnd('\'), [System.StringComparison]::OrdinalIgnoreCase) -or
        [System.IO.Path]::GetFileName($resolvedFixture) -notmatch '^envantertr-private-acl-[a-f0-9]{32}$') {
        throw 'Refusing to remove an ACL fixture outside its generated temporary directory.'
    }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
