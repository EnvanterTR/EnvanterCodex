param(
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin",
    [ValidateRange(1,65535)][int]$Port = 5432,
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,62}$')][string]$AdminUser = "postgres",
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,62}$')][string]$AppRole = "envantertr_app",
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,62}$')][string]$DatabaseName = "envantertr",
    [switch]$ReturnConnectionStringSecure,
    [string]$PendingConnectionPath = ""
)

$ErrorActionPreference = "Stop"
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)) { throw "Run PowerShell as Administrator." }
$psql = Join-Path $PgBinDirectory "psql.exe"
if (-not (Test-Path -LiteralPath $psql -PathType Leaf)) { throw "PostgreSQL 17 psql.exe was not found at $psql" }
if ($AppRole -eq $AdminUser) { throw "The EnvanterTR application role must differ from the PostgreSQL administrator role." }

function Convert-SecureToPlain([Security.SecureString]$Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
function Quote-Argument([string]$Value) { return '"' + $Value.Replace('"','\"') + '"' }
function Invoke-LocalPsql([string]$Sql, [hashtable]$Environment) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $psql
    $arguments = @('-X','-v','ON_ERROR_STOP=1','-A','-t','--host=127.0.0.1',"--port=$Port",("--username=$AdminUser"),'--dbname=postgres')
    $start.Arguments = (($arguments | ForEach-Object { Quote-Argument $_ }) -join ' ')
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($key in $Environment.Keys) { $start.EnvironmentVariables[$key] = [string]$Environment[$key] }
    $process = [Diagnostics.Process]::Start($start)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.StandardInput.WriteLine($Sql)
    $process.StandardInput.Close()
    if (-not $process.WaitForExit(60000)) {
        try { $process.Kill() } catch { }
        throw "PostgreSQL preparation timed out after 60 seconds."
    }
    $stdout = $stdoutTask.Result.Trim()
    $stderr = $stderrTask.Result.Trim()
    if ($process.ExitCode -ne 0) { throw "PostgreSQL preparation failed: $stderr" }
    return $stdout
}

$adminPasswordSecure = Read-Host "Local PostgreSQL administrator password" -AsSecureString
$adminPassword = Convert-SecureToPlain $adminPasswordSecure
$adminPasswordSecure.Dispose()
if ([string]::IsNullOrEmpty($adminPassword)) { throw "The PostgreSQL administrator password cannot be empty." }
$environment = @{ PGPASSWORD = $adminPassword; PGSSLMODE = "disable" }
$roleSql = $AppRole.Replace("'","''")
$databaseSql = $DatabaseName.Replace("'","''")
$roleExists = $false
$databaseExists = $false
$roleCreated = $false
$databaseCreated = $false
$appPassword = $null
try {
    $existing = Invoke-LocalPsql "SELECT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='$roleSql'), EXISTS(SELECT 1 FROM pg_database WHERE datname='$databaseSql');" $environment
    $parts = $existing.Split('|')
    if ($parts.Count -ne 2) { throw "Could not verify the requested PostgreSQL role/database." }
    $roleExists = $parts[0].Trim() -eq 't'
    $databaseExists = $parts[1].Trim() -eq 't'
    if ($roleExists) { throw "PostgreSQL role '$AppRole' already exists; no role or database was changed." }
    if ($databaseExists) { throw "PostgreSQL database '$DatabaseName' already exists; no role or database was changed." }

    $passwordBytes = New-Object byte[] 32
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($passwordBytes); $appPassword = [BitConverter]::ToString($passwordBytes).Replace('-','').ToLowerInvariant() }
    finally { $random.Dispose(); [Array]::Clear($passwordBytes,0,$passwordBytes.Length) }
    [void](Invoke-LocalPsql "CREATE ROLE `"$AppRole`" LOGIN PASSWORD '$appPassword' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;" $environment)
    $roleCreated = $true
    [void](Invoke-LocalPsql "CREATE DATABASE `"$DatabaseName`" OWNER `"$AppRole`" TEMPLATE template0 ENCODING 'UTF8';" $environment)
    $databaseCreated = $true
    $connectionString = "Host=127.0.0.1;Port=$Port;Database=$DatabaseName;Username=$AppRole;Password=$appPassword;SSL Mode=Disable;Timeout=15;Command Timeout=30"
    if ($PendingConnectionPath) {
        $connectionBytes = [Text.Encoding]::UTF8.GetBytes($connectionString)
        $protectedBytes = $null
        try {
            $protectedBytes = [Security.Cryptography.ProtectedData]::Protect(
                $connectionBytes,$null,[Security.Cryptography.DataProtectionScope]::LocalMachine)
            [IO.File]::WriteAllBytes($PendingConnectionPath,$protectedBytes)
        } finally {
            [Array]::Clear($connectionBytes,0,$connectionBytes.Length)
            if ($protectedBytes) { [Array]::Clear($protectedBytes,0,$protectedBytes.Length) }
        }
    }
    if ($ReturnConnectionStringSecure) {
        [pscustomobject]@{ ConnectionStringSecure = (ConvertTo-SecureString -String $connectionString -AsPlainText -Force) }
    } else {
        Write-Output "Created an empty UTF-8 database '$DatabaseName' and least-privilege login role '$AppRole' on 127.0.0.1:$Port."
        Write-Output "Use this connection string at the EnvanterTR install.ps1 prompt (copy it directly; do not save it in a file):"
        Write-Output $connectionString
        Write-Warning "The generated database password is shown once and cannot be recovered. Keep the installer prompt and this PowerShell session private."
    }
} catch {
    if ($databaseCreated) {
        try { [void](Invoke-LocalPsql "DROP DATABASE `"$DatabaseName`";" $environment) }
        catch { Write-Warning "Could not remove the newly created empty database '$DatabaseName'; inspect PostgreSQL manually. $($_.Exception.Message)" }
    }
    if ($roleCreated) {
        try { [void](Invoke-LocalPsql "DROP ROLE `"$AppRole`";" $environment) }
        catch { Write-Warning "Could not remove the newly created role '$AppRole'; inspect PostgreSQL manually. $($_.Exception.Message)" }
    }
    throw
} finally {
    if ($environment.ContainsKey('PGPASSWORD')) { $environment['PGPASSWORD'] = $null }
    if ($adminPassword) { [Array]::Clear($adminPassword.ToCharArray(),0,$adminPassword.Length); $adminPassword = $null }
    if ($appPassword) { [Array]::Clear($appPassword.ToCharArray(),0,$appPassword.Length); $appPassword = $null }
    $connectionString = $null
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
