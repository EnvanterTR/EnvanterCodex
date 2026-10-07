# Windows Server dağıtım taslağı

Bu klasör Windows x64 self-contained yayın, servis kurma ve uygulama güncelleme betiklerini içerir. Bu, henüz müşteri kurulum sihirbazı veya onaylanmış üretim dağıtımı değildir. Betikler gerçek Windows Server ortamında uçtan uca doğrulanmadan üretim kullanımı için değerlendirilmemelidir.

## Gereksinimler ve sınırlar

- Windows Server 2019 veya 2022 x64, yönetici PowerShell'i ve Windows için .NET 10 self-contained yayınını üretebilen .NET 10 SDK'lı derleme makinesi.
- PostgreSQL 17 ayrıca kurulmuş, ağ erişimi ayarlanmış, uygulama veritabanı ve rolü hazırlanmış olmalı. Bu betik PostgreSQL kurmaz ve rol/veritabanı oluşturmaz; ayrı `migrate.ps1` ile yalnızca açık onaydan sonra uygulama şeması oluşturulur/güncellenir.
- TLS sertifikası `Local Computer > Personal (My)` deposunda olmalı; betik tam subject eşleşmesini ve özel anahtarı doğrular. `LocalService` hesabına sertifikanın özel anahtarını okuma izni Windows Sertifikalar MMC'sinden verilmelidir.
- Şirket görselleri ve Data Protection anahtarları `C:\ProgramData\EnvanterTR` altında tutulur. Servis hesabı yalnızca bu veri dizininde yazma izni alır. Veritabanı bağlantı dizesi `Program Files` altındaki Production ayar dosyasında bulunur; erişim SYSTEM, Administrators ve LocalService ile sınırlandırılır. Üretim sır yönetimi/DPAPI entegrasyonu sonraki iştir.
- Kurulum Windows Güvenlik Duvarı'nı değiştirmez. HTTPS 8443 varsayılandır; kurumun ağ ve sertifika politikasına göre erişimi ayrıca ayarlayın.

## Yayın ve ilk servis kurulumu

Derleme makinesinde:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
./deploy/windows/publish.ps1
```

`artifacts/windows/publish` dizinini sunucuya güvenli biçimde aktarın. Sunucuda yönetici PowerShell açın:

```powershell
./install.ps1 -PackagePath C:\Staging\publish
```

Betik HTTPS sertifika subject'ini ve PostgreSQL bağlantı dizesini gizli giriş olarak ister; servis ve veri klasörlerini ACL ile sınırlar; servisi `NT AUTHORITY\LocalService` hesabıyla otomatik başlatılacak şekilde kaydeder. İlk şirket ve platform bootstrap anahtarlarını bir defa ekrana verir; bunları güvenli kasada saklayın. Yeni ve boş veritabanı için:

```powershell
./migrate.ps1 -InitialSetup
```

Sonra `/setup.html` ile şirket sahibini, `/platform-setup.html` ile platform yöneticisini oluşturun. İki hesap da hazır olduğunda, aynı paketteki `disable-bootstrap.ps1` anahtarları etkin ayar dosyasından kaldırır ve bunu yapmadan önce iki hesap durumunu doğrular. Ardından istemci bilgisayardan `https://sunucu-adı:8443/api/status` adresinin `ready` döndürdüğünü kontrol edin. `ready` değilse güvenlik duvarını açmak yerine servis olay günlüklerini, PostgreSQL bağlantısını, migration sayısını ve sertifika özel anahtar izinlerini inceleyin.

## Uygulama güncelleme

Önce veritabanı ve medya için doğrulanmış ayrı yedek alın. Yeni yayın klasörünü sunucuya aktarın ve yönetici PowerShell'den çalıştırın. Şema değişmeyen sürümde:

```powershell
./update.ps1 -PackagePath C:\Staging\publish
```

Betik uygulama dosyalarının kopyasını `C:\Program Files\EnvanterTR\rollback-*` altında tutar, üretim ayar dosyasını korur ve `/api/status` `ready` değilse önceki uygulama dosyalarını geri koyar. **Veritabanı migration'ını geri almaz.** Migration gerektiren sürümlerde bu mekanizma tek başına güvenli rollback sağlamaz; migration öncesi yedek ve uyumlu geri yükleme planı zorunludur.

Yeni sürüm migration içeriyorsa, doğrulanmış veritabanı yedeğinin dosya yolunu açıkça verin. Betik bu dosyanın varlığını kontrol eder ve `MIGRATE` yazarak onay ister:

```powershell
./update.ps1 -PackagePath C:\Staging\publish -ApplyMigrations -BackupPath D:\VerifiedBackups\database.dump
```

Betik uygulama servisini durdurur, yeni uygulamadan `--migrate` komutunu çalıştırır, sonra servisi açıp hazır durumunu bekler. Hata olursa eski uygulama dosyalarını geri koyar; **veritabanını yedekten geri yüklemez ve migration'ı geri almaz**. Yedek dosyasının gerçekten doğrulanmış olması operatörün sorumluluğundadır; betik yalnızca dosyanın varlığını kontrol eder.

## Şifreli Windows yedeği

Yönetici PowerShell'de backup.ps1 komutunu çalıştırın.

Betik servisi kısa süre durdurur; pg_dump ile PostgreSQL 17 dökümünü alır; şirket medyası (profil, envanter ve şirket dosyaları) ile Data Protection anahtarlarını aynı arşive ekler. Arşiv PBKDF2-SHA256 (600.000 tur) ile türetilen anahtarlar kullanılarak AES-256-CBC/HMAC-SHA256 ile şifrelenir. Elle çalıştırıldığında parola iki kez gizli istenir ve yedeğe yazılmaz. Arşiv açılarak parola/MAC, manifest dosya sayıları ve SHA-256 değerleri denetlenir; pg_restore önce dump kataloğunu inceler, sonra yeni ve loopback dışına açılmayan geçici PostgreSQL 17 cluster'ında veritabanını gerçekten geri yükler. Migration geçmişi sorgulanır ve geçici cluster işlem sonunda durdurulup silinir. Yedek ve SHA-256 yan dosyası varsayılan olarak ACL ile kısıtlı C:\ProgramData\EnvanterTR\backups altına yazılır.

İşlem geçici şifreli/açılmış arşivler ve yeni PostgreSQL cluster'ı için yeterli ek boş disk alanı ister. Günlük Görev Zamanlayıcı kaydı oluşturmak için yönetici PowerShell'de `configure-backup-schedule.ps1` çalıştırılır; parola DPAPI LocalMachine ile şifrelenip `backup-secrets` altında SYSTEM/Administrators ACL'iyle saklanır, görev SYSTEM olarak çalışır. Varsayılan saat 02:30 ve saklama 30 gündür; `-DailyAt` ile saat, `-RetentionDays` ile 1–3650 gün arası ayarlanabilir. Sistem hesabı şifreli parolayı açar; parola görev komut satırına veya ayar dosyasına yazılmaz. **Parolayı ayrıca parola kasanıza kaydedin:** DPAPI dosyası makineye bağlıdır, yedek arşivine konmaz ve sunucu kaybında başka bilgisayarda açılamaz. Elle alınan yedeklerde `-PassphraseFile` kullanılmaz ve varsayılan saklama temizliği kapalıdır; `-RetentionDays` açıkça verilirse temizleme uygulanır.

Örnek görev kurulumu:

```powershell
./configure-backup-schedule.ps1 -DailyAt "02:30" -RetentionDays 30
```

Yedek üretiminde veritabanı dökümü, arşiv ve izole restore geçici dizini SYSTEM/Administrators dışına kapatılır. Zamanlanmış yedeğin ilk çalıştırmasını Task Scheduler'dan elle başlatın ve `Get-ScheduledTaskInfo` sonucunu kontrol edin.

Bu, canlı sistemden bağımsız yerel bir restore provasıdır; gerçek Windows Server'da henüz çalıştırılmadı ve ayrı bir kurtarma sunucusunda sınanmamış durumda. Yedek sunucu içinde kalır; sunucu kaybına karşı başka bir makineye/çevrim dışı ortama ve parolaya ayrı erişim denetimiyle kopyalayın. Ayrı offline doğrulama ve canlı restore aracı henüz teslim edilmedi; bu betik tek başına üretim yedekleme politikası sayılmaz. Görev sonucunu `Get-ScheduledTaskInfo -TaskName 'EnvanterTR Daily Encrypted Backup'` ile görüntüleyin; ilk çalıştırmada Task Scheduler'dan elle başlatıp izole restore provası tamamlandığını kontrol edin.
## Henüz teslim edilmemiş

PostgreSQL otomatik kurulumu ve güvenli rol/veritabanı hazırlığı, migration/ilk yönetici sihirbazı, offsite saklama/uyarılar, Windows canlı restore, gerçek Windows Server kurulumu-güncelleme-kurtarma kabul testi, imzalı paket/otomatik uygulama güncellemesi ve migration rollback'i bu ilk taslakta yoktur.
