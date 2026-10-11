param(
    [string]$Subject = 'CN=EnvanterTR Localhost'
)

$ErrorActionPreference = 'Stop'
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) { throw 'Run this certificate setup from an elevated PowerShell session.' }
if ($Subject -cne 'CN=EnvanterTR Localhost') { throw 'Only the fixed EnvanterTR localhost certificate subject is supported.' }

$myStore = [Security.Cryptography.X509Certificates.X509Store]::new('My','LocalMachine')
$rootStore = [Security.Cryptography.X509Certificates.X509Store]::new('Root','LocalMachine')
$certificate = $null
$publicCertificate = $null
try {
    $myStore.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $now = Get-Date
    $certificate = $myStore.Certificates | Where-Object {
        $candidate = $_
        $eku = $candidate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' } | Select-Object -First 1
        $validEku = (-not $eku) -or @($eku.EnhancedKeyUsages | Where-Object { $_.Value -eq '1.3.6.1.5.5.7.3.1' }).Count -gt 0
        $candidate.Subject -ceq $Subject -and $candidate.HasPrivateKey -and $candidate.NotBefore -lt $now -and
            $candidate.NotAfter -gt $now -and $validEku -and
            $candidate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::DnsName,$false) -ieq 'localhost'
    } | Sort-Object NotAfter -Descending | Select-Object -First 1

    if (-not $certificate) {
        $certificate = New-SelfSignedCertificate -DnsName 'localhost' -Subject $Subject `
            -CertStoreLocation 'Cert:\LocalMachine\My' -Provider 'Microsoft Enhanced RSA and AES Cryptographic Provider' `
            -KeyAlgorithm RSA -KeyLength 2048 -KeySpec KeyExchange -KeyExportPolicy NonExportable `
            -KeyUsage DigitalSignature,KeyEncipherment -Type SSLServerAuthentication `
            -NotAfter $now.AddYears(3) -FriendlyName 'EnvanterTR local HTTPS'
    }
    if (-not $certificate -or -not $certificate.HasPrivateKey) { throw 'A localhost TLS certificate with a private key could not be created.' }

    $rsa = [Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($certificate)
    if (-not $rsa) { throw 'The localhost TLS private key could not be opened.' }
    try {
        if ($rsa -is [Security.Cryptography.RSACng]) {
            $keyName = $rsa.Key.UniqueName
            $keyPath = Join-Path "$env:ProgramData\Microsoft\Crypto\Keys" $keyName
        } elseif ($rsa -is [Security.Cryptography.RSACryptoServiceProvider]) {
            $keyName = $rsa.CspKeyContainerInfo.UniqueKeyContainerName
            $keyPath = Join-Path "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys" $keyName
        } else { throw "Unsupported certificate private key provider: $($rsa.GetType().FullName)" }
        if ([string]::IsNullOrWhiteSpace($keyName) -or -not (Test-Path -LiteralPath $keyPath -PathType Leaf)) {
            throw 'The machine certificate private-key file could not be located.'
        }
        & icacls.exe $keyPath /grant 'NT AUTHORITY\LOCAL SERVICE:R' /C | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'LocalService could not be granted read access to the localhost certificate private key.' }
    } finally { $rsa.Dispose() }

    $rootStore.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    try {
        $trusted = @($rootStore.Certificates | Where-Object Thumbprint -eq $certificate.Thumbprint)
        if ($trusted.Count -eq 0) {
            $publicCertificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($certificate.RawData)
            $rootStore.Add($publicCertificate)
        }
    } finally { $rootStore.Close() }

    Write-Output $certificate.Thumbprint
} finally {
    if ($myStore) { $myStore.Close() }
    if ($rootStore) { $rootStore.Close() }
    if ($publicCertificate) { $publicCertificate.Dispose() }
}
