# Windows Server dağıtım taslağı

Bu klasör Windows x64 self-contained yayın, servis kurma ve uygulama güncelleme betiklerini içerir. Bu, henüz müşteri kurulum sihirbazı veya onaylanmış üretim dağıtımı değildir. Betikler gerçek Windows Server ortamında uçtan uca doğrulanmadan üretim kullanımı için değerlendirilmemelidir.

## Gereksinimler ve sınırlar

- Windows Server 2019 veya 2022 x64, yönetici PowerShell'i ve paket derlemesi için .NET 10 SDK'sı ya da çalışan Docker Desktop (resmî .NET 10 SDK imajıyla) bulunan derleme makinesi.
- PostgreSQL 17 ayrıca kurulmuş, ağ erişimi ayarlanmış, uygulama veritabanı ve rolü hazırlanmış olmalı. Bu betik PostgreSQL kurmaz ve rol/veritabanı oluşturmaz; ayrı `migrate.ps1` ile yalnızca açık onaydan sonra uygulama şeması oluşturulur/güncellenir.
- TLS sertifikası `Local Computer > Personal (My)` deposunda olmalı; betik tam subject eşleşmesini ve özel anahtarı doğrular. `LocalService` hesabına sertifikanın özel anahtarını okuma izni Windows Sertifikalar MMC'sinden verilmelidir.
- Şirket görselleri ve Data Protection anahtarları `C:\ProgramData\EnvanterTR` altında tutulur. Servis hesabı yalnızca gerekli veri dizinlerinde yazma izni alır. Veritabanı bağlantı dizesi `Program Files` altındaki Production ayar dosyasında bulunur; erişim SYSTEM, Administrators ve LocalService ile sınırlandırılır. Zamanlanmış yedek parolası ve isteğe bağlı HTTPS bildirim adresi Windows DPAPI LocalMachine ile makineye bağlı şifrelenir ve yalnız SYSTEM/Administrators ACL'li dizinde saklanır. Bağlantı dizesi için DPAPI tabanlı sır kasası hâlâ sonraki iştir.
- Kurulum Windows Güvenlik Duvarı'nı değiştirmez. HTTPS 8443 varsayılandır; kurumun ağ ve sertifika politikasına göre erişimi ayrıca ayarlayın.

## Yayın ve ilk servis kurulumu

Derleme makinesinde:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
./deploy/windows/publish.ps1
```

Betik önce yerel .NET SDK'sını kullanır; SDK yoksa çalışan Docker Desktop üzerinden resmî .NET 10 SDK imajını çağırır. İlk Docker derlemesi imajı indirebilir. Özel çıktı dizini için `-OutputPath` verilebilir.

`artifacts/windows/publish` dizinini sunucuya güvenli biçimde aktarın. Sunucuda yönetici PowerShell açın:

```powershell
./install.ps1 -PackagePath C:\Staging\publish
```

Betik HTTPS sertifika subject'ini ve PostgreSQL bağlantı dizesini gizli giriş olarak ister; servis ve veri klasörlerini ACL ile sınırlar; servisi `NT AUTHORITY\LocalService` hesabıyla otomatik başlatılacak şekilde kaydeder. İlk kurulum sırasında HTTPS `/api/status` uç noktasını iki dakikaya kadar yoklar ve API'nin PostgreSQL'e erişebildiğini doğrulamadan bootstrap anahtarlarını göstermez. Servis başlatma veya bağlantı doğrulaması başarısız olursa oluşturduğu servisi ve uygulama dosyalarını geri alır; `DataRoot`'u silmez. İlk şirket ve platform bootstrap anahtarlarını yalnız başarılı kurulumdan sonra güvenli kasada saklayın. Yeni ve boş veritabanı için:

`InstallRoot` EnvanterTR'ye ayrılmış olmalı; `app` dışında dosya içermemelidir. `InstallRoot` ile `DataRoot` birbiri içine giremez veya junction/symlink olamaz. Bu koşullar kurulumun başka ürün dosyalarının ACL'lerini değiştirmesini ve hata rollback'inin başka bir dizine taşmasını önler.

```powershell
./migrate.ps1 -InitialSetup
```

Sonra `/setup.html` ile şirket sahibini, `/platform-setup.html` ile platform yöneticisini oluşturun. İki hesap da hazır olduğunda, aynı paketteki `disable-bootstrap.ps1` anahtarları etkin ayar dosyasından kaldırır ve bunu yapmadan önce iki hesap durumunu doğrular. Ardından istemci bilgisayardan `https://sunucu-adı:8443/api/status` adresinin `ready` döndürdüğünü kontrol edin. `ready` değilse güvenlik duvarını açmak yerine servis olay günlüklerini, PostgreSQL bağlantısını, migration sayısını ve sertifika özel anahtar izinlerini inceleyin.

Kurulum rollback ve readiness kontrolünün PowerShell kaynak regresyon denetimi:

```powershell
./tests/install-safety-check.ps1
```

Bu kontrol servis açma, veritabanına bağlanma veya Windows Server kurulumunu çalıştırmaz; gerçek hedef sunucu kabulünün yerini tutmaz.

## Uygulama güncelleme

Önce veritabanı ve medya için doğrulanmış ayrı yedek alın. Yeni yayın klasörünü sunucuya aktarın ve yönetici PowerShell'den çalıştırın. Şema değişmeyen sürümde:

```powershell
./update.ps1 -PackagePath C:\Staging\publish
```

Betik her güncellemeden önce servisin çalıştığını doğrular, uygulama dosyalarına dokunmadan **yeni şifreli tam yedek** alır ve parolayı, HMAC'i, manifest dosya hash'lerini ve PostgreSQL dökümünü geçici PostgreSQL 17 ortamına yükleyerek doğrular. Varsayılan parolayı Görev Zamanlayıcı için DPAPI ile korunan `C:\ProgramData\EnvanterTR\backup-secrets\backup-password.dpapi` dosyasından kullanır; bu dosya yoksa parolayı gizli olarak iki kez ister. Yedek ve SHA-256 yan dosyası `C:\ProgramData\EnvanterTR\backups` altına yazılır. İsteğe bağlı `-BackupOutputDirectory` ve `-BackupPassphraseFile` parametreleri kullanılabilir. Başarılı yedekleme güncelleme bakım kilidi içinde yapılır; başka yedek/geri yükleme/güncellemeyle yarışmaz. Betik, gerçek dosya hash'ini yan dosyayla tekrar eşleştirmeden dosyaları değiştirmez. Servis çalışmıyorsa önce servisi başlatın ve `/api/status` değerinin `ready` olduğunu doğrulayın. Betik uygulama dosyalarının kopyasını `C:\Program Files\EnvanterTR\rollback-*` altında tutar ve üretim ayar dosyasını korur. Migration başlatılmadıysa kopyalama/sağlık kontrolü hatasında önceki uygulama dosyaları geri yüklenir; servis başlangıçtaki çalışma durumuna döndürülür. **Veritabanı migration'ı hiçbir durumda otomatik geri alınmaz.**

Yeni sürüm migration içeriyorsa, betik önce yeni bir doğrulanmış yedek oluşturur; ardından `MIGRATE` yazarak şema değişikliğini onaylatır:

```powershell
./update.ps1 -PackagePath C:\Staging\publish -ApplyMigrations
```

Doğrulanmış yeni yedek ve SHA-256 yan dosyası oluşmadan servis/dosya güncellemesi veya migration başlamaz. Betik uygulama servisini durdurur, uygulama dosyalarının önceki sürüm kopyasını alır, yeni uygulamadan `--migrate` komutunu çalıştırır, sonra servisi açıp hazır durumunu bekler. Migration komutu başlatıldıktan sonra hata oluşursa betik yeni uygulama dosyalarını otomatik olarak geri almaz; veritabanı şeması değişmiş olabilir. Servis durdurulur, eski uygulama dosyaları rollback klasöründe korunur ve hata açıkça raporlanır. Migration öncesi doğrulanmış `.etbackup` yedeğini `restore.ps1` ile geri yükleyip sonra eski uygulamayı çalıştırın. Migration öncesi kopyalama hatasında eski uygulama dosyaları geri yüklenir. Yedek üretimi, HMAC/manifest kontrolü ve geçici PostgreSQL geri yükleme provası update betiği içinde yürütülür.

Güncelleme kurtarma dallarının statik regresyon kontrolü `tests/update-safety-check.ps1` ile yapılabilir:

```powershell
./tests/update-safety-check.ps1
```

Bu kontrol PowerShell sözdizimini ve tehlikeli rollback sıralamasını denetler; gerçek Windows Server servis/migration/sağlık kontrolü kabul testinin yerini tutmaz.

## Şifreli Windows yedeği

Yönetici PowerShell'de backup.ps1 komutunu çalıştırın.

Betik ortak Windows bakım kilidini alır; başka bir yedekleme, geri yükleme veya güncelleme çalışıyorsa başlamaz. Ardından servisi kısa süre durdurur; pg_dump ile PostgreSQL 17 dökümünü alır; şirket medyası (profil, envanter ve şirket dosyaları) ile Data Protection anahtarlarını aynı arşive ekler. Arşiv PBKDF2-SHA256 (600.000 tur) ile türetilen anahtarlar kullanılarak AES-256-CBC/HMAC-SHA256 ile şifrelenir. Elle çalıştırıldığında parola iki kez gizli istenir ve yedeğe yazılmaz. Arşiv açılarak parola/MAC, manifest dosya sayıları ve SHA-256 değerleri denetlenir; pg_restore önce dump kataloğunu inceler, sonra yeni ve loopback dışına açılmayan geçici PostgreSQL 17 cluster'ında veritabanını gerçekten geri yükler. Migration geçmişi sorgulanır ve geçici cluster işlem sonunda durdurulup silinir. Yedek ve SHA-256 yan dosyası varsayılan olarak ACL ile kısıtlı C:\ProgramData\EnvanterTR\backups altına yazılır.

İşlem geçici şifreli/açılmış arşivler ve yeni PostgreSQL cluster'ı için yeterli ek boş disk alanı ister. Günlük Görev Zamanlayıcı kaydı oluşturmak için yönetici PowerShell'de `configure-backup-schedule.ps1` çalıştırılır; parola DPAPI LocalMachine ile şifrelenip `backup-secrets` altında SYSTEM/Administrators ACL'iyle saklanır, görev SYSTEM olarak çalışır. Kurulum sırasında isteğe bağlı HTTPS JSON webhook adresini gizli girişten verebilir veya boş bırakarak bildirimleri kapalı tutabilirsiniz; adres aynı dizinde DPAPI ile şifrelenir, görev argümanlarına ve günlüklere yazılmaz. Varsayılan saat 02:30 ve saklama 30 gündür; `-DailyAt` ile saat, `-RetentionDays` ile 1–3650 gün arası ayarlanabilir. Sistem hesabı şifreli parolayı açar; parola görev komut satırına veya ayar dosyasına yazılmaz. **Parolayı ayrıca parola kasanıza kaydedin:** DPAPI dosyası makineye bağlıdır, yedek arşivine konmaz ve sunucu kaybında başka bilgisayarda açılamaz. Elle alınan yedeklerde `-PassphraseFile` kullanılmaz ve varsayılan saklama temizliği kapalıdır; `-RetentionDays` açıkça verilirse temizleme uygulanır.

Örnek görev kurulumu:

```powershell
./configure-backup-schedule.ps1 -DailyAt "02:30" -RetentionDays 30
```

Bu özellikten önce kurulmuş bir zamanlanmış görev varsa, webhook/sarmalayıcı görev akışına geçmek için güncel paket kurulduktan sonra `configure-backup-schedule.ps1` betiğini bir kez yeniden çalıştırın. Özel `-DailyAt`, `-HealthCheckAt`, `-RetentionDays`, `-MirrorDirectory` veya `-MirrorRetentionDays` değerlerinizi yeniden verin; görevler girilen ayarlarla yenilenir.

Alan dışı bir SMB paylaşımına kopya istenirse `-MirrorDirectory "\\yedek-sunucu\envantertr"` verin. Görev `SYSTEM` olarak çalıştığından Windows ağında etki alanı bilgisayar hesabına bu paylaşımda okuma ve yazma izni verilmelidir; kullanıcı parolası göreve kaydedilmez. Aktarım şifreli `.etbackup` ve SHA-256 dosyasını geçici adla kopyalar, hedefte hash'i yeniden hesaplar ve ancak eşleşirse son ada taşır. Başarılı kopyadan sonra varsayılan olarak 90 günden eski `envantertr-*.etbackup` dosyaları ve eşleşen hash yan dosyaları temizlenir. Süre `-MirrorRetentionDays` ile 0–3650 gün arası ayarlanabilir; 0 uzak temizliği kapatır. Yalnızca bu uygulamanın yedek adı kalıbı işlenir. Gerçek ağ paylaşımıyla uçtan uca kabul henüz yapılmadı.

Yedek zamanlayıcısını kurmak ayrıca günlük `EnvanterTR Daily Backup Health Check` görevini oluşturur (varsayılan 04:00). Bu görev son yerel yedeğin güncelliğini ve SHA-256 yan dosyasını kontrol eder; `-MirrorDirectory` verildiyse SMB kopyasının da erişilebilir, 36 saatten yeni ve hash bakımından doğru olduğunu denetler. Sonuç `C:\ProgramData\EnvanterTR\backup-health.log` dosyasına PASS/FAIL olarak yazılır ve başarısızlık görevde sıfır dışı sonuç verir. Yapılandırılmış webhook zamanlanmış yedek üretimi veya sağlık denetimi başarısızlıklarını HTTPS JSON POST olarak bildirir; teslim durumu `backup-alert.log` dosyasına yazılır, webhook yanıtı ve gizli URL günlüğe alınmaz. E-posta/SMS göndermez. Farklı kontrol saati `-HealthCheckAt "04:00"` ile ayarlanabilir.

Yedek üretiminde veritabanı dökümü, arşiv ve izole restore geçici dizini SYSTEM/Administrators dışına kapatılır. Zamanlanmış yedeğin ilk çalıştırmasını Task Scheduler'dan elle başlatın ve `Get-ScheduledTaskInfo` sonucunu kontrol edin.

Bu, canlı sistemden bağımsız yerel bir restore provasıdır; gerçek Windows Server'da henüz çalıştırılmadı ve ayrı bir kurtarma sunucusunda sınanmamış durumda. Yedek sunucu içinde kalır; sunucu kaybına karşı başka bir makineye/çevrim dışı ortama ve parolaya ayrı erişim denetimiyle kopyalayın. Günlük sağlık kontrolü hash ve tazelik denetimidir; şifre parolası/HMAC ve izole veritabanı geri yükleme kontrolü yedek üretiminde ve `restore.ps1` çalıştırılırken yapılır. Windows canlı geri yükleme betiği aşağıda bulunur; gerçek hedef sunucuda henüz kabul edilmedi. Görev sonucunu `Get-ScheduledTaskInfo -TaskName 'EnvanterTR Daily Encrypted Backup'` ile görüntüleyin; ilk çalıştırmada Task Scheduler'dan elle başlatıp izole restore provasının tamamlandığını kontrol edin.

## Canlı Windows geri yükleme

`restore.ps1` yedeğin parolasını/HMAC doğrulamasını, dosya manifestini ve PostgreSQL döküm kataloğunu canlı verilere dokunmadan denetler; dökümü ayrıca geçici bir hedef veritabanına yükleyip migration geçmişini kontrol eder. Canlı değişiklikten hemen önce mevcut sistem için yeni ve doğrulanmış bir güvenlik yedeği alır. Sonra veritabanı, şirket medyası ve Data Protection anahtarlarını değiştirir, uygulama migration'ını çalıştırır ve HTTPS sağlık kontrolünü bekler. Başarıda eski veritabanı ve medya/anahtar klasörleri kurtarma için korunur; hata halinde önceki adlar ve klasörler geri alınmaya çalışılır.

Her PostgreSQL kurtarma komutu varsayılan olarak 30 dakika ile sınırlıdır. Komut takılırsa süreç ağacı sonlandırılır ve kurtarma, canlı sisteme geçmeden hata verir. Büyük veritabanlarında sınır `-PgToolTimeoutSeconds` parametresiyle artırılabilir (1–86.400 saniye).

Yönetici PowerShell'de, sunucudaki yayın klasöründen çalıştırın:

```powershell
./restore.ps1 -BackupPath C:\ProgramData\EnvanterTR\backups\envantertr-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup
```

Yedek parolası gizli giriş olarak istenir. Aynı makinede hazırlanmış DPAPI parola dosyası istenirse `-PassphraseFile` ile verilebilir; çevrim dışı kurtarmada DPAPI dosyası makineye bağlı olduğundan yedek parolasını ayrıca kullanın. İşlem sonunda canlı sistemin değiştirileceğini onaylamak için `RESTORE <veritabanı-adı>` yazın. Betik mevcut uygulama servisi çalışırken başlatılmalı ve mevcut veritabanını yönetebilen, `CREATEDB` izni olan PostgreSQL rolü kullanmalıdır. İzin/bağlantı/yedek doğrulaması veya geçici restore başarısız olursa canlı veritabanı, medya ve anahtar klasörleri değiştirilmez.

Bu betik henüz gerçek Windows Server/PostgreSQL kurulumunda uçtan uca kabul edilmedi. Üretim kullanımı öncesi ayrı ve yalıtılmış Windows Server'da gerçek yedekle prova yapılmalı; kurtarma klasörleri ve `envtr_previous_*` veritabanı geri yüklenen kayıtlar doğrulanmadan silinmemelidir.
## Henüz teslim edilmemiş

PostgreSQL otomatik kurulumu ve güvenli rol/veritabanı hazırlığı, migration/ilk yönetici sihirbazı, gerçek Windows Server kurulumu-güncelleme-kurtarma ve webhook hedefi kabul testi, imzalı paket/otomatik uygulama güncellemesi ve migration rollback'i henüz tamamlanmadı. SMB saklama temizliği yapılandırılabilir; SFTP hedefi için sunucu tarafı saklama/izleme ayrıca ayarlanmalıdır.
