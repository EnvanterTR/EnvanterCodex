param(
    [string]$InstallRoot = "$env:ProgramFiles\EnvanterTR",
    [string]$DataRoot = "$env:ProgramData\EnvanterTR",
    [string]$ServiceName = "EnvanterTR",
    [string]$OutputDirectory = "$env:ProgramData\EnvanterTR\backups",
    [string]$PgBinDirectory = "$env:ProgramFiles\PostgreSQL\17\bin",
    [Parameter(Mandatory = $true)][string]$PassphraseFile,
    [string]$MirrorDirectory,
    [ValidateRange(0,3650)][int]$RetentionDays = 30,
    [ValidateRange(0,3650)][int]$MirrorRetentionDays = 90,
    [string]$TaskName = "EnvanterTR Daily Encrypted Backup"
)

$ErrorActionPreference = "Stop"
$backupScript = Join-Path $PSScriptRoot "backup.ps1"
$alertScript = Join-Path $PSScriptRoot "send-backup-alert.ps1"
try {
    & $backupScript -InstallRoot $InstallRoot -DataRoot $DataRoot -ServiceName $ServiceName `
        -OutputDirectory $OutputDirectory -PgBinDirectory $PgBinDirectory -PassphraseFile $PassphraseFile `
        -MirrorDirectory $MirrorDirectory -RetentionDays $RetentionDays -MirrorRetentionDays $MirrorRetentionDays
} catch {
    if (Test-Path -LiteralPath $alertScript -PathType Leaf) {
        try { & $alertScript -DataRoot $DataRoot -Event "scheduled_backup_failed" -Source $TaskName }
        catch { Write-Warning "Backup failed and the optional alert handler could not run." }
    }
    throw
}
