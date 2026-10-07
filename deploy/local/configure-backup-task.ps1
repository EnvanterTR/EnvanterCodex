param(
    [switch] $Uninstall,
    [string] $PassphraseFile = '',
    [string] $OutputDirectory = '',
    [ValidatePattern('^([01]\d|2[0-3]):[0-5]\d$')]
    [string] $At = '02:00',
    [ValidateRange(1, 3650)]
    [int] $RetentionDays = 30,
    [ValidatePattern('^[A-Za-z0-9_-]{1,64}$')]
    [string] $TaskName = 'EnvanterTR-Encrypted-Backup'
)

$ErrorActionPreference = 'Stop'
if ($Uninstall) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
    Write-Output "Zamanlanmış görev kaldırıldı: $TaskName"
    return
}

if ([string]::IsNullOrWhiteSpace($PassphraseFile)) { throw '-PassphraseFile ile erişimi kısıtlanmış parola dosyasının yolunu belirtin.' }
$passwordPath = (Resolve-Path -LiteralPath $PassphraseFile -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $passwordPath -PathType Leaf)) { throw 'Parola dosyası bulunamadı.' }
$scriptDirectory = $PSScriptRoot
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $scriptDirectory '..\..')).Path
$runnerScript = Join-Path $scriptDirectory 'run-backup.ps1'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $projectRoot 'backups' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$outputRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path

# Restrict the plaintext passphrase to this Windows user, SYSTEM, and administrators.
$passwordAcl = Get-Acl -LiteralPath $passwordPath
$passwordAcl.SetAccessRuleProtection($true, $false)
$ownerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$systemSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$administratorsSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
foreach ($sid in @($ownerSid, $systemSid, $administratorsSid)) {
    $rule = [System.Security.AccessControl.FileSystemAccessRule]::new($sid, 'FullControl', 'Allow')
    [void]$passwordAcl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $passwordPath -AclObject $passwordAcl

$currentProcessPath = (Get-Process -Id $PID).Path
$taskArguments = '-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "{0}" -PassphraseFile "{1}" -OutputDirectory "{2}" -RetentionDays {3}' -f $runnerScript, $passwordPath, $outputRoot, $RetentionDays
$action = New-ScheduledTaskAction -Execute $currentProcessPath -Argument $taskArguments -WorkingDirectory $projectRoot
$runAt = [DateTime]::Today.Add([TimeSpan]::ParseExact($At, 'hh\:mm', [System.Globalization.CultureInfo]::InvariantCulture))
$trigger = New-ScheduledTaskTrigger -Daily -At $runAt
$principal = New-ScheduledTaskPrincipal -UserId ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 4) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 15)
$task = New-ScheduledTask -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'EnvanterTR local Docker backup: encrypt, verify, and apply retention.'
Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

Write-Output "Günlük $At görevi kuruldu: $TaskName"
Write-Output 'Görev yalnızca bu kullanıcı oturum açmışken çalışır; Docker Desktop açık olmalıdır.'
Write-Output "Parola dosyası ACL ile sınırlandı: $passwordPath"
Write-Output "Kaldırmak için: pwsh -File `"$scriptDirectory\configure-backup-task.ps1`" -Uninstall -TaskName $TaskName"
