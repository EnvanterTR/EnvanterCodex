param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [string]$OutputDirectory = "$env:ProgramData\EnvanterTR\backups",
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin"
)

$ErrorActionPreference = "Stop"
$iterations = 600000
$magic = [byte[]](0x45,0x54,0x42,0x4B,0x01)
$appPath = Join-Path $InstallRoot "app"
$configPath = Join-Path $appPath "appsettings.Production.json"
if (-not (Test-Path -LiteralPath $configPath)) { throw "Production configuration is missing." }
if (-not (Test-Path -LiteralPath (Join-Path $PgBinDirectory "pg_dump.exe"))) { throw "PostgreSQL 17 pg_dump.exe was not found at $PgBinDirectory." }
if (-not (Test-Path -LiteralPath (Join-Path $PgBinDirectory "pg_restore.exe"))) { throw "PostgreSQL 17 pg_restore.exe was not found at $PgBinDirectory." }

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection.ConnectionString = [string]$config.ConnectionStrings.EnvanterTR
function Get-ConnectionValue([string[]]$Names, [string]$Default = "") {
    foreach ($name in $Names) {
        foreach ($key in $connection.Keys) {
            if ([string]::Equals([string]$key, $name, [StringComparison]::OrdinalIgnoreCase)) { return [string]$connection[$key] }
        }
    }
    return $Default
}

$hostName = Get-ConnectionValue @("Host","Server")
$databaseName = Get-ConnectionValue @("Database","Initial Catalog")
$userName = Get-ConnectionValue @("Username","User ID","User")
$dbPassword = Get-ConnectionValue @("Password")
$port = Get-ConnectionValue @("Port") "5432"
if ([string]::IsNullOrWhiteSpace($hostName) -or [string]::IsNullOrWhiteSpace($databaseName) -or [string]::IsNullOrWhiteSpace($userName)) {
    throw "The configured PostgreSQL connection string must include Host, Database, and Username."
}

$service = Get-Service -Name $ServiceName -ErrorAction Stop
if ($service.Status -ne "Running") { throw "The EnvanterTR service must be running before a backup can begin." }
$backupPasswordSecure = Read-Host "Backup encryption passphrase (minimum 16 characters; store it separately)" -AsSecureString
$confirmPasswordSecure = Read-Host "Repeat backup encryption passphrase" -AsSecureString
function Convert-SecureToPlain([Security.SecureString]$Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
$backupPassword = Convert-SecureToPlain $backupPasswordSecure
$confirmPassword = Convert-SecureToPlain $confirmPasswordSecure
$backupPasswordSecure.Dispose()
$confirmPasswordSecure.Dispose()
if ($backupPassword.Length -lt 16) { throw "Use a passphrase with at least 16 characters." }
if (-not [string]::Equals($backupPassword, $confirmPassword, [StringComparison]::Ordinal)) { throw "Passphrases do not match." }

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$root = [IO.Path]::GetFullPath($DataRoot).TrimEnd('\')
$outputFull = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\')
if (-not $outputFull.Equals($root, [StringComparison]::OrdinalIgnoreCase) -and
    -not $outputFull.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "Backups must be stored under the protected EnvanterTR data directory."
}
& icacls.exe $OutputDirectory /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Could not restrict backup directory access." }

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupName = "envantertr-$timestamp-$([guid]::NewGuid().ToString('N').Substring(0,8)).etbackup"
$finalPath = Join-Path $OutputDirectory $backupName
$checksumPath = "$finalPath.sha256"
$work = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-backup-" + [guid]::NewGuid().ToString("N"))
$payload = Join-Path $work "payload"
$databaseDump = Join-Path $payload "database.dump"
$zipPath = Join-Path $work "payload.zip"
$cipherPath = Join-Path $work "cipher.bin"
$decryptedZip = Join-Path $work "verified.zip"
$expandedPath = Join-Path $work "verified"
$tempBackup = "$finalPath.partial"
$serviceWasRunning = $false

function Add-FileToHmac([Security.Cryptography.HMAC]$Hmac, [string]$Path, [long]$Length = -1) {
    $stream = [IO.File]::OpenRead($Path)
    try {
        $buffer = New-Object byte[] 65536
        $remaining = $Length
        while ($Length -lt 0 -or $remaining -gt 0) {
            $wanted = if ($Length -lt 0 -or $remaining -ge $buffer.Length) { $buffer.Length } else { [int]$remaining }
            $read = $stream.Read($buffer, 0, $wanted)
            if ($read -eq 0) { break }
            [void]$Hmac.TransformBlock($buffer, 0, $read, $buffer, 0)
            if ($Length -ge 0) { $remaining -= $read }
        }
        if ($Length -ge 0 -and $remaining -ne 0) { throw "Encrypted archive ended unexpectedly." }
    } finally { $stream.Dispose() }
}

function Protect-Backup([string]$SourceZip, [string]$Destination, [string]$Password, [int]$Rounds, [byte[]]$HeaderMagic) {
    $salt = New-Object byte[] 16
    $iv = New-Object byte[] 16
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($salt); $rng.GetBytes($iv) } finally { $rng.Dispose() }
    $roundBytes = [BitConverter]::GetBytes($Rounds)
    $header = New-Object byte[] ($HeaderMagic.Length + $roundBytes.Length + $salt.Length + $iv.Length)
    [Buffer]::BlockCopy($HeaderMagic,0,$header,0,$HeaderMagic.Length)
    [Buffer]::BlockCopy($roundBytes,0,$header,$HeaderMagic.Length,$roundBytes.Length)
    [Buffer]::BlockCopy($salt,0,$header,$HeaderMagic.Length + $roundBytes.Length,$salt.Length)
    [Buffer]::BlockCopy($iv,0,$header,$HeaderMagic.Length + $roundBytes.Length + $salt.Length,$iv.Length)

    $derive = [Security.Cryptography.Rfc2898DeriveBytes]::new($Password,$salt,$Rounds,[Security.Cryptography.HashAlgorithmName]::SHA256)
    $keys = $derive.GetBytes(64)
    $derive.Dispose()
    $aes = [Security.Cryptography.Aes]::Create()
    $aes.KeySize = 256
    $aes.Mode = [Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    $aes.Key = [byte[]]$keys[0..31]
    $aes.IV = $iv
    $encryptor = $aes.CreateEncryptor()
    $inputStream = [IO.File]::OpenRead($SourceZip)
    $cipherStream = [IO.File]::Create($cipherPath)
    $crypto = [Security.Cryptography.CryptoStream]::new($cipherStream,$encryptor,[Security.Cryptography.CryptoStreamMode]::Write)
    try { $inputStream.CopyTo($crypto) } finally { $crypto.Dispose(); $inputStream.Dispose(); $aes.Dispose() }

    $hmac = [Security.Cryptography.HMACSHA256]::new([byte[]]$keys[32..63])
    [void]$hmac.TransformBlock($header,0,$header.Length,$header,0)
    Add-FileToHmac $hmac $cipherPath
    [void]$hmac.TransformFinalBlock([byte[]]::new(0),0,0)
    $mac = $hmac.Hash
    $hmac.Dispose()
    $output = [IO.File]::Create($Destination)
    $cipherInput = [IO.File]::OpenRead($cipherPath)
    try {
        $output.Write($header,0,$header.Length)
        $cipherInput.CopyTo($output)
        $output.Write($mac,0,$mac.Length)
    } finally { $cipherInput.Dispose(); $output.Dispose() }
    [Array]::Clear($keys,0,$keys.Length)
}

function Verify-And-Decrypt([string]$Source, [string]$Destination, [string]$Password, [byte[]]$HeaderMagic) {
    $headerLength = $HeaderMagic.Length + 4 + 16 + 16
    $file = [IO.File]::OpenRead($Source)
    try {
        if ($file.Length -le ($headerLength + 32)) { throw "Encrypted backup is truncated." }
        $header = New-Object byte[] $headerLength
        if ($file.Read($header,0,$header.Length) -ne $header.Length) { throw "Encrypted backup header is incomplete." }
        for ($index = 0; $index -lt $HeaderMagic.Length; $index++) {
            if ($header[$index] -ne $HeaderMagic[$index]) { throw "Unrecognized backup format." }
        }
        $rounds = [BitConverter]::ToInt32($header,$HeaderMagic.Length)
        if ($rounds -lt 200000 -or $rounds -gt 2000000) { throw "Backup key-derivation parameters are outside accepted limits." }
        $salt = New-Object byte[] 16
        $iv = New-Object byte[] 16
        [Buffer]::BlockCopy($header,$HeaderMagic.Length + 4,$salt,0,16)
        [Buffer]::BlockCopy($header,$HeaderMagic.Length + 20,$iv,0,16)
        $cipherLength = $file.Length - $headerLength - 32
        $file.Position = $file.Length - 32
        $expectedMac = New-Object byte[] 32
        if ($file.Read($expectedMac,0,32) -ne 32) { throw "Backup authentication code is missing." }
        $file.Position = $headerLength
        $cipherOut = [IO.File]::Create($cipherPath)
        $hmac = $null
        try {
            $derive = [Security.Cryptography.Rfc2898DeriveBytes]::new($Password,$salt,$rounds,[Security.Cryptography.HashAlgorithmName]::SHA256)
            $keys = $derive.GetBytes(64)
            $derive.Dispose()
            $hmac = [Security.Cryptography.HMACSHA256]::new([byte[]]$keys[32..63])
            [void]$hmac.TransformBlock($header,0,$header.Length,$header,0)
            $buffer = New-Object byte[] 65536
            $remaining = $cipherLength
            while ($remaining -gt 0) {
                $wanted = [int][Math]::Min($buffer.Length,$remaining)
                $read = $file.Read($buffer,0,$wanted)
                if ($read -eq 0) { throw "Encrypted backup ended unexpectedly." }
                [void]$hmac.TransformBlock($buffer,0,$read,$buffer,0)
                $cipherOut.Write($buffer,0,$read)
                $remaining -= $read
            }
            [void]$hmac.TransformFinalBlock([byte[]]::new(0),0,0)
            $actualMac = $hmac.Hash
            $difference = 0
            for ($index = 0; $index -lt 32; $index++) { $difference = $difference -bor ($actualMac[$index] -bxor $expectedMac[$index]) }
            if ($difference -ne 0) { throw "Backup password is incorrect or the archive has been modified." }
            $cipherOut.Dispose()
            $aes = [Security.Cryptography.Aes]::Create()
            $aes.KeySize = 256
            $aes.Mode = [Security.Cryptography.CipherMode]::CBC
            $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7
            $aes.Key = [byte[]]$keys[0..31]
            $aes.IV = $iv
            $decryptor = $aes.CreateDecryptor()
            $cipherIn = [IO.File]::OpenRead($cipherPath)
            $plainOut = [IO.File]::Create($Destination)
            $crypto = [Security.Cryptography.CryptoStream]::new($cipherIn,$decryptor,[Security.Cryptography.CryptoStreamMode]::Read)
            try { $crypto.CopyTo($plainOut) } finally { $crypto.Dispose(); $plainOut.Dispose(); $aes.Dispose() }
            [Array]::Clear($keys,0,$keys.Length)
        } finally {
            if ($hmac) { $hmac.Dispose() }
            if ($cipherOut -and -not $cipherOut.SafeFileHandle.IsClosed) { $cipherOut.Dispose() }
        }
    } finally { $file.Dispose() }
}

try {
    New-Item -ItemType Directory -Path $payload -Force | Out-Null
    $serviceWasRunning = $true
    Stop-Service -Name $ServiceName
    (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))

    $pgDump = Join-Path $PgBinDirectory "pg_dump.exe"
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $pgDump
    $arguments = @("--format=custom","--no-owner","--no-privileges","--host=$hostName","--port=$port","--username=$userName","--dbname=$databaseName","--file=$databaseDump")
    function Quote-Argument([string]$Value) { return '"' + $Value.Replace('"','\"') + '"' }
    $start.Arguments = (($arguments | ForEach-Object { Quote-Argument $_ }) -join " ")
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    if (-not [string]::IsNullOrEmpty($dbPassword)) { $start.EnvironmentVariables["PGPASSWORD"] = $dbPassword }
    $sslMode = Get-ConnectionValue @("SSL Mode")
    if ($sslMode) {
        $sslMap = @{ Disable="disable"; Prefer="prefer"; Require="require"; VerifyCA="verify-ca"; VerifyFull="verify-full" }
        if ($sslMap.ContainsKey($sslMode)) { $start.EnvironmentVariables["PGSSLMODE"] = $sslMap[$sslMode] }
    }
    $process = [Diagnostics.Process]::Start($start)
    $process.WaitForExit()
    [void]$start.EnvironmentVariables.Remove("PGPASSWORD")
    $dbPassword = $null
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $databaseDump)) { throw "pg_dump failed with exit code $($process.ExitCode)." }
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue

    $mediaSource = [string]$config.Storage.CompanyMediaPath
    $keysSource = [string]$config.DataProtection.KeysPath
    if (-not (Test-Path -LiteralPath $mediaSource -PathType Container)) { throw "Company media directory is missing: $mediaSource" }
    if (-not (Test-Path -LiteralPath $keysSource -PathType Container)) { throw "Data Protection key directory is missing: $keysSource" }
    Copy-Item -LiteralPath $mediaSource -Destination (Join-Path $payload "company-media") -Recurse
    Copy-Item -LiteralPath $keysSource -Destination (Join-Path $payload "data-protection-keys") -Recurse

    $manifestFiles = @(Get-ChildItem -LiteralPath $payload -File -Recurse | ForEach-Object {
        [ordered]@{
            path = $_.FullName.Substring($payload.Length + 1).Replace('\','/')
            length = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    })
    $manifest = [ordered]@{
        format = "EnvanterTR-Windows-Backup-v1"
        createdUtc = [DateTime]::UtcNow.ToString("o")
        database = $databaseName
        fileCount = $manifestFiles.Count
        files = $manifestFiles
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $payload "manifest.json") -Encoding UTF8
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipFile = [IO.File]::Create($zipPath)
    $archive = [IO.Compression.ZipArchive]::new($zipFile,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $payload -File -Recurse) {
            $entryName = $file.FullName.Substring($payload.Length + 1).Replace('\','/')
            $entry = $archive.CreateEntry($entryName,[IO.Compression.CompressionLevel]::Optimal)
            $source = [IO.File]::OpenRead($file.FullName)
            $destination = $entry.Open()
            try { $source.CopyTo($destination) } finally { $destination.Dispose(); $source.Dispose() }
        }
    } finally { $archive.Dispose(); $zipFile.Dispose() }

    $listStart = [Diagnostics.ProcessStartInfo]::new()
    $listStart.FileName = Join-Path $PgBinDirectory "pg_restore.exe"
    $listStart.Arguments = "--list " + (Quote-Argument $databaseDump)
    $listStart.UseShellExecute = $false
    $listStart.CreateNoWindow = $true
    $listStart.RedirectStandardOutput = $true
    $listStart.RedirectStandardError = $true
    $listProcess = [Diagnostics.Process]::Start($listStart)
    $listOutput = $listProcess.StandardOutput.ReadToEnd()
    $listError = $listProcess.StandardError.ReadToEnd()
    $listProcess.WaitForExit()
    if ($listProcess.ExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($listOutput)) { throw "pg_restore could not inspect the database dump: $listError" }

    Protect-Backup $zipPath $tempBackup $backupPassword $iterations $magic
    Verify-And-Decrypt $tempBackup $decryptedZip $backupPassword $magic
    $zipRead = [IO.Compression.ZipFile]::OpenRead($decryptedZip)
    try {
        foreach ($entry in $zipRead.Entries) {
            $relative = $entry.FullName.Replace('/','\')
            if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|\\)\.\.(\\|$)' -or $relative -match '^[A-Za-z]:') {
                throw "Unsafe path in encrypted archive."
            }
            $destinationPath = [IO.Path]::GetFullPath((Join-Path $expandedPath $relative))
            $extractRoot = [IO.Path]::GetFullPath($expandedPath).TrimEnd('\') + '\'
            if (-not $destinationPath.StartsWith($extractRoot,[StringComparison]::OrdinalIgnoreCase)) { throw "Archive entry escapes the extraction directory." }
            if ([string]::IsNullOrEmpty($entry.Name)) {
                New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
                continue
            }
            New-Item -ItemType Directory -Path (Split-Path -Parent $destinationPath) -Force | Out-Null
            $source = $entry.Open()
            $destination = [IO.File]::Create($destinationPath)
            try { $source.CopyTo($destination) } finally { $destination.Dispose(); $source.Dispose() }
        }
    } finally { $zipRead.Dispose() }
    $verifiedManifest = Get-Content -LiteralPath (Join-Path $expandedPath "manifest.json") -Raw | ConvertFrom-Json
    if ($verifiedManifest.format -ne "EnvanterTR-Windows-Backup-v1") { throw "Backup manifest version is invalid." }
    foreach ($entry in $verifiedManifest.files) {
        $relative = ([string]$entry.path).Replace('/','\')
        if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|\\)\.\.(\\|$)') { throw "Unsafe path in backup manifest." }
        $candidate = Join-Path $expandedPath $relative
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Backup verification is missing file $relative" }
        $actualFile = Get-Item -LiteralPath $candidate
        $actualHash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash
        if ($actualFile.Length -ne [long]$entry.length -or $actualHash -ne [string]$entry.sha256) { throw "Backup verification failed for $relative" }
    }
    if ([int]$verifiedManifest.fileCount -ne @($verifiedManifest.files).Count) { throw "Backup manifest file count does not match." }

    Move-Item -LiteralPath $tempBackup -Destination $finalPath
    $digest = (Get-FileHash -LiteralPath $finalPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath $checksumPath -Value "$digest *$backupName" -Encoding ASCII
    Write-Output "Encrypted backup created and verified: $finalPath"
    Write-Output "SHA-256: $digest"
    Write-Warning "The passphrase is not stored. Keep it offline and separately from this backup. This verifies archive authentication, file hashes, and pg_restore listing; it does not perform a live database restore."
} finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    if ($serviceWasRunning) { Start-Service -Name $ServiceName }
    if ($backupPassword) { [Array]::Clear($backupPassword.ToCharArray(),0,$backupPassword.Length); $backupPassword = $null }
    if ($confirmPassword) { [Array]::Clear($confirmPassword.ToCharArray(),0,$confirmPassword.Length); $confirmPassword = $null }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    if (Test-Path -LiteralPath $tempBackup) { Remove-Item -LiteralPath $tempBackup -Force }
}
