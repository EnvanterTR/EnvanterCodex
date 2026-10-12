param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR"
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
$appPath = Join-Path $InstallRoot "app"
$configPath = Join-Path $appPath "appsettings.Production.json"
$secretsPath = Join-Path $DataRoot "secrets"
$service = Get-Service -Name $ServiceName -ErrorAction Stop
if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) { throw "Production configuration is missing: $configPath" }
if (-not (Test-Path -LiteralPath $secretsPath -PathType Container) -or
    ((Get-Item -LiteralPath $secretsPath).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw "The protected DataRoot secrets directory is missing or unsafe: $secretsPath"
}

$hostName = (Read-Host "SMTP server hostname").Trim()
if ([string]::IsNullOrWhiteSpace($hostName) -or $hostName -match '[\s/\\]') { throw "Enter a valid SMTP hostname." }
$portText = (Read-Host "SMTP port [587]").Trim()
if (-not $portText) { $portText = "587" }
$port = 0
if (-not [int]::TryParse($portText,[ref]$port) -or $port -lt 1 -or $port -gt 65535) { throw "SMTP port must be between 1 and 65535." }
$username = (Read-Host "SMTP username").Trim()
$passwordSecure = Read-Host "SMTP password (hidden)" -AsSecureString
$passwordBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwordSecure)
$passwordBytes = $null
$protectedBytes = $null
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBstr)
    if ([string]::IsNullOrWhiteSpace($username) -or [string]::IsNullOrWhiteSpace($password)) { throw "SMTP username and password are required." }
    $passwordBytes = [Text.UTF8Encoding]::new($false).GetBytes($password)
    $protectedBytes = [Security.Cryptography.ProtectedData]::Protect(
        $passwordBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBstr)
    if ($null -ne $passwordBytes) { [Array]::Clear($passwordBytes,0,$passwordBytes.Length) }
    $password = $null
    $passwordSecure.Dispose()
}

$fromAddress = (Read-Host "Sender email address").Trim()
try { $mailAddress = [Net.Mail.MailAddress]::new($fromAddress); if ($mailAddress.Address -ne $fromAddress) { throw "Use a plain email address." } }
catch { throw "Enter a valid sender email address." }
$supportEmail = (Read-Host "Support mailbox email (optional)").Trim()
if ($supportEmail) {
    try { $supportMailAddress = [Net.Mail.MailAddress]::new($supportEmail); if ($supportMailAddress.Address -ne $supportEmail) { throw "Use a plain email address." } }
    catch { throw "Enter a valid support mailbox email address or leave it blank." }
}
$fromName = (Read-Host "Sender display name [EnvanterTR]").Trim()
if (-not $fromName) { $fromName = "EnvanterTR" }
$publicBaseUrlText = (Read-Host "Public HTTPS URL (example https://inventory.example.com)").Trim().TrimEnd('/')
$publicBaseUrl = $null
if (-not [uri]::TryCreate($publicBaseUrlText,[UriKind]::Absolute,[ref]$publicBaseUrl) -or
    $publicBaseUrl.Scheme -ne "https" -or $publicBaseUrl.UserInfo -or $publicBaseUrl.Query -or $publicBaseUrl.Fragment) {
    throw "Public base URL must be an absolute HTTPS URL without credentials, query, or fragment."
}

$production = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($null -eq $production.WindowsSecrets) { $production | Add-Member -MemberType NoteProperty -Name WindowsSecrets -Value ([pscustomobject]@{}) }
if ($null -eq $production.Email) { $production | Add-Member -MemberType NoteProperty -Name Email -Value ([pscustomobject]@{}) }
if ($null -eq $production.Email.Smtp) { $production.Email | Add-Member -MemberType NoteProperty -Name Smtp -Value ([pscustomobject]@{}) }
if ($null -eq $production.Support) { $production | Add-Member -MemberType NoteProperty -Name Support -Value ([pscustomobject]@{}) }
$oldSecretPath = [string]$production.WindowsSecrets.SmtpPasswordPath
$newSecretPath = Join-Path $secretsPath ("smtp-password-" + [guid]::NewGuid().ToString("N") + ".dpapi")
$temporaryConfig = Join-Path $appPath ("appsettings.Production." + [guid]::NewGuid().ToString("N") + ".tmp")
$backupConfig = Join-Path $appPath ("appsettings.Production." + [guid]::NewGuid().ToString("N") + ".bak")
$configurationReplaced = $false
$wasRunning = $service.Status -eq "Running"
$httpsPort = 8443
try {
    if ($production.Kestrel.Endpoints.Https.Url -match ':(\d+)$') { $httpsPort = [int]$Matches[1] }
    $enableSslText = (Read-Host "Enable SMTP STARTTLS? [Y/n]").Trim()
    $enableSsl = $enableSslText -notmatch '^(?i:n|no|false)$'

    [IO.File]::WriteAllBytes($newSecretPath,$protectedBytes)
    [Array]::Clear($protectedBytes,0,$protectedBytes.Length)
    $protectedBytes = $null
    foreach ($setting in @{
        Host = $hostName
        Port = $port.ToString([Globalization.CultureInfo]::InvariantCulture)
        EnableSsl = $enableSsl.ToString().ToLowerInvariant()
        Username = $username
    }.GetEnumerator()) {
        $production.Email.Smtp | Add-Member -MemberType NoteProperty -Name $setting.Key -Value $setting.Value -Force
    }
    $production.Email | Add-Member -MemberType NoteProperty -Name FromAddress -Value $fromAddress -Force
    $production.Email | Add-Member -MemberType NoteProperty -Name FromName -Value $fromName -Force
    $production.Email | Add-Member -MemberType NoteProperty -Name PublicBaseUrl -Value $publicBaseUrl.AbsoluteUri.TrimEnd('/') -Force
    $production.Support | Add-Member -MemberType NoteProperty -Name Email -Value $supportEmail -Force
    $production.WindowsSecrets | Add-Member -MemberType NoteProperty -Name SmtpPasswordPath -Value $newSecretPath -Force
    $json = $production | ConvertTo-Json -Depth 12
    [IO.File]::WriteAllText($temporaryConfig,$json,[Text.UTF8Encoding]::new($false))

    if ($wasRunning) {
        Stop-Service -Name $ServiceName
        (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
    }
    [IO.File]::Replace($temporaryConfig,$configPath,$backupConfig,$true)
    $configurationReplaced = $true
    if ($wasRunning) {
        Start-Service -Name $ServiceName
        $deadline = (Get-Date).AddMinutes(2)
        $ready = $false
        do {
            Start-Sleep -Seconds 3
            try {
                $statusUri = "https://localhost:$httpsPort/api/status"
                if ($PSVersionTable.PSVersion.Major -ge 7) {
                    $status = Invoke-RestMethod -Uri $statusUri -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
                } else {
                    $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
                    [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
                    try { $status = Invoke-RestMethod -Uri $statusUri -TimeoutSec 5 -ErrorAction Stop }
                    finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
                }
                if ($status.status -eq "ready") { $ready = $true; break }
            } catch { }
        } while ((Get-Date) -lt $deadline)
        if (-not $ready) { throw "Application did not return to ready after SMTP configuration; previous settings will be restored." }
    }
    Remove-Item -LiteralPath $backupConfig -Force -ErrorAction SilentlyContinue
    if ($oldSecretPath -and $oldSecretPath -ne $newSecretPath -and
        [IO.Path]::GetFullPath($oldSecretPath).StartsWith([IO.Path]::GetFullPath($secretsPath).TrimEnd('\') + '\',[StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $oldSecretPath -Force -ErrorAction SilentlyContinue
    }
    Write-Output "SMTP settings saved. SMTP password is DPAPI-protected under DataRoot; application status is ready."
} catch {
    $configurationFailure = $_
    $rollbackSucceeded = -not $configurationReplaced
    if ($configurationReplaced) {
        try {
            if ((Get-Service -Name $ServiceName).Status -eq "Running") {
                Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
                (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
            }
            [IO.File]::Replace($backupConfig,$configPath,$null,$true)
            if ($wasRunning) { Start-Service -Name $ServiceName }
            $rollbackSucceeded = $true
        } catch { Write-Warning "SMTP configuration rollback was incomplete; inspect the saved configuration backup '$backupConfig'. $($_.Exception.Message)" }
    }
    if ($rollbackSucceeded) { Remove-Item -LiteralPath $newSecretPath -Force -ErrorAction SilentlyContinue }
    throw $configurationFailure
} finally {
    if ($null -ne $protectedBytes) { [Array]::Clear($protectedBytes,0,$protectedBytes.Length) }
    Remove-Item -LiteralPath $temporaryConfig -Force -ErrorAction SilentlyContinue
}
