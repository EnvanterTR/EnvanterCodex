param(
    [string]$PackagePath = $PSScriptRoot,
    [string]$PostgreSqlHost = "127.0.0.1",
    [ValidateRange(1,65535)][int]$PostgreSqlPort = 5432,
    [ValidateRange(1,65535)][int]$HttpsPort = 8443,
    [string]$DnsName = "",
    [string]$CertificateSubject = "",
    [switch]$AllowMissingPostgreSql,
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [ValidateRange(1,1000)][int]$MinimumFreeGB = 10
)

$ErrorActionPreference = "Stop"
$checks = [Collections.Generic.List[object]]::new()

function Add-Check([string]$Name,[ValidateSet("PASS","WARN","FAIL")][string]$Result,[string]$Detail) {
    $checks.Add([pscustomobject]@{ Name=$Name; Result=$Result; Detail=$Detail })
}

function Test-TcpEndpoint([string]$HostName,[int]$Port) {
    $client = [Net.Sockets.TcpClient]::new()
    try {
        $task = $client.ConnectAsync($HostName,$Port)
        if (-not $task.Wait(3000)) { return $false }
        return $client.Connected
    } catch { return $false }
    finally { $client.Dispose() }
}

function Get-CertificateDnsNames([Security.Cryptography.X509Certificates.X509Certificate2]$Certificate) {
    $extension = $Certificate.Extensions | Where-Object { $_.Oid.Value -eq "2.5.29.17" } | Select-Object -First 1
    if (-not $extension) { return @($Certificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::DnsName,$false)) }
    $bytes = $extension.RawData
    $offset = 0
    if ($bytes.Length -lt 2 -or $bytes[$offset++] -ne 0x30) { return @() }
    $firstLength = [int]$bytes[$offset++]
    if (($firstLength -band 0x80) -eq 0) { $sequenceLength = $firstLength }
    else {
        $lengthBytes = $firstLength -band 0x7f
        if ($lengthBytes -lt 1 -or $lengthBytes -gt 4 -or ($offset + $lengthBytes) -gt $bytes.Length) { return @() }
        $sequenceLength = 0
        for ($index=0; $index -lt $lengthBytes; $index++) { $sequenceLength = ($sequenceLength -shl 8) -bor [int]$bytes[$offset++] }
    }
    $end = [Math]::Min($bytes.Length,$offset + $sequenceLength)
    $names = [Collections.Generic.List[string]]::new()
    while ($offset -lt $end) {
        $tag = [int]$bytes[$offset++]
        if ($offset -ge $end) { break }
        $length = [int]$bytes[$offset++]
        if (($length -band 0x80) -eq 0) { $valueLength = $length }
        else {
            $lengthBytes = $length -band 0x7f
            if ($lengthBytes -lt 1 -or $lengthBytes -gt 4 -or ($offset + $lengthBytes) -gt $end) { return @() }
            $valueLength = 0
            for ($index=0; $index -lt $lengthBytes; $index++) { $valueLength = ($valueLength -shl 8) -bor [int]$bytes[$offset++] }
        }
        if (($offset + $valueLength) -gt $end) { return @() }
        if ($tag -eq 0x82) { $names.Add([Text.Encoding]::ASCII.GetString($bytes,$offset,$valueLength)) }
        elseif ($tag -eq 0x87 -and $valueLength -in @(4,16)) {
            $ipBytes = New-Object byte[] $valueLength
            [Array]::Copy($bytes,$offset,$ipBytes,0,$valueLength)
            $names.Add(([Net.IPAddress]::new($ipBytes)).ToString())
        }
        $offset += $valueLength
    }
    if ($names.Count -eq 0) { return @($Certificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::DnsName,$false)) }
    return $names.ToArray()
}

function Test-CertificateDnsName([string]$Pattern,[string]$Name) {
    if ($Pattern -ieq $Name) { return $true }
    if ($Pattern.StartsWith("*.",[StringComparison]::Ordinal)) {
        $suffix = $Pattern.Substring(1)
        if ($Name.EndsWith($suffix,[StringComparison]::OrdinalIgnoreCase)) {
            $left = $Name.Substring(0,$Name.Length - $suffix.Length)
            return $left.Length -gt 0 -and $left -notmatch '\.'
        }
    }
    return $false
}

function Test-SafeDirectory([string]$Path,[string]$OtherPath) {
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    $other = [IO.Path]::GetFullPath($OtherPath).TrimEnd('\') + '\'
    if ($full.StartsWith($other,[StringComparison]::OrdinalIgnoreCase) -or
        $other.StartsWith($full,[StringComparison]::OrdinalIgnoreCase)) { return $false }
    foreach ($candidate in @($full,$other)) {
        $cursor = $candidate.TrimEnd('\')
        while (-not [string]::IsNullOrWhiteSpace($cursor)) {
            if (Test-Path -LiteralPath $cursor -PathType Container) {
                try { $attributes = [IO.File]::GetAttributes($cursor) }
                catch { return $false }
                if ($attributes -band [IO.FileAttributes]::ReparsePoint) { return $false }
            }
            $parent = [IO.Directory]::GetParent($cursor)
            if (-not $parent) { break }
            $cursor = $parent.FullName
        }
    }
    return $true
}

try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        Add-Check "Operating system" FAIL "This package requires Windows Server 2019 or 2022 x64."
    } else {
        $os = Get-CimInstance Win32_OperatingSystem
        $supported = $os.Caption -match 'Windows Server 2019|Windows Server 2022'
        if ($supported) { Add-Check "Operating system" PASS "$($os.Caption), build $($os.BuildNumber)." }
        else { Add-Check "Operating system" FAIL "$($os.Caption) is not supported by the current Windows Server package. Windows 11 personal use needs its separate installer, which is not released yet." }
    }
} catch { Add-Check "Operating system" FAIL "Could not identify Windows version: $($_.Exception.Message)" }

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) { Add-Check "Administrator rights" PASS "The current PowerShell session is elevated." }
else { Add-Check "Administrator rights" FAIL "Open PowerShell with Run as administrator before installing the service." }

if ($PSVersionTable.PSVersion.Major -ge 5) { Add-Check "PowerShell" PASS "PowerShell $($PSVersionTable.PSVersion) is available." }
else { Add-Check "PowerShell" FAIL "Windows PowerShell 5.1 or PowerShell 7 is required." }

try {
    $package = (Resolve-Path -LiteralPath $PackagePath).Path
    if (Test-Path -LiteralPath (Join-Path $package "EnvanterTR.Web.exe") -PathType Leaf) {
        Add-Check "Application package" PASS "EnvanterTR.Web.exe is present in $package."
    } else { Add-Check "Application package" FAIL "EnvanterTR.Web.exe is missing from $package. Extract the verified Windows ZIP first." }
} catch { Add-Check "Application package" FAIL "Package folder not found: $PackagePath" }

if (Test-SafeDirectory $InstallRoot $DataRoot) { Add-Check "Install/data paths" PASS "Paths are separate and are not reparse points." }
else { Add-Check "Install/data paths" FAIL "InstallRoot and DataRoot must be separate dedicated directories, not junctions or symlinks." }

if (Get-Service -Name "EnvanterTR" -ErrorAction SilentlyContinue) {
    Add-Check "Existing installation" FAIL "The EnvanterTR service already exists. Use update.ps1 for an existing installation; this preflight never replaces it."
} else { Add-Check "Existing installation" PASS "No EnvanterTR service is registered at the default service name." }

try {
    foreach ($targetPath in @($InstallRoot,$DataRoot)) {
        $root = [IO.Path]::GetPathRoot([IO.Path]::GetFullPath($targetPath))
        $drive = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='$($root.TrimEnd('\'))'"
        if (-not $drive -or ([double]$drive.FreeSpace -lt ($MinimumFreeGB * 1GB))) {
            throw "Drive $root needs at least $MinimumFreeGB GB free for $targetPath."
        }
    }
    Add-Check "Free disk space" PASS "Install and data volumes each have at least $MinimumFreeGB GB free."
} catch { Add-Check "Free disk space" FAIL $_.Exception.Message }

foreach ($port in @(@{ Name="PostgreSQL TCP"; Host=$PostgreSqlHost; Port=$PostgreSqlPort },@{ Name="HTTPS port"; Host="127.0.0.1"; Port=$HttpsPort })) {
    if ($port.Name -eq "HTTPS port") {
        $listener = Get-NetTCPConnection -State Listen -LocalPort $port.Port -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($listener) { Add-Check $port.Name FAIL "Port $($port.Port) is already in use by process id $($listener.OwningProcess)." }
        else { Add-Check $port.Name PASS "TCP port $($port.Port) is available." }
    } elseif ($port.Name -eq "PostgreSQL TCP" -and $AllowMissingPostgreSql) {
        Add-Check $port.Name WARN "PostgreSQL is not required for the remaining read-only checks. The guided setup can offer the signed PostgreSQL 17.11 installer after a separate confirmation."
    } elseif (Test-TcpEndpoint $port.Host $port.Port) {
        Add-Check $port.Name PASS "PostgreSQL responds at $($port.Host):$($port.Port). Confirm it is PostgreSQL 17 before installation."
    } else { Add-Check $port.Name FAIL "Cannot connect to PostgreSQL at $($port.Host):$($port.Port). Start PostgreSQL or correct the host and port." }
}

try {
    $store = [Security.Cryptography.X509Certificates.X509Store]::new("My","LocalMachine")
    $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
    try {
        $now = Get-Date
        $candidates = @($store.Certificates | Where-Object {
            $_.HasPrivateKey -and $_.NotBefore -lt $now -and $_.NotAfter -gt $now -and
            ([string]::IsNullOrWhiteSpace($CertificateSubject) -or $_.Subject -eq $CertificateSubject)
        })
        if (-not [string]::IsNullOrWhiteSpace($DnsName)) {
            $candidates = @($candidates | Where-Object {
                $certificateNames = @(Get-CertificateDnsNames $_)
                @($certificateNames | Where-Object { Test-CertificateDnsName $_ $DnsName }).Count -gt 0
            })
        }
        if ($candidates.Count -eq 1) {
            $cert = $candidates[0]
            $serverAuth = $cert.Extensions | Where-Object { $_.Oid.Value -eq "2.5.29.37" } | Select-Object -First 1
            $serverAuthValues = @($serverAuth.EnhancedKeyUsages | ForEach-Object { $_.Value })
            $hasServerAuth = -not $serverAuth -or $serverAuthValues -contains "1.3.6.1.5.5.7.3.1"
            if ($hasServerAuth) { Add-Check "HTTPS certificate" PASS "$($cert.Subject), expires $($cert.NotAfter.ToString('yyyy-MM-dd')); private key present." }
            else { Add-Check "HTTPS certificate" FAIL "$($cert.Subject) does not allow TLS server authentication." }
            if ([string]::IsNullOrWhiteSpace($DnsName)) { Add-Check "TLS hostname" WARN "Pass -DnsName with the name employees will use so the certificate name can be checked." }
            else { Add-Check "TLS hostname" PASS "Certificate name matches $DnsName." }
        } elseif ($candidates.Count -eq 0) {
            $nameHint = if ([string]::IsNullOrWhiteSpace($DnsName)) { "" } else { " and DNS name '$DnsName'" }
            Add-Check "HTTPS certificate" FAIL "No unexpired certificate with a private key matched LocalMachine\My$nameHint. Provide -CertificateSubject and -DnsName as appropriate."
        } else {
            Add-Check "HTTPS certificate" FAIL "$($candidates.Count) certificates matched. Re-run with -CertificateSubject to select one exact subject."
        }
    } finally { $store.Close() }
} catch { Add-Check "HTTPS certificate" FAIL "Could not inspect LocalMachine\My: $($_.Exception.Message)" }

Add-Check "Certificate service access" WARN "The installer must be granted read access to the certificate private key for NT AUTHORITY\LocalService. Verify this before opening network access."
Add-Check "Firewall and DNS" WARN "This check does not change Windows Firewall or DNS. Confirm the chosen server name resolves for clients and permit HTTPS TCP $HttpsPort only through your network policy."

Write-Output "EnvanterTR Windows Server installation preflight (read-only)"
Write-Output ""
foreach ($check in $checks) {
    $line = "[{0}] {1}: {2}" -f $check.Result,$check.Name,$check.Detail
    if ($check.Result -eq "FAIL") { Write-Host $line -ForegroundColor Red }
    elseif ($check.Result -eq "WARN") { Write-Host $line -ForegroundColor Yellow }
    else { Write-Host $line -ForegroundColor Green }
}
$failures = @($checks | Where-Object Result -eq "FAIL")
$warnings = @($checks | Where-Object Result -eq "WARN")
Write-Output ""
if ($failures.Count -gt 0) {
    Write-Output "$($failures.Count) prerequisite(s) need attention. No system changes were made."
    exit 1
}
Write-Output "All required checks passed. $($warnings.Count) manual network/certificate item(s) remain; no system changes were made."
exit 0
