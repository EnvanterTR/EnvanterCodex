param([Parameter(Mandatory = $true)][string]$PackagePath)

$ErrorActionPreference = "Stop"
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$exe = Join-Path $package "EnvanterTR.Web.exe"
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Published Windows application is missing: $exe" }

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-dpapi-startup-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
$process = $null
$oldEnvironment = @{
    AspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
    AspNetCoreUrls = $env:ASPNETCORE_URLS
    DataProtectionKeysPath = $env:DataProtection__KeysPath
    StorageCompanyMediaPath = $env:Storage__CompanyMediaPath
    ConnectionString = $env:ConnectionStrings__EnvanterTR
    WindowsSecretPath = $env:WindowsSecrets__ConnectionStringPath
    SmtpSecretPath = $env:WindowsSecrets__SmtpPasswordPath
}

try {
    $secretPath = Join-Path $tempRoot "database-connection.dpapi"
    $connectionBytes = [Text.Encoding]::UTF8.GetBytes(
        "Host=127.0.0.1;Port=1;Database=dpapi_probe;Username=probe;Password=Test-only-secret;Timeout=1;Command Timeout=1")
    try {
        $protectedBytes = [Security.Cryptography.ProtectedData]::Protect(
            $connectionBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        [IO.File]::WriteAllBytes($secretPath,$protectedBytes)
        [Array]::Clear($protectedBytes,0,$protectedBytes.Length)
    } finally { [Array]::Clear($connectionBytes,0,$connectionBytes.Length) }
    $smtpSecretPath = Join-Path $tempRoot "smtp-password.dpapi"
    $smtpPasswordBytes = [Text.Encoding]::UTF8.GetBytes("Test-only-SMTP-secret")
    try {
        $protectedSmtpBytes = [Security.Cryptography.ProtectedData]::Protect(
            $smtpPasswordBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        [IO.File]::WriteAllBytes($smtpSecretPath,$protectedSmtpBytes)
        [Array]::Clear($protectedSmtpBytes,0,$protectedSmtpBytes.Length)
    } finally { [Array]::Clear($smtpPasswordBytes,0,$smtpPasswordBytes.Length) }

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
    $listener.Start()
    $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    $listener.Stop()

    $settings = [ordered]@{
        WindowsSecrets = @{ ConnectionStringPath = $secretPath; SmtpPasswordPath = $smtpSecretPath }
        DataProtection = @{ KeysPath = (Join-Path $tempRoot "keys") }
        Storage = @{ CompanyMediaPath = (Join-Path $tempRoot "media") }
    }
    $settings | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $tempRoot "appsettings.Production.json") -Encoding UTF8

    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$port"
    $env:DataProtection__KeysPath = Join-Path $tempRoot "keys"
    $env:Storage__CompanyMediaPath = Join-Path $tempRoot "media"
    $env:ConnectionStrings__EnvanterTR = $null
    $env:WindowsSecrets__ConnectionStringPath = $null
    $env:WindowsSecrets__SmtpPasswordPath = $null

    $stdoutPath = Join-Path $tempRoot "stdout.log"
    $stderrPath = Join-Path $tempRoot "stderr.log"
    $process = Start-Process -FilePath $exe -WorkingDirectory $tempRoot -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    $http = [Net.Http.HttpClient]::new()
    $deadline = (Get-Date).AddSeconds(60)
    $started = $false
    $responseBody = $null
    try {
        do {
            Start-Sleep -Seconds 2
            if ($process.HasExited) { break }
            try {
                $response = $http.GetAsync("http://127.0.0.1:$port/api/status").GetAwaiter().GetResult()
                $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ($responseBody -match '"product"\s*:\s*"EnvanterTR"' -and
                    $responseBody -match '"status"\s*:\s*"database-unavailable"') {
                    $started = $true
                    break
                }
            } catch { }
        } while ((Get-Date) -lt $deadline)
    } finally { $http.Dispose() }

    if (-not $started) {
        $diagnostics = @()
        if (Test-Path -LiteralPath $stdoutPath) { $diagnostics += Get-Content -LiteralPath $stdoutPath -Tail 25 }
        if (Test-Path -LiteralPath $stderrPath) { $diagnostics += Get-Content -LiteralPath $stderrPath -Tail 25 }
        throw "The published app did not start with the LocalMachine DPAPI secret and expose its status API. $($diagnostics -join [Environment]::NewLine)"
    }
    Write-Output "PASS: published Windows x64 app opened the DPAPI database and SMTP secrets and served /api/status; the isolated database target was intentionally unavailable."
} finally {
    if ($process -and -not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
    $env:ASPNETCORE_ENVIRONMENT = $oldEnvironment.AspNetCoreEnvironment
    $env:ASPNETCORE_URLS = $oldEnvironment.AspNetCoreUrls
    $env:DataProtection__KeysPath = $oldEnvironment.DataProtectionKeysPath
    $env:Storage__CompanyMediaPath = $oldEnvironment.StorageCompanyMediaPath
    $env:ConnectionStrings__EnvanterTR = $oldEnvironment.ConnectionString
    $env:WindowsSecrets__ConnectionStringPath = $oldEnvironment.WindowsSecretPath
    $env:WindowsSecrets__SmtpPasswordPath = $oldEnvironment.SmtpSecretPath
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
