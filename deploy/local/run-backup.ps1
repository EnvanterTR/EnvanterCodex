param(
    [string] $PassphraseFile = '',
    [string] $OutputDirectory = "",
    [ValidateRange(1, 3650)]
    [int] $RetentionDays = 30
)

$ErrorActionPreference = 'Stop'
$privateAclModule = Join-Path $PSScriptRoot 'PrivateAcl.psm1'
if (-not (Test-Path -LiteralPath $privateAclModule -PathType Leaf)) { throw 'Güvenli yedek ACL yardımcısı bulunamadı.' }
Import-Module $privateAclModule -Force
$scriptDirectory = $PSScriptRoot
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $scriptDirectory '..\..')).Path
$backupScript = Join-Path $scriptDirectory 'backup.ps1'
$verifyScript = Join-Path $scriptDirectory 'verify-backup.ps1'
$temporaryPasswordPath = $null
$passwordPath = $null
$firstPassphrase = $null
$confirmationPassphrase = $null
$firstPointer = [IntPtr]::Zero
$confirmationPointer = [IntPtr]::Zero
$firstPlaintext = $null
$confirmationPlaintext = $null
if ([string]::IsNullOrWhiteSpace($PassphraseFile)) {
    Write-Output 'Yedek parolası ekranda gösterilmeden iki kez istenecek. En az 16 karakter kullanın ve parolayı yedekten ayrı bir yerde saklayın.'
    $firstPassphrase = Read-Host 'Yeni yedek parolası' -AsSecureString
    $confirmationPassphrase = Read-Host 'Yedek parolasını tekrar girin' -AsSecureString
    try {
        $firstPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($firstPassphrase)
        $confirmationPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($confirmationPassphrase)
        $firstPlaintext = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($firstPointer)
        $confirmationPlaintext = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($confirmationPointer)
        if (-not [string]::Equals($firstPlaintext, $confirmationPlaintext, [StringComparison]::Ordinal)) { throw 'Parolalar eşleşmedi; yedek alınmadı.' }
        if ($firstPlaintext.Length -lt 16 -or $firstPlaintext.Length -gt 4096) { throw 'Parola en az 16 ve en fazla 4.096 karakter olmalıdır; yedek alınmadı.' }
        $secretRoot = Join-Path $env:USERPROFILE '.envantertr-secrets'
        if ((Test-Path -LiteralPath $secretRoot -PathType Container) -and ((Get-Item -LiteralPath $secretRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Geçici parola dizini junction veya symlink olamaz.'
        }
        New-Item -ItemType Directory -Force -Path $secretRoot | Out-Null
        Set-EnvanterTRPrivateAcl -Path $secretRoot -Kind Directory
        $temporaryPasswordPath = Join-Path $secretRoot ('.backup-{0}.tmp' -f [Guid]::NewGuid().ToString('N'))
        try {
            [IO.File]::WriteAllText($temporaryPasswordPath, $firstPlaintext, [Text.UTF8Encoding]::new($false))
            Set-EnvanterTRPrivateAcl -Path $temporaryPasswordPath -Kind File
        }
        catch {
            Remove-Item -LiteralPath $temporaryPasswordPath -Force -ErrorAction SilentlyContinue
            $temporaryPasswordPath = $null
            throw
        }
        $passwordPath = (Resolve-Path -LiteralPath $temporaryPasswordPath).Path
    }
    finally {
        if ($firstPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($firstPointer) }
        if ($confirmationPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($confirmationPointer) }
        if ($firstPassphrase) { $firstPassphrase.Dispose() }
        if ($confirmationPassphrase) { $confirmationPassphrase.Dispose() }
        $firstPlaintext = $null
        $confirmationPlaintext = $null
    }
}
else {
    $passwordPath = (Resolve-Path -LiteralPath $PassphraseFile -ErrorAction Stop).Path
    if (-not (Test-Path -LiteralPath $passwordPath -PathType Leaf)) { throw 'Parola dosyası bulunamadı.' }
    $passphrase = [System.IO.File]::ReadAllText($passwordPath).TrimEnd([char[]]@("`r", "`n"))
    if ($passphrase.Length -lt 16 -or $passphrase.Length -gt 4096) { throw 'Parola en az 16 ve en fazla 4.096 karakter olmalıdır; yedek alınmadı.' }
    $passphrase = $null
}
try {
    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot 'backups' }
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $backupRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
    $logDirectory = Join-Path $backupRoot '.scheduled-logs'
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    Set-EnvanterTRPrivateAcl -Path $logDirectory -Kind Directory
    $logPath = Join-Path $logDirectory ("run-{0}-{1}.log" -f [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'), [Guid]::NewGuid().ToString('N').Substring(0, 8))
    Start-Transcript -LiteralPath $logPath | Out-Null
}
catch {
    if ($temporaryPasswordPath -and (Test-Path -LiteralPath $temporaryPasswordPath -PathType Leaf)) {
        Remove-Item -LiteralPath $temporaryPasswordPath -Force -ErrorAction SilentlyContinue
    }
    throw
}

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

Set-EnvanterTRPrivateAcl -Path $archivePath -Kind File

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
    if ($temporaryPasswordPath -and (Test-Path -LiteralPath $temporaryPasswordPath -PathType Leaf)) {
        Remove-Item -LiteralPath $temporaryPasswordPath -Force -ErrorAction SilentlyContinue
    }
}
