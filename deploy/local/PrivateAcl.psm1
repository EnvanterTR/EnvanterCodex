function Set-EnvanterTRPrivateAcl {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [ValidateSet('File', 'Directory')]
        [string] $Kind
    )

    $resolvedPath = (Resolve-Path -LiteralPath $Path -ErrorAction Stop).Path
    $ownerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $systemSid = 'S-1-5-18'
    $administratorsSid = 'S-1-5-32-544'
    $inheritance = if ($Kind -eq 'Directory') { '(OI)(CI)' } else { '' }
    $rules = @(
        "*$ownerSid`:$inheritance" + 'F',
        "*$systemSid`:$inheritance" + 'F',
        "*$administratorsSid`:$inheritance" + 'F'
    )

    & "$env:SystemRoot\System32\icacls.exe" $resolvedPath '/inheritance:r' '/grant:r' @rules | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Yedek yolu için güvenli Windows erişim listesi kurulamadı (icacls kodu $LASTEXITCODE): $resolvedPath"
    }

    $acl = Get-Acl -LiteralPath $resolvedPath -ErrorAction Stop
    if (-not $acl.AreAccessRulesProtected) {
        throw "Yedek yolu ACL kalıtımını kapatamadı: $resolvedPath"
    }
    $expected = @($ownerSid, $systemSid, $administratorsSid)
    $rulesFound = $acl.GetAccessRules($true, $false, [System.Security.Principal.SecurityIdentifier])
    $actual = @($rulesFound | ForEach-Object { $_.IdentityReference.Value } | Sort-Object -Unique)
    if (@(Compare-Object ($expected | Sort-Object -Unique) $actual).Count -ne 0) {
        throw "Yedek yolu ACL doğrulaması başarısız oldu; içerik korunarak işlem durduruldu: $resolvedPath"
    }
    foreach ($sid in $expected) {
        $sidRules = @($rulesFound | Where-Object { $_.IdentityReference.Value -eq $sid })
        if ($sidRules.Count -eq 0 -or @($sidRules | Where-Object { $_.AccessControlType -ne 'Allow' -or -not $_.FileSystemRights.HasFlag([System.Security.AccessControl.FileSystemRights]::FullControl) }).Count -gt 0) {
            throw "Yedek yolu ACL yetki doğrulaması başarısız oldu; içerik korunarak işlem durduruldu: $resolvedPath"
        }
    }
}

Export-ModuleMember -Function Set-EnvanterTRPrivateAcl
