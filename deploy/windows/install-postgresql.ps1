param(
    [ValidateRange(1,65535)][int]$Port = 5432,
    [string]$InstallDirectory = "$env:ProgramFiles\PostgreSQL\17",
    [string]$DownloadDirectory = "$env:ProgramData\EnvanterTR\downloads"
)

$ErrorActionPreference = 'Stop'
$version = '17.11-1'
$installerUri = 'https://get.enterprisedb.com/postgresql/postgresql-17.11-1-windows-x64.exe'
$installerName = [IO.Path]::GetFileName($installerUri)
$installerPath = Join-Path $DownloadDirectory $installerName
$partialPath = "$installerPath.download"
$expectedPublisher = 'EnterpriseDB Corporation'

function Assert-Elevated {
    $principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'PostgreSQL installation requires an elevated administrator session.'
    }
}

function Assert-OfficialInstaller([string]$Path) {
    $signature = Get-AuthenticodeSignature -LiteralPath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
        -not $signature.SignerCertificate -or
        $signature.SignerCertificate.Subject -notmatch [regex]::Escape($expectedPublisher)) {
        throw "PostgreSQL installer signature is invalid or is not from $expectedPublisher. The file was not run."
    }
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host "Verified EDB Authenticode signature and SHA-256: $hash" -ForegroundColor Green
}

Assert-Elevated
$binDirectory = Join-Path $InstallDirectory 'bin'
$psql = Join-Path $binDirectory 'psql.exe'
$services = @(Get-Service -Name 'postgresql*17*' -ErrorAction SilentlyContinue)
if ((Test-Path -LiteralPath $psql -PathType Leaf) -or $services.Count -gt 0) {
    throw "A PostgreSQL 17 installation or service already exists. This helper will not upgrade, repair, or replace it. Verify the installation manually and restart Setup.cmd."
}

if (Test-Path -LiteralPath $DownloadDirectory) {
    $attributes = [IO.File]::GetAttributes($DownloadDirectory)
    if ($attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The PostgreSQL download directory cannot be a junction or symbolic link.' }
} else {
    New-Item -ItemType Directory -Path $DownloadDirectory -Force | Out-Null
}
& icacls.exe $DownloadDirectory /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict access to the PostgreSQL installer cache.' }

if (Test-Path -LiteralPath $installerPath -PathType Leaf) {
    try { Assert-OfficialInstaller -Path $installerPath }
    catch {
        Remove-Item -LiteralPath $installerPath -Force
        Write-Host 'The cached installer was not trusted and has been removed; downloading a fresh copy.' -ForegroundColor Yellow
    }
}

if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    Write-Host "PostgreSQL $version installer is downloading from the official EDB host. It is approximately 360 MB." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $installerUri -OutFile $partialPath -UseBasicParsing
        if (-not (Test-Path -LiteralPath $partialPath -PathType Leaf) -or (Get-Item -LiteralPath $partialPath).Length -lt 100MB) {
            throw 'The downloaded PostgreSQL installer is unexpectedly small.'
        }
        Assert-OfficialInstaller -Path $partialPath
        Move-Item -LiteralPath $partialPath -Destination $installerPath
    } catch {
        Remove-Item -LiteralPath $partialPath -Force -ErrorAction SilentlyContinue
        throw
    }
}

Assert-OfficialInstaller -Path $installerPath
Write-Host 'The signed EDB installer will open. Enter a strong password for its PostgreSQL administrator account; EnvanterTR will ask for it afterward to create its separate least-privilege database role.' -ForegroundColor Yellow
Write-Host 'The EnvanterTR setup does not read or store the PostgreSQL administrator password.'
$arguments = "--mode qt --serverport $Port --disable-components pgAdmin,stackbuilder"
$process = Start-Process -FilePath $installerPath -ArgumentList $arguments -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "The EDB PostgreSQL installer exited with code $($process.ExitCode). No EnvanterTR database or service was created." }

if (-not (Test-Path -LiteralPath $psql -PathType Leaf)) {
    throw "EDB installer finished, but PostgreSQL 17 was not found at $psql. Confirm the PostgreSQL installation completed, then run Setup.cmd again."
}
$versionOutput = (& $psql --version 2>&1 | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $versionOutput -notmatch '\b17\.') {
    throw "PostgreSQL 17 verification failed ($versionOutput). Setup stopped before creating EnvanterTR data."
}
Write-Host "PostgreSQL 17 is installed at $binDirectory. Continue with Setup.cmd to create the EnvanterTR database." -ForegroundColor Green
