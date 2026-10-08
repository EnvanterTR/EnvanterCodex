function Get-EnvanterTRConnectionString {
    param([Parameter(Mandatory = $true)][string]$ConfigurationPath)

    $config = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
    $secretPath = [string]$config.WindowsSecrets.ConnectionStringPath
    if ([string]::IsNullOrWhiteSpace($secretPath)) {
        # Read-only compatibility for installations created before DPAPI storage.
        $legacy = [string]$config.ConnectionStrings.EnvanterTR
        if ([string]::IsNullOrWhiteSpace($legacy)) { throw "Production configuration has no PostgreSQL connection secret." }
        return $legacy
    }
    if ($env:OS -ne "Windows_NT") { throw "Windows DPAPI secrets must be opened on Windows." }
    if (-not (Test-Path -LiteralPath $secretPath -PathType Leaf)) { throw "Protected PostgreSQL connection secret is missing: $secretPath" }

    $protected = [IO.File]::ReadAllBytes($secretPath)
    $clear = $null
    try {
        $clear = [Security.Cryptography.ProtectedData]::Unprotect(
            $protected,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        $value = [Text.UTF8Encoding]::new($false,$true).GetString($clear)
        if ([string]::IsNullOrWhiteSpace($value)) { throw "Protected PostgreSQL connection secret is empty." }
        return $value
    } catch [Security.Cryptography.CryptographicException] {
        throw "Protected PostgreSQL connection secret cannot be opened on this Windows machine."
    } finally {
        [Array]::Clear($protected,0,$protected.Length)
        if ($null -ne $clear) { [Array]::Clear($clear,0,$clear.Length) }
    }
}

Export-ModuleMember -Function Get-EnvanterTRConnectionString
