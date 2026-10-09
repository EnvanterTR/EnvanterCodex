$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$drillRoot = Join-Path $tempRoot ('envantertr-recovery-' + [Guid]::NewGuid().ToString('N'))
$passphrasePath = Join-Path $drillRoot 'passphrase.txt'
$backupRoot = Join-Path $drillRoot 'backup'
$restoredRoot = Join-Path $drillRoot 'restored'
$backupScript = Join-Path $projectRoot 'deploy/local/run-backup.ps1'
$restorePreviewScript = Join-Path $projectRoot 'deploy/local/restore-preview.ps1'
$backupToolProject = Join-Path $projectRoot 'tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj'
$privateAclModule = Join-Path $projectRoot 'deploy/local/PrivateAcl.psm1'
$drillCompleted = $false

if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'deploy/local/.env') -PathType Leaf)) {
    throw 'Yerel Compose gizli ayar dosyası bulunamadı; önce deploy/local yönergeleriyle geliştirme ortamını kurun.'
}
foreach ($path in @($backupScript, $restorePreviewScript, $backupToolProject, $privateAclModule)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Kurtarma provası dosyası bulunamadı: $path" }
}

New-Item -ItemType Directory -Path $drillRoot | Out-Null
try {
    Import-Module $privateAclModule -Force
    Set-EnvanterTRPrivateAcl -Path $drillRoot -Kind Directory
    $passphrase = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    [IO.File]::WriteAllText($passphrasePath, $passphrase, [Text.UTF8Encoding]::new($false))
    $passphrase = $null
    Set-EnvanterTRPrivateAcl -Path $passphrasePath -Kind File

    & $backupScript -PassphraseFile $passphrasePath -OutputDirectory $backupRoot -RetentionDays 1
    if ($LASTEXITCODE -ne 0) { throw 'Şifreli yerel yedek oluşturma/doğrulama başarısız oldu.' }
    $archive = @(Get-ChildItem -LiteralPath $backupRoot -File -Filter 'envantertr-*.etbackup')
    if ($archive.Count -ne 1) { throw "Tek bir şifreli deneme yedeği bekleniyordu; bulunan: $($archive.Count)." }

    $mounts = @(
        '--mount', "type=bind,source=$projectRoot,target=/source",
        '--mount', "type=bind,source=$drillRoot,target=/drill"
    )
    & docker run --rm @mounts mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project /source/tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj --configuration Release --no-launch-profile -- decrypt "/drill/backup/$($archive[0].Name)" /drill/restored /drill/passphrase.txt
    if ($LASTEXITCODE -ne 0) { throw 'Şifreli yedeğin güvenli açılması başarısız oldu.' }

    $previewOutput = @(& $restorePreviewScript -BackupDirectory $restoredRoot 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "İzole geri yükleme provası başarısız oldu: $($previewOutput -join ' ')" }
    $previewOutput | ForEach-Object { Write-Output $_ }
    $summary = $previewOutput -join "`n"
    if ($summary -notmatch 'Migration kaydı: ([1-9]\d*); medya dosyası: ([1-9]\d*);') {
        throw 'İzole geri yükleme PostgreSQL şemasını ve medya dosyalarını doğrulamadı.'
    }

    Write-Output ("Şifreli arşiv bayt sayısı: {0}. Yedek, açma, PostgreSQL ve medya provası başarılı." -f $archive[0].Length)
    $drillCompleted = $true
}
finally {
    $resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedDrillRoot = [IO.Path]::GetFullPath($drillRoot)
    if (-not $resolvedDrillRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Kurtarma provası geçici yolu sistem geçici dizininin dışında; temizlik durduruldu.'
    }
    if (Test-Path -LiteralPath $resolvedDrillRoot -PathType Container) {
        Remove-Item -LiteralPath $resolvedDrillRoot -Recurse -Force
    }
    if ($drillCompleted) { Write-Output 'Geçici parola, şifresiz yedek, açılmış kopya ve prova konteyneri temizlendi.' }
}
