param(
    [Parameter(Mandatory = $true)]
    [string] $PassphraseFile,
    [string] $OutputDirectory = "",
    [ValidateRange(1, 3650)]
    [int] $RetentionDays = 30
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = $PSScriptRoot
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $scriptDirectory '..\..')).Path
$backupScript = Join-Path $scriptDirectory 'backup.ps1'
$verifyScript = Join-Path $scriptDirectory 'verify-backup.ps1'
$passwordPath = (Resolve-Path -LiteralPath $PassphraseFile -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $passwordPath -PathType Leaf)) { throw 'Parola dosyası bulunamadı.' }
$passphrase = [System.IO.File]::ReadAllText($passwordPath).TrimEnd([char[]]@("`r", "`n"))
if ($passphrase.Length -lt 16 -or $passphrase.Length -gt 4096) { throw 'Parola en az 16 ve en fazla 4.096 karakter olmalıdır; yedek alınmadı.' }
$passphrase = $null
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot 'backups' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$backupRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
$logDirectory = Join-Path $backupRoot '.scheduled-logs'
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$logAcl = Get-Acl -LiteralPath $logDirectory
$logAcl.SetAccessRuleProtection($true, $false)
$ownerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$systemSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$administratorsSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
foreach ($sid in @($ownerSid, $systemSid, $administratorsSid)) {
    $rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
        $sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    [void]$logAcl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $logDirectory -AclObject $logAcl
$logPath = Join-Path $logDirectory ("run-{0}-{1}.log" -f [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'), [Guid]::NewGuid().ToString('N').Substring(0, 8))
Start-Transcript -LiteralPath $logPath | Out-Null

try {

function Invoke-Docker([string[]] $Arguments) {
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker komutu başarısız oldu (kod $LASTEXITCODE)." }
}

function Invoke-EncryptedArchive([string[]] $ToolArguments) {
    $arguments = @(
        'run', '--rm',
        '--mount', "type=bind,source=$projectRoot,target=/source",
        '--mount', "type=bind,source=$backupRoot,target=/backups",
        '--mount', "type=bind,source=$passwordPath,target=/run/secrets/backup-password,readonly",
        'mcr.microsoft.com/dotnet/sdk:10.0',
        'dotnet', 'run', '--project', '/source/tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj',
        '--configuration', 'Release', '--no-launch-profile', '--'
    ) + $ToolArguments
    Invoke-Docker $arguments
}

$powerShellPath = (Get-Process -Id $PID).Path
$backupOutput = @(& $powerShellPath -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $backupScript -OutputDirectory $backupRoot 2>&1)
if ($LASTEXITCODE -ne 0) {
    $backupOutput | ForEach-Object { Write-Error $_.ToString() -ErrorAction Continue }
    throw 'Yedek oluşturulamadı; şifreleme aşamasına geçilmedi.'
}
$backupOutput | ForEach-Object { Write-Output $_.ToString() }
$createdLine = $backupOutput | ForEach-Object { $_.ToString() } | Where-Object { $_.StartsWith('Yedek oluşturuldu: ', [System.StringComparison]::Ordinal) } | Select-Object -Last 1
if (-not $createdLine) { throw 'Yedek betiği başarılı klasör yolunu bildirmedi; hiçbir dosya silinmedi.' }
$backupDirectory = [System.IO.Path]::GetFullPath($createdLine.Substring('Yedek oluşturuldu: '.Length))
$backupParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetDirectoryName($backupDirectory))
if (-not $backupParent.Equals($backupRoot, [System.StringComparison]::OrdinalIgnoreCase) -or [System.IO.Path]::GetFileName($backupDirectory) -notmatch '^envantertr-\d{8}-\d{6}-[a-f0-9]{8}$') {
    throw 'Yedek klasörünün yolu beklenen konum/biçimde değil; hiçbir dosya silinmedi.'
}

& $powerShellPath -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $verifyScript $backupDirectory
if ($LASTEXITCODE -ne 0) { throw 'Şifrelemeden önce yedek doğrulaması başarısız; ham yedek korundu.' }

$archivePath = '{0}.etbackup' -f $backupDirectory
Invoke-EncryptedArchive @('encrypt', "/backups/$([System.IO.Path]::GetFileName($backupDirectory))", "/backups/$([System.IO.Path]::GetFileName($archivePath))", '/run/secrets/backup-password')
try {
    Invoke-EncryptedArchive @('verify', "/backups/$([System.IO.Path]::GetFileName($archivePath))", '/run/secrets/backup-password')
}
catch {
    Remove-Item -LiteralPath $archivePath -Force -ErrorAction SilentlyContinue
    throw
}

$archiveAcl = Get-Acl -LiteralPath $archivePath
$archiveAcl.SetAccessRuleProtection($true, $false)
$ownerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$systemSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$administratorsSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
foreach ($sid in @($ownerSid, $systemSid, $administratorsSid)) {
    $rule = [System.Security.AccessControl.FileSystemAccessRule]::new($sid, 'FullControl', 'Allow')
    [void]$archiveAcl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $archivePath -AclObject $archiveAcl

# Remove the plaintext directory only after both archive creation and verification succeeded.
if (Test-Path -LiteralPath $backupDirectory -PathType Container) {
    $resolvedBackup = [System.IO.Path]::GetFullPath($backupDirectory)
    $resolvedParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetDirectoryName($resolvedBackup))
    if ($resolvedParent.Equals($backupRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolvedBackup -Recurse -Force
    }
}

$cutoff = [DateTime]::UtcNow.AddDays(-$RetentionDays)
foreach ($oldArchive in Get-ChildItem -LiteralPath $backupRoot -File -Filter 'envantertr-*.etbackup') {
    if ($oldArchive.Name -match '^envantertr-\d{8}-\d{6}-[a-f0-9]{8}\.etbackup$' -and $oldArchive.LastWriteTimeUtc -lt $cutoff) {
        Remove-Item -LiteralPath $oldArchive.FullName -Force
    }
}
foreach ($oldLog in Get-ChildItem -LiteralPath $logDirectory -File -Filter 'run-*.log') {
    if ($oldLog.LastWriteTimeUtc -lt $cutoff) { Remove-Item -LiteralPath $oldLog.FullName -Force }
}

Write-Output "Şifreli ve doğrulanmış yedek hazır: $archivePath"
Write-Output "Saklama süresi: $RetentionDays gün. Ham yedek klasörü işlem sonunda kaldırıldı."
}
finally {
    try { Stop-Transcript | Out-Null } catch { }
}
