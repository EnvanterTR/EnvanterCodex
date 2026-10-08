# Yerel geliştirme kurulumu (Windows + Docker Desktop)

Bu paket geliştirici önizlemesi içindir; müşteriye dağıtılacak son Windows kurulum programı değildir. Uygulama `localhost` ile sınırlıdır. PostgreSQL portu Windows ana makinesine yayımlanmaz. Şirket veritabanı, medya dosyaları ve oturum şifreleme anahtarları ayrı Docker adlandırılmış volume'larında kalır; konteyneri durdurmak volume'ları silmez.

## İlk çalıştırma

PowerShell'i proje klasöründe açın:

```powershell
Set-Location .\deploy\local
if (Test-Path .env) { throw '.env zaten var; parolaları yenilemeyin. Var olan veritabanı bu parolaya bağlıdır.' }
function New-Secret { [guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N') }
@(
  "POSTGRES_PASSWORD=$(New-Secret)"
  "SETUP_BOOTSTRAP_TOKEN=$(New-Secret)"
  "SETUP_PLATFORM_BOOTSTRAP_TOKEN=$(New-Secret)"
  'APP_PORT=8080'
) | Set-Content -LiteralPath .env -Encoding ascii
docker compose up -d database
docker compose run --rm web --migrate
docker compose up -d web public-site
```

İlk çalıştırmada Docker, uygulama ve PostgreSQL imajlarını internetten indirir. Ardından:

- Şirket alanı: <http://localhost:8080/>
- Pazarlama ana sayfası önizlemesi: <http://localhost:8081/>
- Pazarlama sayfasındaki müşteri girişi: <http://localhost:8081/giris> (web uygulamasına aynı yerel adres üzerinden geçer)
- İlk şirket sahibi kurulumu: <http://localhost:8080/setup.html>
- İlk platform sahibi kurulumu: <http://localhost:8080/platform-setup.html>
- Sağlık durumu: <http://localhost:8080/api/status>

Kurulum formundaki ilk şirket anahtarı `SETUP_BOOTSTRAP_TOKEN` değeridir. Yerel kurulumda panoya almak için:

```powershell
(Get-Content .env | Where-Object { $_.StartsWith('SETUP_BOOTSTRAP_TOKEN=') }).Substring('SETUP_BOOTSTRAP_TOKEN='.Length) | Set-Clipboard
```

Formda şirket, yönetici adı, e-posta ve yönetici parolası belirlenir. Aynı e-posta daha önce platform hesabı olarak açılmışsa, mevcut parolayla bu şirkete sahip yetkisi bağlanır; parola veya profil değiştirilmez. Anahtar tarayıcıya kaydedilmez. Bu geliştirme önizlemesi henüz TLS sağlamaz; yalnızca bu bilgisayardan erişilir. Ana makineyi şirket ağına açmak için HTTPS, Windows güvenlik duvarı, TLS sertifikası, yedekleme ve kurulum sihirbazı ayrıca tamamlanmalıdır.

Platform sahibinin kurulum anahtarı `SETUP_PLATFORM_BOOTSTRAP_TOKEN` değeridir. Platform kurulumu önce veya şirket kurulumundan sonra yapılabilir. Aynı e-posta kullanılırsa mevcut hesap parolasıyla var olan hesaba platform yetkisi eklenir; yeni bir ikinci hesap oluşturulmaz. Kurulumdan sonra `.env` içindeki iki kurulum anahtarını kaldırıp web servisini yeniden oluşturun; uygulama bu anahtarlar olmadan da çalışır. Parolalar/anahtarlar kullanıcıya veya dış servise gönderilmez.

## Başlatma, durdurma ve veri saklama

```powershell
docker compose up -d
docker compose ps
docker compose stop
docker compose start
```

Compose projesinin adı `envantertr-product-dev` olarak ayrılmıştır; eski test kurulumlarıyla aynı Docker projesini paylaşmaz. `stop`, `start` veya `docker compose down` veri volume'larını silmez. **`docker compose down --volumes` çalıştırmayın**: bu yerel geliştirme veritabanını, şirket medyasını ve oturum anahtarlarını kalıcı olarak siler. Uygulama güncellemesinde kaynak kodun değişmesi PostgreSQL volume'unu ezmez. Yeni migration uygulanmadan önce ayrıca yedek alınmalıdır; migration komutu şemayı değiştirir. Oturum anahtarları konteyner yeniden oluşturulduğunda korunur; volume'lar silinirse mevcut oturum çerezleri geçersiz olur.

`deploy/local/.env` parolaları ve kurulum anahtarlarını içerir; paylaşmayın ve Git'e eklemeyin. `.env.example` yalnızca şablondur.

## Elle yerel yedek alma

Uygulama ve veritabanı çalışırken proje kökünden Windows'ta `pwsh -File deploy/local/backup.ps1`, Linux'ta `bash deploy/local/backup.sh` çalıştırın. İsterseniz ilk argümanla yedeklerin yazılacağı dizini belirtebilirsiniz. Betikler PostgreSQL özel biçimli dökümünü, şirket medya volume'unu ve iki içerik için SHA-256 özetlerini ayrı bir klasöre yazar; dökümün PostgreSQL aracıyla, medya arşivinin tar ile okunabildiğini kontrol eder ve geçici konteyner dosyalarını işlem sonunda kaldırır. Medya arşivi, şirket logoları, varlık ekleri, çalışan profil fotoğrafları ve yönetici/İK erişimine açık çalışan PDF/görsel belgeleri ile oturum hesabı profil fotoğrafları dahil medya volume'undaki dosyaların tamamını içerir; dosya metaverileri ve şirket kayıtları PostgreSQL dökümündedir. Yalnızca manifesto ve özetleri tamamlanan yedek klasörü başarılı sayılır; hata durumunda betik kendi oluşturduğu kısmi klasörü temizlemeyi dener. Yedek klasörü Git'e eklenmez.

Kopyalandıktan veya taşındıktan sonra bir paketi yeniden doğrulamak için Windows'ta `pwsh -File deploy/local/verify-backup.ps1 "D:\Yedekler\envantertr-20261005-120000-ab12cd34"` ya da Linux'ta `bash deploy/local/verify-backup.sh /mnt/backups/envantertr-20261005-120000-ab12cd34` çalıştırın. Doğrulayıcı manifesti ve SHA-256 özetlerini denetler, medya arşivini açmadan tar içerik dizinini okur ve PostgreSQL döküm kataloğunu doğrular. Docker ve `postgres:17-alpine` imajı gerekir; eksik imaj indirilip yalnızca salt okunur dosya bağlamasıyla, ağ bağlantısı kapalı bir geçici konteynerde kullanılır. Veritabanını değiştirmez ve geri yükleme yapmaz.

### İzole kurtarma önizlemesi

Bir yedeğin PostgreSQL'e gerçekten geri yüklenebildiğini ve medya arşivinin açılabildiğini kontrol etmek için Windows'ta `pwsh -File deploy/local/restore-preview.ps1 "D:\Yedekler\envantertr-20261005-120000-ab12cd34"`, Linux'ta `bash deploy/local/restore-preview.sh /mnt/backups/envantertr-20261005-120000-ab12cd34` çalıştırın. Önce manifest ve SHA-256 denetimleri yapılır; ardından PostgreSQL 17 yalıtılmış, ağa bağlanmayan, port yayımlamayan ve ana makineden volume bağlamayan geçici konteynere yüklenir. Medya arşivi aynı konteynerde geçici dizine açılır; migration satırları ve dosya sayısı raporlanır. Komut sonunda konteyner zorla kaldırılır. Etkin Compose veritabanına/volume'una dokunmaz; canlı veritabanına geri yükleme aracı değildir. Yalnızca açılmış yedek klasörünü kabul eder; `.etbackup` önce `decrypt` komutuyla yeni bir klasöre çıkarılmalıdır.

Yedek betiğinin klasör çıktısı başlangıçta **şifrelenmemiştir**; parola hash'leri ve şirket verileri veritabanı dökümündedir. PowerShell betiği oluşturduğu klasörde erişimi geçerli kullanıcı, Windows SYSTEM ve yöneticilerle sınırlar; Bash betiği özel dosya izinleriyle oluşturur. Veritabanı ve medya ayrı adımlarda kopyalandığından işlem sırasında yazma yapılmayan sakin bir zamanda alın. Aşağıdaki akış bu klasörü şifreleyebilir ve yerel geliştirici makinesinde zamanlayabilir. Canlı uygulama veritabanına geri yükleme ve üretim işletimi desteklenmiyor; müşteri hizmetinde tek yedek olarak kullanmayın. Açılmış klasör için yukarıdaki izole kurtarma önizlemesini kullanın; etkin veritabanına komut satırından elle yükleme yapmayın.

### Elle alınan yedeği parola ile şifreleme

`tools/EnvanterTR.Backup` aracı mevcut bir yedek klasörünü AES-256-CBC ile şifreler ve HMAC-SHA256 ile doğrular; parola anahtarı PBKDF2-SHA256 ile 600.000 turda türetilir. Yalnızca tamamlanmış bir yedek klasörünü şifreleyin. Komutları depo kökünde PowerShell veya Bash'ten çalıştırın; Docker ve `mcr.microsoft.com/dotnet/sdk:10.0` imajı gerekir. Parola terminalde görünmeden istenir ve en az 16 karakter olmalıdır:

```powershell
docker run --rm -it -v "${PWD}:/source" -w /source mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj -- encrypt /source/backups/YEDEK-KLASORU /source/backups/YEDEK-KLASORU.etbackup
docker run --rm -it -v "${PWD}:/source" -w /source mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj -- verify /source/backups/YEDEK-KLASORU.etbackup
docker run --rm -it -v "${PWD}:/source" -w /source mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj -- decrypt /source/backups/YEDEK-KLASORU.etbackup /source/restore-preview
```

Bash'te aynı komutlarda `-v "${PWD}:/source"` yerine `-v "$(pwd):/source"` kullanın:

```bash
docker run --rm -it -v "$(pwd):/source" -w /source mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj -- encrypt /source/backups/YEDEK-KLASORU /source/backups/YEDEK-KLASORU.etbackup
docker run --rm -it -v "$(pwd):/source" -w /source mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj -- verify /source/backups/YEDEK-KLASORU.etbackup
docker run --rm -it -v "$(pwd):/source" -w /source mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project tools/EnvanterTR.Backup/EnvanterTR.Backup.csproj -- decrypt /source/backups/YEDEK-KLASORU.etbackup /source/restore-preview
```

`YEDEK-KLASORU` yerine yedek betiğinin oluşturduğu gerçek klasör adını yazın. Doğrulama parola/MAC kontrolü yapar ve içeriğin ZIP olarak okunabildiğini inceler. Açma işlemi doğrulamadan sonra dosyaları **yeni ve henüz var olmayan** hedef klasöre çıkarır; var olan verilerin üstüne yazmaz. Bu, PostgreSQL veritabanına geri yükleme yapmaz. Şifreleme sırasında işletim sisteminin geçici klasöründe kısa süreli bir düz ZIP oluşturulur ve işlem başarıyla tamamlandığında silinir; bu nedenle yalnızca erişimi kısıtlı, güvenilir bir bilgisayarda çalıştırın. Çıkarılan yedek klasörü de şirket verisi içerir; hedef dizinin erişim izinlerini kısıtlayın. `.etbackup` dosyası dışında parolayı da aynı yere koymayın. Bu tekil araç zamanlama veya veritabanı geri yüklemesi sağlamaz; sonraki akış yerel zamanlamayı ayrıca kurar.

### Yerel geliştirme makinesinde günlük şifreli yedek

`run-backup.ps1` ve `run-backup.sh`, mevcut yedek betiğini çalıştırır; ham klasörü doğrular, parolalı arşivi üretip tekrar doğrular ve **ancak bundan sonra** düz yedek klasörünü kaldırır. Windows betiği yedek klasörünü, çalışma günlüğünü ve şifreli arşivi yalnızca geçerli kullanıcı, SYSTEM ve Administrators erişecek şekilde `icacls` ile korur; DACL'yi ayrıca doğrular ve SACL ayrıcalığı istemez. Başarılı `.etbackup` arşivlerini varsayılan olarak 30 günden sonra temizler. Şifreleme veya doğrulama başarısız olursa düz kopyayı kurtarılabilmesi için bırakır; hata bildirimini zamanlayıcı geçmişinden/logundan kontrol edin. Klasör/dosya silme SSD, günlükleme dosya sistemi veya anlık görüntülerde güvenli fiziksel silme garantisi vermez; yedek diski için BitLocker/LUKS gibi tam disk şifrelemesi kullanın.

Önce en az 16 karakterlik parola içeren dosyayı proje dizini ve bulut eşitleme klasörlerinin dışında oluşturun. Windows'ta kurulum betiği bu dosyanın ACL'ini geçerli kullanıcı, SYSTEM ve yöneticilerle sınırlar. Linux'ta parola dosyasının izni `600` yapılır. Parolayı yedekten ayrı, güvenli bir yere de saklayın; kaybolursa şifreli arşiv açılamaz.

Windows PowerShell 7 ve Docker Desktop kullanırken parola dosyasını Docker Desktop'ın bağlayabildiği, proje ve eşitlenen klasörlerin dışındaki bir konumda tutun. `C:\Users\<kullanıcı>\.envantertr-secrets` gibi kullanıcı profili altındaki bir dizin uygundur; bazı Docker Desktop kurulumları `%LOCALAPPDATA%` içindeki dosya bağlamalarını konteynere aktaramaz. Kurulum betiği parola dosyasının ACL'ini geçerli kullanıcı, SYSTEM ve yöneticilerle sınırlar. Depo kökünden günlük 02:00 görevi kurun:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.envantertr-secrets" | Out-Null
# En az 16 karakterlik parola dosyasını editörle bu konuma oluşturun; içeriğini komut geçmişine yazmayın.
pwsh -File deploy/local/configure-backup-task.ps1 -PassphraseFile "$env:USERPROFILE\.envantertr-secrets\backup-passphrase.txt" -At 02:00 -RetentionDays 30
```

Görev kullanıcı oturumu açıkken çalışır ve Docker Desktop'ın hazır olmasını bekler; bu önizlemede Windows oturumu kapalıyken arka plan Docker erişimi garanti edilmez. Elle çalıştırmak için `Start-ScheduledTask -TaskName EnvanterTR-Encrypted-Backup`; kaldırmak için kurulum betiğine `-Uninstall` verin. Ayrıntılı çıktı `backups/.scheduled-logs` altında tutulur; ayrıca Görev Geçmişi ve yeni `.etbackup` dosyasını kontrol edin.

Linux'ta kullanıcı systemd zamanlayıcısını kurun (yolları kendi özel parola dosyanıza ve depolama alanınıza göre değiştirin):

```bash
mkdir -p /home/USER/.config/envantertr
chmod 700 /home/USER/.config/envantertr
chmod 600 /home/USER/.config/envantertr/backup-passphrase
bash deploy/local/configure-backup-timer.sh /home/USER/.config/envantertr/backup-passphrase /home/USER/backups 02:00 30
systemctl --user status envantertr-backup.timer
```

Kullanıcı systemd yöneticisi çalışıyor ve Docker erişimi varsa zamanlayıcı günlük çalışır; oturum yöneticisi kapalıyken çalıştırmak için Linux yönetiminizde ayrıca `systemd --user` linger yapılandırılmalıdır. Elle başlatmak için `systemctl --user start envantertr-backup.service`; sonucu okumak için `journalctl --user -u envantertr-backup.service`; kaldırmak için `bash deploy/local/configure-backup-timer.sh --uninstall` kullanın. Bu akış yerel geliştirme kolaylığıdır; çevrim dışı kopya, başka makinede geri yükleme tatbikatı, yedek alarmı/izleme veya üretim saklama politikası yerine geçmez.
