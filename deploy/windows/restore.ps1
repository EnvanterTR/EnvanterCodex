param(
    [Parameter(Mandatory = $true)][string]$BackupPath,
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin",
    [string]$PassphraseFile,
    [ValidateRange(1,86400)][int]$PgToolTimeoutSeconds = 1800
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }

$backup = (Resolve-Path -LiteralPath $BackupPath).Path
$appPath = Join-Path $InstallRoot "app"
$configPath = Join-Path $appPath "appsettings.Production.json"
$mediaPath = $null
$keysPath = $null
foreach ($toolName in "pg_restore.exe","psql.exe") {
    if (-not (Test-Path -LiteralPath (Join-Path $PgBinDirectory $toolName) -PathType Leaf)) {
        throw "PostgreSQL recovery tool is missing: $(Join-Path $PgBinDirectory $toolName)"
    }
}
foreach ($required in @($configPath,(Join-Path $appPath "EnvanterTR.Web.exe"),(Join-Path $PSScriptRoot "BackupFormat.psm1"))) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Required installation file is missing: $required" }
}
$service = Get-Service -Name $ServiceName -ErrorAction Stop
if ($service.Status -ne "Running") { throw "The EnvanterTR service must be running before a restore can begin." }

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection.ConnectionString = [string]$config.ConnectionStrings.EnvanterTR
function Get-ConnectionValue([string[]]$Names, [string]$Default = "") {
    foreach ($name in $Names) {
        foreach ($key in $connection.Keys) {
            if ([string]::Equals([string]$key,$name,[StringComparison]::OrdinalIgnoreCase)) { return [string]$connection[$key] }
        }
    }
    return $Default
}
function Quote-Argument([string]$Value) { return '"' + $Value.Replace('"','\"') + '"' }
function Invoke-PgTool([string]$Executable,[string[]]$Arguments,[hashtable]$Environment = @{}) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $Executable
    $start.Arguments = (($Arguments | ForEach-Object { Quote-Argument $_ }) -join " ")
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($key in $Environment.Keys) { $start.EnvironmentVariables[$key] = [string]$Environment[$key] }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($PgToolTimeoutSeconds * 1000)) {
            try { $process.Kill($true) } catch { }
            $process.WaitForExit()
            throw "$([IO.Path]::GetFileName($Executable)) exceeded the $PgToolTimeoutSeconds-second recovery command limit and was terminated."
        }
        $process.WaitForExit()
        $stdout = $stdoutTask.Result
        $stderr = $stderrTask.Result
        if ($process.ExitCode -ne 0) { throw "$([IO.Path]::GetFileName($Executable)) failed with exit code $($process.ExitCode): $stderr" }
        return $stdout
    } finally {
        $process.Dispose()
    }
}
function Quote-SqlIdentifier([string]$Value) { return '"' + $Value.Replace('"','""') + '"' }
function Quote-SqlLiteral([string]$Value) { return "'" + $Value.Replace("'","''") + "'" }

$hostName = Get-ConnectionValue @("Host","Server") "localhost"
$databaseName = Get-ConnectionValue @("Database","Initial Catalog")
$userName = Get-ConnectionValue @("Username","User ID","User")
$dbPassword = Get-ConnectionValue @("Password")
$port = Get-ConnectionValue @("Port") "5432"
$sslMode = Get-ConnectionValue @("SSL Mode")
if ([string]::IsNullOrWhiteSpace($databaseName) -or [string]::IsNullOrWhiteSpace($userName)) {
    throw "The configured PostgreSQL connection string must include Database and Username."
}
$mediaPath = [IO.Path]::GetFullPath([string]$config.Storage.CompanyMediaPath)
$keysPath = [IO.Path]::GetFullPath([string]$config.DataProtection.KeysPath)
if ([string]::IsNullOrWhiteSpace([string]$config.Storage.CompanyMediaPath) -or [string]::IsNullOrWhiteSpace([string]$config.DataProtection.KeysPath)) {
    throw "Production configuration must define company media and Data Protection key directories."
}
$dataRootFull = [IO.Path]::GetFullPath($DataRoot).TrimEnd('\') + '\'
if (-not (Test-Path -LiteralPath $DataRoot -PathType Container)) { throw "DataRoot does not exist: $DataRoot" }
foreach ($path in @([IO.Path]::GetFullPath($DataRoot),$mediaPath,$keysPath)) {
    if ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Restore will not atomically swap a reparse-point data directory: $path"
    }
}
foreach ($path in @($mediaPath,$keysPath)) {
    if (-not $path.StartsWith($dataRootFull,[StringComparison]::OrdinalIgnoreCase)) {
        throw "Restore requires media and Data Protection keys to be under DataRoot so their directories can be atomically swapped: $path"
    }
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { throw "Configured data directory is missing: $path" }
}

$initialEnvironment = @{ PGPASSWORD = $dbPassword }
if ($sslMode) {
    $sslMap = @{ Disable="disable"; Prefer="prefer"; Require="require"; VerifyCA="verify-ca"; VerifyFull="verify-full" }
    if ($sslMap.ContainsKey($sslMode)) { $initialEnvironment["PGSSLMODE"] = $sslMap[$sslMode] }
}
$psql = Join-Path $PgBinDirectory "psql.exe"
$pgRestore = Join-Path $PgBinDirectory "pg_restore.exe"
$quotedDatabase = Quote-SqlIdentifier $databaseName
$privileges = Invoke-PgTool $psql @("-X","-A","-t","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command","SELECT (rolsuper OR rolcreatedb)::text FROM pg_roles WHERE rolname = current_user;") $initialEnvironment
if ($privileges.Trim() -ne "true") { throw "The configured PostgreSQL role must have CREATEDB or superuser permission to stage a verified restore." }
$ownsDatabase = Invoke-PgTool $psql @("-X","-A","-t","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command","SELECT (d.datdba = (SELECT oid FROM pg_roles WHERE rolname = current_user) OR (SELECT rolsuper FROM pg_roles WHERE rolname = current_user))::text FROM pg_database d WHERE d.datname = '$($databaseName.Replace("'","''"))';") $initialEnvironment
if ($ownsDatabase.Trim() -ne "true") { throw "The configured PostgreSQL role must own the current database to switch it safely." }

$plainPassword = $null
if (-not [string]::IsNullOrWhiteSpace($PassphraseFile)) {
    $passphraseFull = [IO.Path]::GetFullPath($PassphraseFile)
    $dataRootPath = [IO.Path]::GetFullPath($DataRoot).TrimEnd('\') + '\'
    if (-not $passphraseFull.StartsWith($dataRootPath,[StringComparison]::OrdinalIgnoreCase)) {
        throw "PassphraseFile must be inside the protected EnvanterTR DataRoot."
    }
    if (-not (Test-Path -LiteralPath $passphraseFull -PathType Leaf)) { throw "PassphraseFile does not exist." }
    Add-Type -AssemblyName System.Security
    $protected = [IO.File]::ReadAllBytes($passphraseFull)
    $clear = $null
    try {
        $clear = [Security.Cryptography.ProtectedData]::Unprotect($protected,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
        $plainPassword = [Text.Encoding]::Unicode.GetString($clear)
    } finally {
        [Array]::Clear($protected,0,$protected.Length)
        if ($clear) { [Array]::Clear($clear,0,$clear.Length) }
    }
} else {
    $secure = Read-Host "Backup encryption passphrase" -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer); $secure.Dispose() }
}
if ([string]::IsNullOrWhiteSpace($plainPassword) -or $plainPassword.Length -lt 16) { throw "Backup passphrase is invalid or shorter than 16 characters." }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$suffix = [guid]::NewGuid().ToString("N").Substring(0,12)
$stageDatabase = "envtr_restore_${stamp}_${suffix}"
$oldDatabase = "envtr_previous_${stamp}_${suffix}"
$failedDatabase = "envtr_failed_${stamp}_${suffix}"
$work = Join-Path ([IO.Path]::GetTempPath()) ("envantertr-restore-" + [guid]::NewGuid().ToString("N"))
$stageRoot = Join-Path $DataRoot (".restore-stage-" + $suffix)
$oldMedia = "$mediaPath.restore-$stamp-$suffix"
$oldKeys = "$keysPath.restore-$stamp-$suffix"
$cipherPath = Join-Path $work "cipher.bin"
$plainZip = Join-Path $work "verified.zip"
$expanded = Join-Path $work "expanded"
$databaseSwitched = $false
$databaseOldRenamed = $false
$mediaSwitched = $false
$mediaOldMoved = $false
$keysSwitched = $false
$keysOldMoved = $false
$serviceStopped = $false
$liveMutationStarted = $false
$connectionEnvironment = @{ PGPASSWORD = $dbPassword }
if ($sslMode -and $initialEnvironment.ContainsKey("PGSSLMODE")) { $connectionEnvironment["PGSSLMODE"] = $initialEnvironment["PGSSLMODE"] }
$maintenanceMutex = [Threading.Mutex]::new($false,"Global\EnvanterTR_Maintenance")
$maintenanceMutexAcquired = $false
try { $maintenanceMutexAcquired = $maintenanceMutex.WaitOne(0) }
catch [Threading.AbandonedMutexException] { $maintenanceMutexAcquired = $true; Write-Warning "An earlier maintenance process ended unexpectedly; continuing with the abandoned operation lock." }
if (-not $maintenanceMutexAcquired) { $maintenanceMutex.Dispose(); throw "Another EnvanterTR backup, restore or update operation is in progress." }

try {
    foreach ($path in @($work,$stageRoot,$oldMedia,$oldKeys)) {
        if (Test-Path -LiteralPath $path) { throw "Restore staging or recovery path already exists: $path" }
    }
    New-Item -ItemType Directory -Path $work,$expanded,$stageRoot -Force | Out-Null
    & icacls.exe $work /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not restrict restore workspace access." }
    & icacls.exe $stageRoot /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not restrict staged media and key access." }

    Import-Module (Join-Path $PSScriptRoot "BackupFormat.psm1") -Force -DisableNameChecking
    $magic = [byte[]](0x45,0x54,0x42,0x4B,0x01)
    Verify-And-Decrypt $backup $plainZip $plainPassword $magic $cipherPath
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($plainZip)
    try {
        $archiveEntries = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in $archive.Entries) {
            $relative = $entry.FullName.Replace('/','\')
            if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|\\)\.\.(\\|$)' -or $relative -match '^[A-Za-z]:' -or $relative -match '[\x00-\x1F<>:"|?*]') {
                throw "Backup contains an unsafe path."
            }
            if (-not $archiveEntries.Add($relative)) { throw "Backup contains duplicate archive paths." }
            $destination = [IO.Path]::GetFullPath((Join-Path $expanded $relative))
            $root = [IO.Path]::GetFullPath($expanded).TrimEnd('\') + '\'
            if (-not $destination.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)) { throw "Backup path escapes the restore workspace." }
            if ([string]::IsNullOrEmpty($entry.Name)) { New-Item -ItemType Directory -Path $destination -Force | Out-Null; continue }
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            $source = $entry.Open()
            $output = [IO.File]::Create($destination)
            try { $source.CopyTo($output) } finally { $output.Dispose(); $source.Dispose() }
        }
    } finally { $archive.Dispose() }

    $manifestPath = Join-Path $expanded "manifest.json"
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Backup manifest is missing." }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.format -ne "EnvanterTR-Windows-Backup-v1" -or [int]$manifest.fileCount -ne @($manifest.files).Count) {
        throw "Backup manifest format or file count is invalid."
    }
    foreach ($name in @("company-media","data-protection-keys")) {
        New-Item -ItemType Directory -Path (Join-Path $expanded $name) -Force | Out-Null
    }
    $manifestPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in $manifest.files) {
        $relative = ([string]$file.path).Replace('/','\')
        if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|\\)\.\.(\\|$)' -or $relative -match '^[A-Za-z]:' -or $relative -match '[\x00-\x1F<>:"|?*]') { throw "Backup manifest contains an unsafe path." }
        if (-not $manifestPaths.Add($relative)) { throw "Backup manifest contains duplicate file paths." }
        $candidate = [IO.Path]::GetFullPath((Join-Path $expanded $relative))
        $root = [IO.Path]::GetFullPath($expanded).TrimEnd('\') + '\'
        if (-not $candidate.StartsWith($root,[StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "Backup verification is missing file '$relative'."
        }
        $actual = Get-Item -LiteralPath $candidate
        $hash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual.Length -ne [long]$file.length -or $hash -ne [string]$file.sha256) { throw "Backup verification failed for '$relative'." }
    }
    $actualFiles = @(Get-ChildItem -LiteralPath $expanded -File -Recurse | Where-Object { $_.FullName -ne $manifestPath })
    if ($actualFiles.Count -ne $manifestPaths.Count) { throw "The extracted backup file set does not match its manifest." }
    foreach ($actualFile in $actualFiles) {
        $relative = $actualFile.FullName.Substring($expanded.Length + 1).Replace('\','/')
        if (-not $manifestPaths.Contains($relative.Replace('/','\'))) { throw "Unlisted file found in backup: $relative" }
    }
    $dumpEntry = @($manifest.files | Where-Object { $_.path -eq "database.dump" })
    if ($dumpEntry.Count -ne 1) { throw "Backup must contain exactly one verified database.dump." }
    $dump = [IO.Path]::GetFullPath((Join-Path $expanded ([string]$dumpEntry[0].path)))
    if (-not (Test-Path -LiteralPath $dump -PathType Leaf)) { throw "Verified database.dump is missing." }
    foreach ($name in @("company-media","data-protection-keys")) {
        if (-not (Test-Path -LiteralPath (Join-Path $expanded $name) -PathType Container)) { throw "Backup is missing $name." }
    }
    $catalog = Invoke-PgTool $pgRestore @("--list",$dump)
    if ([string]::IsNullOrWhiteSpace($catalog)) { throw "PostgreSQL could not inspect the backup dump." }

    New-Item -ItemType Directory -Path (Join-Path $stageRoot "company-media"),(Join-Path $stageRoot "data-protection-keys") -Force | Out-Null
    foreach ($name in @("company-media","data-protection-keys")) {
        $sourceDirectory = Join-Path $expanded $name
        $targetDirectory = Join-Path $stageRoot $name
        foreach ($item in Get-ChildItem -LiteralPath $sourceDirectory -Force) {
            Copy-Item -LiteralPath $item.FullName -Destination $targetDirectory -Recurse -Force
        }
    }

    $localeJson = Invoke-PgTool $psql @("-X","-A","-t","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command","SELECT json_build_object('encoding',pg_encoding_to_char(encoding),'provider',datlocprovider,'collate',datcollate,'ctype',datctype,'icuLocale',daticulocale)::text FROM pg_database WHERE datname = '$($databaseName.Replace("'","''"))';") $connectionEnvironment
    if ([string]::IsNullOrWhiteSpace($localeJson)) { throw "Could not read the live database encoding and locale settings." }
    $databaseLocale = $localeJson | ConvertFrom-Json
    if ($databaseLocale.encoding -ne "UTF8") { throw "The live database uses '$($databaseLocale.encoding)'; only UTF8 databases can be safely restored by this release." }
    if ($databaseLocale.provider -eq "c") {
        $localeOptions = "LOCALE_PROVIDER libc LC_COLLATE $(Quote-SqlLiteral ([string]$databaseLocale.collate)) LC_CTYPE $(Quote-SqlLiteral ([string]$databaseLocale.ctype))"
    } elseif ($databaseLocale.provider -eq "i" -and -not [string]::IsNullOrWhiteSpace([string]$databaseLocale.icuLocale)) {
        $localeOptions = "LOCALE_PROVIDER icu ICU_LOCALE $(Quote-SqlLiteral ([string]$databaseLocale.icuLocale))"
    } else {
        throw "The live database uses unsupported locale provider '$($databaseLocale.provider)'; refusing to create a staging database with different comparison rules."
    }
    $createdbSql = "CREATE DATABASE $(Quote-SqlIdentifier $stageDatabase) OWNER $(Quote-SqlIdentifier $userName) TEMPLATE template0 ENCODING $(Quote-SqlLiteral ([string]$databaseLocale.encoding)) $localeOptions;"
    [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command",$createdbSql) $connectionEnvironment)
    [void](Invoke-PgTool $pgRestore @("--exit-on-error","--single-transaction","--no-owner","--no-privileges","--host=$hostName","--port=$port","--username=$userName","--dbname=$stageDatabase",$dump) $connectionEnvironment)
    $migrationCount = Invoke-PgTool $psql @("-X","-A","-t","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=$stageDatabase","--command",'SELECT count(*) FROM public."__EFMigrationsHistory";') $connectionEnvironment
    $parsedMigrationCount = 0
    if (-not [int]::TryParse($migrationCount.Trim(),[ref]$parsedMigrationCount) -or $parsedMigrationCount -lt 1) {
        throw "The staged database does not contain a valid EnvanterTR migration history."
    }

    $confirmation = Read-Host "This replaces the live database, media and Data Protection keys. Type RESTORE $databaseName to continue"
    if ($confirmation -cne "RESTORE $databaseName") { throw "Restore cancelled; live data was not changed." }
    $backupScript = Join-Path $appPath "backup.ps1"
    if (-not (Test-Path -LiteralPath $backupScript -PathType Leaf)) { throw "Pre-restore safety backup script is missing: $backupScript" }
    $safetyOutput = & $backupScript -InstallRoot $InstallRoot -DataRoot $DataRoot -ServiceName $ServiceName -PgBinDirectory $PgBinDirectory
    if (-not $?) { throw "Pre-restore safety backup failed; live data was not changed." }
    $safetyOutput | ForEach-Object { Write-Output $_ }

    $serviceStopped = $true
    Stop-Service -Name $ServiceName
    (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
    $liveMutationStarted = $true
    $terminateSql = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$($databaseName.Replace("'","''"))' AND pid <> pg_backend_pid();"
    [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command",$terminateSql) $connectionEnvironment)
    $renameOldSql = "ALTER DATABASE $quotedDatabase RENAME TO $(Quote-SqlIdentifier $oldDatabase);"
    [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command",$renameOldSql) $connectionEnvironment)
    $databaseOldRenamed = $true
    $renameStageSql = "ALTER DATABASE $(Quote-SqlIdentifier $stageDatabase) RENAME TO $quotedDatabase;"
    [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command",$renameStageSql) $connectionEnvironment)
    $databaseSwitched = $true

    Move-Item -LiteralPath $mediaPath -Destination $oldMedia
    $mediaOldMoved = $true
    Move-Item -LiteralPath (Join-Path $stageRoot "company-media") -Destination $mediaPath
    $mediaSwitched = $true
    Move-Item -LiteralPath $keysPath -Destination $oldKeys
    $keysOldMoved = $true
    Move-Item -LiteralPath (Join-Path $stageRoot "data-protection-keys") -Destination $keysPath
    $keysSwitched = $true
    & icacls.exe $mediaPath /grant:r "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set restored media permissions." }
    & icacls.exe $keysPath /grant:r "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M" /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not set restored Data Protection key permissions." }

    Push-Location $appPath
    try {
        & (Join-Path $appPath "EnvanterTR.Web.exe") --migrate
        if ($LASTEXITCODE -ne 0) { throw "Migration on the staged restore database failed with exit code $LASTEXITCODE." }
    } finally { Pop-Location }
    Start-Service -Name $ServiceName
    $deadline = (Get-Date).AddMinutes(2)
    $healthy = $false
    do {
        Start-Sleep -Seconds 3
        try {
            $httpsPort = ([uri]$config.Kestrel.Endpoints.Https.Url).Port
            if ($PSVersionTable.PSVersion.Major -ge 7) {
                $status = Invoke-RestMethod -Uri "https://localhost:$httpsPort/api/status" -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
            } else {
                $previousCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback
                [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
                try { $status = Invoke-RestMethod -Uri "https://localhost:$httpsPort/api/status" -TimeoutSec 5 -ErrorAction Stop }
                finally { [Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback }
            }
            if ($status.status -eq "ready" -and $status.pendingMigrationCount -eq 0) { $healthy = $true; break }
        } catch { }
    } while ((Get-Date) -lt $deadline)
    if (-not $healthy) { throw "Restored service did not report ready." }

    Write-Output "Restore completed and health check passed."
    Write-Output "Safety backup: $($safetyOutput | Where-Object { $_ -match 'Encrypted backup created and verified:' } | Select-Object -Last 1)"
    Write-Output "Previous database retained as: $oldDatabase"
    Write-Output "Previous media retained at: $oldMedia"
    Write-Output "Previous Data Protection keys retained at: $oldKeys"
    Write-Output "Restored from: $backup"
} catch {
    $restoreError = $_
    try {
        if ($serviceStopped -and (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue).Status -eq "Running") {
            Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
            (Get-Service -Name $ServiceName).WaitForStatus("Stopped",[TimeSpan]::FromSeconds(60))
        }
    } catch { }
    if ($databaseSwitched -or $databaseOldRenamed) {
        try {
            $failedExists = Invoke-PgTool $psql @("-X","-A","-t","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command","SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = '$databaseName')::text;") $connectionEnvironment
            if ($failedExists.Trim() -eq "true") {
                $renameFailedSql = "ALTER DATABASE $quotedDatabase RENAME TO $(Quote-SqlIdentifier $failedDatabase);"
                [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command",$renameFailedSql) $connectionEnvironment)
            }
            $restoreOldSql = "ALTER DATABASE $(Quote-SqlIdentifier $oldDatabase) RENAME TO $quotedDatabase;"
            [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command",$restoreOldSql) $connectionEnvironment)
        } catch { Write-Warning "Database rollback failed; preserved database names require administrator recovery. $($_.Exception.Message)" }
    }
    if ($keysSwitched -or $keysOldMoved) {
        try {
            if (Test-Path -LiteralPath $keysPath) { Move-Item -LiteralPath $keysPath -Destination (Join-Path $stageRoot "failed-data-protection-keys") }
            Move-Item -LiteralPath $oldKeys -Destination $keysPath
        } catch { Write-Warning "Data Protection keys rollback failed: $($_.Exception.Message)" }
    }
    if ($mediaSwitched -or $mediaOldMoved) {
        try {
            if (Test-Path -LiteralPath $mediaPath) { Move-Item -LiteralPath $mediaPath -Destination (Join-Path $stageRoot "failed-company-media") }
            Move-Item -LiteralPath $oldMedia -Destination $mediaPath
        } catch { Write-Warning "Company media rollback failed: $($_.Exception.Message)" }
    }
    if ($serviceStopped) {
        try { Start-Service -Name $ServiceName -ErrorAction Stop } catch { Write-Warning "The original service could not be restarted: $($_.Exception.Message)" }
    }
    throw "Restore failed; previous database/media/key data were retained where possible. $($restoreError.Exception.Message)"
} finally {
    try {
    if ($plainPassword) { [Array]::Clear($plainPassword.ToCharArray(),0,$plainPassword.Length); $plainPassword = $null }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
    if (-not $liveMutationStarted) {
        try {
            $stageExists = Invoke-PgTool $psql @("-X","-A","-t","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command","SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = '$stageDatabase')::text;") $connectionEnvironment
            if ($stageExists.Trim() -eq "true") {
                [void](Invoke-PgTool $psql @("-X","-v","ON_ERROR_STOP=1","--host=$hostName","--port=$port","--username=$userName","--dbname=postgres","--command","DROP DATABASE $(Quote-SqlIdentifier $stageDatabase);") $connectionEnvironment)
            }
        } catch { Write-Warning "Could not remove the temporary staged database '$stageDatabase': $($_.Exception.Message)" }
        if (Test-Path -LiteralPath $stageRoot) { Remove-Item -LiteralPath $stageRoot -Recurse -Force -ErrorAction SilentlyContinue }
    }
    if (Test-Path -LiteralPath $stageRoot) {
        $remaining = Get-ChildItem -LiteralPath $stageRoot -Force -ErrorAction SilentlyContinue
        if (-not $remaining) { Remove-Item -LiteralPath $stageRoot -Force -ErrorAction SilentlyContinue }
    }
    } finally {
        if ($maintenanceMutexAcquired) { $maintenanceMutex.ReleaseMutex(); $maintenanceMutexAcquired = $false }
        $maintenanceMutex.Dispose()
    }
}
