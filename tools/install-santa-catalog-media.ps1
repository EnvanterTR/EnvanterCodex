param([string]$ComposeProject = 'envantertr-product-dev')

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$composeFile = (Resolve-Path (Join-Path $repoRoot 'deploy\local\compose.yaml')).Path
$mediaRoot = '/var/lib/envantertr/company-media'
$media = Join-Path $PSScriptRoot 'demo-media'
$stageRoot = Join-Path ([IO.Path]::GetTempPath()) "envantertr-santa-media-$PID"
New-Item -ItemType Directory -Force -Path $media | Out-Null
New-Item -ItemType Directory -Force -Path $stageRoot | Out-Null

function Invoke-DockerChecked([string[]]$DockerArguments) {
    $output = & docker @DockerArguments
    if ($LASTEXITCODE -ne 0) { throw "Docker komutu başarısız oldu: docker $($DockerArguments -join ' ')" }
    return $output
}
function Invoke-Db([string]$Sql) {
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Sql))
    $cmd = "printf '%s' '$encoded' | base64 -d | psql -U envantertr -d envantertr -At -F '|'"
    return Invoke-DockerChecked @('exec',$databaseContainer,'sh','-c',$cmd)
}
function Get-Image([string]$Name,[string]$Url) {
    $path = Join-Path $media $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Invoke-WebRequest -Uri $Url -OutFile $path -TimeoutSec 45 -MaximumRedirection 8
    }
    if ((Get-Item -LiteralPath $path).Length -lt 128) { throw "Görsel indirilemedi veya bozuk: $Name" }
    return $path
}
function Copy-CompanyImage([string]$Source,[string]$RelativePath,[string]$TargetName) {
    $directory = Join-Path (Join-Path $stageRoot $companyPath) ($RelativePath.Replace('/','\'))
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    Copy-Item -LiteralPath $Source -Destination (Join-Path $directory $TargetName) -Force
}

$databaseContainer = (Invoke-DockerChecked @('compose','--project-name',$ComposeProject,'--file',$composeFile,'ps','-q','database') | Select-Object -First 1).Trim()
$webContainer = (Invoke-DockerChecked @('compose','--project-name',$ComposeProject,'--file',$composeFile,'ps','-q','web') | Select-Object -First 1).Trim()
if (-not $databaseContainer -or -not $webContainer) { throw "'$ComposeProject' Compose projesi çalışır durumda olmalıdır." }
$company = (Invoke-Db 'SELECT "Id"::text FROM "Companies" WHERE "Slug"=''santa-demo'';') | Select-Object -First 1
if (-not $company -or $company -notmatch '^[0-9a-fA-F-]{36}$') { throw 'Santa demo tenant bulunamadı.' }
$companyPath = $company.Replace('-','').ToLowerInvariant()

# Product photos are public vendor/reseller catalog illustrations; employee photos are synthetic demo profiles.
$products = @{
    laptop = Get-Image 'catalog-laptop.png' 'https://p2-ofp.static.pub/fes/cms/2024/03/27/hs3ajyroypf14ob9qcg4sn637kjex7689328.png?height=1200&width=1200'
    latitude = Get-Image 'catalog-latitude.jpg' 'https://www.1stop.lt/storage/shop/products/324000-325000/324523/9aed7948bb7bdb5bb68cc32e73fedadbd1eae1b0.jpeg'
    elitebook = Get-Image 'catalog-elitebook.png' 'https://mx-media.hptiendaenlinea.com/catalog/product/cache/74c1057f7991b4edb2bc7bdaa94de933/B/8/B8JV4LT-1_T1744403730.png'
    desktop = Get-Image 'catalog-desktop.jpg' 'https://i.dell.com/sites/csimages/App-Merchandizing_Images/all/1939_US_DFH_NPI_OptiPlex_7020_sff_plus_1280x1280.jpg'
    prodesk = Get-Image 'catalog-prodesk.png' 'https://cdn11.bigcommerce.com/s-bfxxgrup/images/stencil/1280x1280/products/141332/190308/400G9SFF__18046.1701286941.png?c=2'
    server = Join-Path $media 'santa-server.jpg'
    dellR760 = Get-Image 'catalog-poweredge-r760.jpg' 'https://i.dell.com/is/image/DellContent/content/dam/ss2/product-images/dell-enterprise-products/enterprise-systems/poweredge/r760/spi/enterprise-poweredge-server-r760-black-hero-504x350-ng.psd?fmt=pjpg&hei=402&op_usm=1.75%2C0.3%2C2%2C0&pscan=auto&qlt=90&resMode=sharp'
    thinkcentre = Get-Image 'catalog-thinkcentre.png' 'https://p4-ofp.static.pub/fes/cms/2024/05/10/cij6bdh1w60hbmonr6mlqzb637hd5f219978.png'
    lenovoServer = Get-Image 'catalog-thinksystem.png' 'https://serverproven.lenovo.com/img/servers/sr650-v3-sp5/sr650-v3-sp5_soc.png'
    dellR550 = Get-Image 'catalog-poweledge-r550.webp' 'https://image.made-in-china.com/2f0j00NZDkQyhKQicw/Brand-New-Poweredge-R550-2u-Dual-Socket-Server-with-Maximum-24-Cores.webp'
    switch = Get-Image 'catalog-switch.jpg' 'https://cdn.panacompu.com/cdn-img/pv/cisco-c9200l-24t-4g-e-switch-isometric-front-view.jpg?fixedwidthheight=true&height=630&width=1200'
    firewall = Get-Image 'catalog-firewall.jpg' 'https://cdn11.bigcommerce.com/s-2kqswvsy80/images/stencil/2560w/products/42618/2181201/1061263908__27544.1667521744.jpg?c=2'
    monitor = Get-Image 'catalog-monitor.png' 'https://psrefstuff.lenovo.com/syspool/Sys/Image/ThinkVision/ThinkVision_P24h_30/ThinkVision_P24h_30_CT1_02.png'
    hpMonitor = Get-Image 'catalog-hp-monitor.jpg' 'https://epc.com.ua/image/cache/import_files/e8/e85bfae4-d173-4f41-8797-bd75862ccd5d_f2cecf39-ae2f-4bfb-93e9-79d4481c7ab5-1000x1000.jpeg'
    printer = Get-Image 'catalog-printer.jpg' 'https://publicapi.solotodo.com/products/228424/picture_file/?height=630&width=1200'
    multiPrinter = Get-Image 'catalog-multifunction-printer.webp' 'https://www.brother.com.kh/-/media/ap2/products/printer/mfc-l5710dw_white/mfc-l5710dw_w_f.webp?rev=5a9a67d5951d42a2bfa3bba27f8a612f'
    phone = Get-Image 'catalog-phone.webp' 'https://www.pulse.ie/web/image/product.product/54/image_1024/Yealink%20T46U?unique=c14e2c7'
    phoneT54 = Get-Image 'catalog-phone-t54w.jpg' 'https://www.voipsupply.com/media/catalog/category/T54W-Front.jpg'
    ups = Get-Image 'catalog-ups.jpg' 'https://hanoicomputercdn.com/media/lib/24-09-2024/bo-luu-dien-ups-eaton-5px1500irt2ug2-1500va-1350w_0004_layer2.jpg'
    ap = Get-Image 'catalog-ap.webp' 'https://www.incehesap.com/resim/urun/202512/69357067ccf21.webp'
}
$accessoryPhoto = Join-Path $media 'santa-accessory-bag.jpg'
$accessoryPhotos = @{
    keyboard = Get-Image 'accessory-keyboard-set.png' 'https://resource.logitech.com/w_544%2Ch_466%2Car_7%3A6%2Cc_pad%2Cq_auto%2Cf_auto%2Cdpr_1.0/d_transparent.gif/content/dam/logitech/en/products/combos/mk540-mouse-keyboard-combo/gallery/mk540-deu-gallery1.png'
    hub = Get-Image 'accessory-hub.jpg' 'https://static.tp-link.com/upload/image-line/UH7020C_overview_4_large_20250205103808h.jpg'
    dock = Get-Image 'accessory-dock.jpg' 'https://www.tpstech.in/cdn/shop/products/Dell_Docking_Station_WD19S_From_TPSTech_03.jpg?v=1664369943&width=1445'
    bag = $accessoryPhoto
    cat6 = Get-Image 'accessory-cat6.jpg' 'https://media.adeo.com/mkp/b2a1e25952434b8a072d9342eeba6f9c/media.jpg?fit=bounds&format=jpg&height=650&quality=80&width=650'
    hdmi = Get-Image 'accessory-hdmi.jpg' 'https://www.cdiscount.com/pdt2/4/5/3/1/700x700/auc3003314817453/rw/cable-hdmi-male-2m-pour-tv-philips-console-gold-3d.jpg'
    displayport = Get-Image 'accessory-displayport.jpg' 'https://www.pc-portatil.com/134413-superlarge_default/cable-displayport-para-monitor-macho-macho.jpg'
    mouse = Get-Image 'accessory-mouse.png' 'https://www.yowd.io/cdn/shop/files/1-M185WirelesMouse-min.png?v=1704974828'
    adapter = Get-Image 'accessory-adapter.png' 'https://p2-ofp.static.pub/ShareResource/560x450/SSNP/4X20M26268/4X20M26268-560x450-02.61f5a0696c22551d.png'
}
$brandDomains = @{ Lenovo='lenovo.com'; Dell='dell.com'; HP='hp.com'; Cisco='cisco.com'; Fortinet='fortinet.com'; Samsung='samsung.com'; Brother='brother.com'; Yealink='yealink.com'; Eaton='eaton.com'; Ubiquiti='ui.com'; Logitech='logitech.com'; Samsonite='samsonite.com' }
$supplierDomains = @{ 'Penta Teknoloji'='penta.com.tr'; 'Arena Bilgisayar'='arena.com.tr'; 'Vatan Bilgisayar'='vatanbilgisayar.com'; Teknosa='teknosa.com'; Hepsiburada='hepsiburada.com'; İncehesap='incehesap.com'; 'Amazon Türkiye'='amazon.com.tr' }

foreach ($line in (Invoke-Db 'SELECT e."Id"::text,e."EmployeeNumber" FROM "Employees" e JOIN "Companies" c ON c."Id"=e."CompanyId" WHERE c."Slug"=''santa-demo'' ORDER BY e."EmployeeNumber";')) {
    $parts = $line -split '\|'; $number = [int]($parts[1] -replace '\D',''); $gender = if ($number -le 15) { 'men' } else { 'women' }; $portrait = ($number - 1) % 15 + 1
    $photo = Get-Image "person-$($number.ToString('00')).jpg" "https://randomuser.me/api/portraits/$gender/$portrait.jpg"
    Copy-CompanyImage $photo "employees/$($parts[0].Replace('-','').ToLowerInvariant())" 'profile.jpg'
}

foreach ($line in (Invoke-Db 'SELECT m."Id"::text,coalesce(c."Name",''''),m."Name",coalesce(b."Name",'''') FROM "AssetModels" m JOIN "Companies" t ON t."Id"=m."CompanyId" LEFT JOIN "AssetCategories" c ON c."Id"=m."CategoryId" AND c."CompanyId"=t."Id" LEFT JOIN "AssetBrands" b ON b."Id"=m."BrandId" AND b."CompanyId"=t."Id" WHERE t."Slug"=''santa-demo'' ORDER BY m."Name";')) {
    $parts = $line -split '\|',4; $id = $parts[0]; $category = $parts[1].ToLowerInvariant(); $modelName=$parts[2]; $brand=$parts[3]
    $key = if ($category.Contains('dizüstü')) {'laptop'} elseif ($category.Contains('masaüstü')) {'desktop'} elseif ($category.Contains('sunucu')) {'server'} elseif ($category.Contains('anahtar')) {'switch'} elseif ($category.Contains('duvar')) {'firewall'} elseif ($category.Contains('monitör')) {'monitor'} elseif ($category.Contains('yazıcı')) {'printer'} elseif ($category.Contains('telefon')) {'phone'} elseif ($category.Contains('güç')) {'ups'} elseif ($category.Contains('erişim')) {'ap'} else {'server'}
    if ($modelName -match 'Latitude') { $key='latitude' } elseif ($modelName -match 'EliteBook|Elitebook') { $key='elitebook' } elseif ($modelName -match 'ProDesk') { $key='prodesk' } elseif ($modelName -match '^E24') { $key='hpMonitor' } elseif ($modelName -match 'ThinkSystem') { $key='lenovoServer' } elseif ($modelName -match 'R760') { $key='dellR760' } elseif ($modelName -match 'R550') { $key='dellR550' } elseif ($modelName -match 'ThinkCentre') { $key='thinkcentre' } elseif ($modelName -match 'MFC-L5710') { $key='multiPrinter' } elseif ($modelName -match 'T54W') { $key='phoneT54' }
    $photo = $products[$key]; $filename = [IO.Path]::GetFileName($photo); $kind='models'
    $encodedId=$id.ToLowerInvariant(); $sql="UPDATE `"AssetModels`" SET `"PhotoFileName`"='$filename' WHERE `"Id`"='$id' AND `"CompanyId`"='$company';"
    Invoke-Db $sql | Out-Null
    Copy-CompanyImage $photo "catalog/$kind/$($encodedId.Replace('-',''))" $filename
}

foreach ($line in (Invoke-Db 'SELECT a."Id"::text,coalesce(b."Name",''''),a."Name" FROM "AccessoryModels" a JOIN "Companies" c ON c."Id"=a."CompanyId" LEFT JOIN "AssetBrands" b ON b."Id"=a."BrandId" AND b."CompanyId"=a."CompanyId" WHERE c."Slug"=''santa-demo'' ORDER BY a."Name";')) {
    $parts=$line -split '\|',3; $id=$parts[0]; $brand=$parts[1]; $name=$parts[2].ToLowerInvariant(); $key=if($name.Contains('mk540')){'keyboard'}elseif($name.Contains('hub')){'hub'}elseif($name.Contains('dock')){'dock'}elseif($name.Contains('guardit')){'bag'}elseif($name.Contains('cat6')){'cat6'}elseif($name.Contains('hdmi')){'hdmi'}elseif($name.Contains('displayport')){'displayport'}elseif($name.Contains('m185')){'mouse'}elseif($name.Contains('65 w')){'adapter'}else{'bag'}; $photo=$accessoryPhotos[$key]
    $filename=[IO.Path]::GetFileName($photo); $idPath=$id.Replace('-','').ToLowerInvariant()
    Invoke-Db "UPDATE `"AccessoryModels`" SET `"PhotoFileName`"='$filename' WHERE `"Id`"='$id' AND `"CompanyId`"='$company';" | Out-Null
    Copy-CompanyImage $photo "catalog/accessory-models/$idPath" $filename
}

foreach ($line in (Invoke-Db 'SELECT b."Id"::text,b."Name" FROM "AssetBrands" b JOIN "Companies" c ON c."Id"=b."CompanyId" WHERE c."Slug"=''santa-demo'' ORDER BY b."Name";')) {
    $parts=$line -split '\|',2; $id=$parts[0]; $name=$parts[1]; $domain=$brandDomains[$name]
    if (-not $domain) { continue }; $logo=Get-Image "brand-$($name.ToLowerInvariant()).png" "https://www.google.com/s2/favicons?domain=$domain&sz=128"; $idPath=$id.Replace('-','').ToLowerInvariant()
    Invoke-Db "UPDATE `"AssetBrands`" SET `"LogoFileName`"='$(Split-Path -Leaf $logo)' WHERE `"Id`"='$id' AND `"CompanyId`"='$company' AND `"LogoFileName`" IS NULL;" | Out-Null
    Copy-CompanyImage $logo "catalog/brands/$idPath" (Split-Path -Leaf $logo)
}

foreach ($line in (Invoke-Db 'SELECT s."Id"::text,s."Name" FROM "SupplierCatalogEntries" s JOIN "Companies" c ON c."Id"=s."CompanyId" WHERE c."Slug"=''santa-demo'' ORDER BY s."Name";')) {
    $parts=$line -split '\|',2; $id=$parts[0]; $name=$parts[1]; $domain=$supplierDomains[$name]
    if (-not $domain) { continue }; $logo=Get-Image "supplier-$($id.Substring(0,8)).png" "https://www.google.com/s2/favicons?domain=$domain&sz=128"; $idPath=$id.Replace('-','').ToLowerInvariant()
    Invoke-Db "UPDATE `"SupplierCatalogEntries`" SET `"LogoFileName`"='$(Split-Path -Leaf $logo)' WHERE `"Id`"='$id' AND `"CompanyId`"='$company' AND `"LogoFileName`" IS NULL;" | Out-Null
    Copy-CompanyImage $logo "catalog/suppliers/$idPath" (Split-Path -Leaf $logo)
}

Invoke-DockerChecked @('exec','--user','0',$webContainer,'mkdir','-p',"$mediaRoot/$companyPath") | Out-Null
Invoke-DockerChecked @('cp',(Join-Path $stageRoot '.'),"${webContainer}:$mediaRoot/") | Out-Null
Invoke-DockerChecked @('exec','--user','0',$webContainer,'chown','-R','app:app',"$mediaRoot/$companyPath") | Out-Null
Write-Output 'Santa demo çalışan profilleri, model/ürün görselleri ve bilinen üretici/tedarikçi logoları kuruldu.'
