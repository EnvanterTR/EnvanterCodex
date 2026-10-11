# Yerel hesap parolası kurtarma

Parola kurtarma e-postası SMTP ayarı gerektirir. Sunucuya erişimi olan işletmeci, e-posta gönderimi çalışmıyorsa `EnvanterTR.AccountRecovery` aracını sunucu konsolunda kullanabilir. Araç web API'si açmaz ve parola değerini komut satırından, dosyadan veya ortam değişkeninden almaz.

## Güvenlik davranışı

- Araç yalnızca etkileşimli terminalde çalışır, hedef e-postayı ister ve değişiklikten önce aynı adresin tekrar yazılmasını gerektirir.
- Yalnızca e-postası doğrulanmış ve en az bir şirket veya platform erişimi olan mevcut hesabı kurtarır.
- 28 karakterli kriptografik rastgele geçici parola üretir; Identity parola kurallarını uygular.
- Parolayı ekrana yalnızca başarılı işlemden sonra bir kez yazar. Ekranı kapatmadan önce güvenli parola yöneticisine kaydedin.
- Başarısız giriş sayacını ve kilidi temizler, güvenlik damgasını yeniler ve şirket/platform denetim kaydı oluşturur. 2FA ayarını değiştirmez.
- Veritabanı güncellemeleri tek transaction içindedir; hata olursa geri alınır. Hata çıktısında bağlantı dizesi veya parola gösterilmez.
- Araç çalıştırmak için uygulama sunucusuna işletim sistemi düzeyinde erişim ve veritabanı bağlantısı gerekir. Bu yetkiye sahip kişilere karşı uygulama içi kimlik doğrulaması sağlamaz.

## Yerel Docker geliştirme kurulumu (Windows PowerShell)

1. EnvanterTR depo köküne geçin. Aşağıdaki komutlar projeyi derler ve çıktıyı geçici Windows klasörüne alır:

```powershell
$repo = (Get-Location).Path
$publish = Join-Path $env:TEMP ('envantertr-account-recovery-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $publish | Out-Null
docker run --rm `
  --mount "type=bind,source=$repo,target=/workspace" `
  --mount "type=bind,source=$publish,target=/out" `
  --workdir /workspace `
  mcr.microsoft.com/dotnet/sdk:10.0 `
  dotnet publish tools/EnvanterTR.AccountRecovery/EnvanterTR.AccountRecovery.csproj -c Release -o /out
```

2. Derlenen aracı çalışan web konteynerine kopyalayıp etkileşimli konsoldan başlatın. Konteyner kendi mevcut veritabanı bağlantı ayarını kullanır:

```powershell
$container = 'envantertr-product-dev-web-1'
docker exec $container mkdir -p /tmp/envantertr-account-recovery
docker cp "$publish\." "${container}:/tmp/envantertr-account-recovery"
docker exec -it $container dotnet /tmp/envantertr-account-recovery/EnvanterTR.AccountRecovery.dll reset-password
```

Ekranda üretilen geçici parolayla `http://localhost:8080/` adresine giriş yapın. Ardından **Hesap ayarları → Parolayı değiştir** bölümünden kendi parolanızı belirleyin. Kurtarma çıktısını kaydettikten sonra konteynerdeki geçici kopyayı silin:

```powershell
docker exec $container rm -rf /tmp/envantertr-account-recovery
Remove-Item -LiteralPath $publish -Recurse -Force
```

Üretim sunucusunda aracı aynı .NET 10 SDK ile `dotnet publish` edin ve .NET 10 runtime yüklü sunucuda, uygulamanın veritabanı yapılandırmasını okuyabildiği çalışma dizininden etkileşimli terminalde çalıştırın. Sunucuya erişim yetkisi olmayan kullanıcıya bu aracı veya çıktısını vermeyin.
