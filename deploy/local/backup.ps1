param(
    [string] $OutputDirectory = ""
)

$ErrorActionPreference = 'Stop'
$scriptDirectory = $PSScriptRoot
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $scriptDirectory '..\..')).Path
$composeFile = Join-Path $scriptDirectory 'compose.yaml'
$environmentFile = Join-Path $scriptDirectory '.env'

if (-not (Test-Path -LiteralPath $environmentFile -PathType Leaf)) {
    throw "Gizli ayar dosyası bulunamadı: $environmentFile. Önce yerel kurulumu tamamlayın."
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot 'backups'
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$backupRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
$stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$backupDirectory = Join-Path $backupRoot "envantertr-$stamp-$suffix"
New-Item -ItemType Directory -Path $backupDirectory | Out-Null
$backupAcl = Get-Acl -LiteralPath $backupDirectory
$backupAcl.SetAccessRuleProtection($true, $false)
$ownerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$systemSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$administratorsSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
foreach ($sid in @($ownerSid, $systemSid, $administratorsSid)) {
    $rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
        $sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    [void]$backupAcl.AddAccessRule($rule)
}
Set-Acl -LiteralPath $backupDirectory -AclObject $backupAcl
$dumpName = "database-$suffix.dump"
$helperName = "envantertr-backup-$suffix"
$databaseContainer = $null
$webContainer = $null
$mediaVolume = $null
$dumpPath = Join-Path $backupDirectory $dumpName
$mediaPath = Join-Path $backupDirectory 'company-media.tar.gz'
$helperCreated = $false
$dumpInContainer = $false

function Invoke-Docker([string[]] $Arguments) {
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Docker komutu başarısız oldu (kod $LASTEXITCODE)." }
}

try {
    $composePrefix = @('compose', '--env-file', $environmentFile, '-f', $composeFile)
    $databaseContainer = (& docker @composePrefix ps -q database | Select-Object -First 1).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($databaseContainer)) {
        throw 'Veritabanı konteyneri çalışmıyor. Önce deploy/local yönergeleriyle geliştirme ortamını başlatın.'
    }
    $webContainer = (& docker @composePrefix ps -q web | Select-Object -First 1).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($webContainer)) {
        throw 'Uygulama konteyneri çalışmıyor; şirket medya volume adı güvenle belirlenemedi.'
    }
    $ready = (& docker @composePrefix exec -T database sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "Veritabanı hazır değil: $ready" }
    $mediaVolume = (& docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/envantertr/company-media"}}{{.Name}}{{end}}{{end}}' $webContainer | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($mediaVolume)) { throw 'Şirket medya volume adı belirlenemedi; yedek eksik kalmaması için işlem durduruldu.' }

    $dumpContainerPath = "/tmp/$dumpName"
    $dumpInContainer = $true
    Invoke-Docker ($composePrefix + @('exec', '-T', 'database', 'sh', '-c', "pg_dump --no-owner --no-acl --format=custom --username=`"`$POSTGRES_USER`" `"`$POSTGRES_DB`" --file='$dumpContainerPath'"))
    Invoke-Docker @('cp', "${databaseContainer}:$dumpContainerPath", $dumpPath)

    Invoke-Docker @('create', '--name', $helperName, '--volume', "${mediaVolume}:/source:ro", 'postgres:17-alpine', 'sh', '-c', 'tar -czf /tmp/company-media.tar.gz -C /source .')
    $helperCreated = $true
    Invoke-Docker @('start', '-a', $helperName)
    Invoke-Docker @('cp', "${helperName}:/tmp/company-media.tar.gz", $mediaPath)

    $manifest = [ordered]@{
        format = 'envantertr-local-backup'
        schemaVersion = 1
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        scope = 'local-compose-instance'
        databaseDump = $dumpName
        companyMediaArchive = 'company-media.tar.gz'
        sha256 = [ordered]@{
            databaseDump = (Get-FileHash -LiteralPath $dumpPath -Algorithm SHA256).Hash.ToLowerInvariant()
            companyMediaArchive = (Get-FileHash -LiteralPath $mediaPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        encryption = 'none'
        restoreSupportedByApplication = $false
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backupDirectory 'manifest.json') -Encoding utf8
    Write-Output "Yedek oluşturuldu: $backupDirectory"
    Write-Output 'Kapsam: yerel Compose veritabanı ve şirket medya volume dosyaları. Dosyaları yetkisiz erişime kapalı, ayrı bir depoda saklayın.'
    Write-Output 'Bu yedek şifrelenmemiştir; uygulama içinden geri yükleme henüz desteklenmez.'
}
finally {
    if ($helperCreated) { & docker rm $helperName *> $null }
    if ($dumpInContainer -and $databaseContainer) { & docker exec $databaseContainer rm -f "/tmp/$dumpName" *> $null }
}
