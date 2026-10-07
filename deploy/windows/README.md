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

Betik servisi kısa süre durdurur; pg_dump ile PostgreSQL 17 dökümünü alır; şirket medyası (profil, envanter ve şirket dosyaları) ile Data Protection anahtarlarını aynı arşive ekler. Arşiv PBKDF2-SHA256 (600.000 tur) ile türetilen anahtarlar kullanılarak AES-256-CBC/HMAC-SHA256 ile şifrelenir. Parola iki kez gizli istenir ve yedeğe yazılmaz. Arşiv açılarak parola/MAC, manifest dosya sayıları ve SHA-256 değerleri denetlenir; ayrıca pg_restore --list döküm kataloğunu okuyabildiğini doğrular. Yedek ve SHA-256 yan dosyası varsayılan olarak ACL ile kısıtlı C:\ProgramData\EnvanterTR\backups altına yazılır.

Bu kontrol canlı geri yükleme veya ayrı bir PostgreSQL'e gerçek restore provası değildir. Yedek sunucu içinde kalır; sunucu kaybına karşı başka bir makineye/çevrim dışı ortama ve parolaya ayrı erişim denetimiyle kopyalayın. Windows için doğrulama/izole geri yükleme ve canlı restore aracı henüz teslim edilmedi; bu betik tek başına üretim yedekleme politikası sayılmaz.
## Henüz teslim edilmemiş

PostgreSQL otomatik kurulumu ve güvenli rol/veritabanı hazırlığı, migration/ilk yönetici sihirbazı, DPAPI veya Windows secret store, özel PKI sertifika yetkilendirmesinin otomasyonu, firewall onay akışı, imzalı paket/otomatik güncelleme, offsite saklama/retention/uyarılar, Windows izole restore provası ve canlı restore, gerçek Windows Server kurulumu-güncelleme-kurtarma kabul testi ve migration rollback'i bu ilk taslakta yoktur.
