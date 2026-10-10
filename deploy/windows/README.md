# Windows dağıtım paketi

Bu klasör Windows x64 self-contained yayın, etkileşimli ilk kurulum yardımcısı, servis kurma ve uygulama güncelleme betiklerini içerir. İndirilen ZIP açıldıktan sonra `Setup.cmd` başlatılır; yükseltilmiş bir PowerShell penceresi açarak adımları yönlendirir. Yardımcı mevcut bir veritabanı/servis kurulumunun üzerine yazmaz. Gerçek müşteri Windows Server kabulü tamamlanmadan paket üretime hazır sayılmaz.

Sunucu veya disk kaybı için çevrim dışı parola/anahtar saklama ve iki platformlu kurtarma adımları: [`docs/DISASTER-RECOVERY.md`](../../docs/DISASTER-RECOVERY.md).

## Gereksinimler ve sınırlar

- Windows Server 2019 veya 2022 x64, yönetici PowerShell'i ve paket derlemesi için .NET 10 SDK'sı ya da çalışan Docker Desktop (resmî .NET 10 SDK imajıyla) bulunan derleme makinesi.
- `Setup.cmd` kurulum akışı Windows Server içindir. Tek bilgisayarda Windows 11 üzerinde kişisel kurulum ayrı ürün paketi olarak hazırlanmalıdır; bu paket Windows 11'i desteklediğini iddia etmez. Kişisel PC, ofis Windows Server'ı ve ofis Ubuntu sunucusu üç ayrı müşteri kullanım biçimidir; sunucu seçeneğinde çalışanların bilgisayarına istemci kurulmaz.
- PostgreSQL 17 ayrıca kurulmuş ve çalışıyor olmalı. `prepare-postgresql.ps1` yalnızca yerel PostgreSQL'e bağlanarak yeni, sınırlı yetkili uygulama rolü ve UTF-8 veritabanı hazırlar; PostgreSQL yazılımını kurmaz. `migrate.ps1` açık onaydan sonra uygulama şemasını oluşturur/günceller.
- TLS sertifikası `Local Computer > Personal (My)` deposunda olmalı; betik tam subject eşleşmesini ve özel anahtarı doğrular. `LocalService` hesabına sertifikanın özel anahtarını okuma izni Windows Sertifikalar MMC'sinden verilmelidir.
- Şirket görselleri, Data Protection anahtarları ve DPAPI ile LocalMachine kapsamında şifrelenmiş PostgreSQL bağlantı sırrı `C:\ProgramData\EnvanterTR` altında tutulur. Production ayar dosyasında yalnızca şifreli sır dosyasının yolu bulunur; sır klasöründe SYSTEM ve Administrators tam yetkili, LocalService salt okunur erişimlidir. Uygulama ve bakım betikleri sırrı yalnız aynı Windows makinesinde açabilir; dosya başka makineye taşınırsa açılamaz. Zamanlanmış yedek parolası ve isteğe bağlı HTTPS bildirim adresi de DPAPI ile makineye bağlı şifrelenir. Çevrim dışı parola kasası/kurtarma kaydı zorunludur.
- Kurulum Windows Güvenlik Duvarı'nı değiştirmez. HTTPS 8443 varsayılandır; kurumun ağ ve sertifika politikasına göre erişimi ayrıca ayarlayın.

## Yayın ve ilk servis kurulumu

Derleme makinesinde:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
./deploy/windows/publish.ps1
```

Betik önce yerel .NET SDK'sını kullanır; SDK yoksa çalışan Docker Desktop üzerinden resmî .NET 10 SDK imajını çağırır. İlk Docker derlemesi imajı indirebilir. Özel çıktı dizini için `-OutputPath` verilebilir.

`main` dalındaki GitHub Actions Windows doğrulama işi başarılı tamamlanırsa, aynı işin **Artifacts** bölümünde `envantertr-windows-x64-<commit-sha>` adlı indirme 14 gün boyunca saklanır. GitHub'da **Actions → EnvanterTR CI → başarılı çalıştırma → Artifacts** yolunu izleyin. İndirme içinde `envantertr-windows-x64-<12-karakter-commit>.zip`, `.zip.sha256` yan dosyası ve `verify-release.ps1` doğrulama aracı bulunur. Bu imzasız x64 pakette `Setup.cmd` ilk kurulum yardımcısı vardır; başarılı CI veya yerel paket testi gerçek Windows Server kabulü anlamına gelmez.

Arşivi açmadan önce SHA-256 değerini doğrulayın:

```powershell
$archive = Get-ChildItem .\envantertr-windows-x64-*.zip | Select-Object -First 1
if (-not $archive) { throw "Windows yayın ZIP dosyası bulunamadı." }
./verify-release.ps1 -ArchivePath $archive.FullName
Expand-Archive -LiteralPath $archive.FullName -DestinationPath C:\Staging\publish
```

SHA-256 aktarım bozulmasını saptar; dosyanın yayıncı kimliğini kanıtlamaz. Paketi sunucuya aktarmadan önce artifact'i doğru GitHub çalıştırmasından indirdiğinizi ve çalıştırmanın beklediğiniz commit'e ait olduğunu doğrulayın. Üretim paketi kod imzalama ve hedef sunucu kabulünden sonra ayrıca yayımlanmalıdır.

`artifacts/windows/publish` dizinini sunucuya güvenli biçimde aktarın. Önkoşulları (PostgreSQL 17, TLS sertifikası ve DNS) tamamladıktan sonra paketi ayrı bir klasöre çıkarıp `Setup.cmd` dosyasına çift tıklayın. Yardımcı yönetici izni ister; DNS adını, HTTPS/DB portlarını ve kullanılacak TLS sertifikasını sorar. Ön kontroller geçtikten sonra devam etmek için `INSTALL` yazılır. `prepare-postgresql.ps1` uygulama parolasını üretir ve kurulum sırasında gizli biçimde aktarır; ekrana bağlantı parolası yazmaz. Veritabanı şeması için ayrıca `MIGRATE` onayı gerekir.

Sihirbaz PostgreSQL yazılımını kurmaz, DNS veya Windows Güvenlik Duvarı'nı değiştirmez ve TLS sertifikası edinmez. Kurum IT'si bu hazırlıkları yapmalıdır. Önkoşul hatalarında sihirbaz açıklama verir; düzeltme sonrası yeniden çalıştırılabilir. Yeni DB/rol oluşturulduktan sonra servis veya migration adımı başarısız olursa, bağlantı sırrı C:\ProgramData\EnvanterTR\secrets altında LocalMachine DPAPI ile korunur; aynı ayarlar ve sertifika ile yeniden çalıştırıldığında ilk kurulum kaldığı adımdan sürer. Başarısız secret kaydında yeni oluşturulan boş DB/rol geri alınmaya çalışılır. Mevcut veritabanı ve servis üzerine yazılmaz. Kurulum sırasında oluşturulan ilk şirket ve platform yöneticisi anahtarları bir kez gösterilir; güvenli kasaya kaydedin, ilk hesaplar açıldıktan sonra `disable-bootstrap.ps1` çalıştırın.

Elle denetim veya sorun giderme için ön kontrolü doğrudan yönetici PowerShell'den çalıştırın:

```powershell
./preflight.ps1 -PostgreSqlHost 127.0.0.1 -DnsName envanter.sirketiniz.com -CertificateSubject "CN=envanter.sirketiniz.com"
```

Ön kontrol yalnızca okuma yapar: işletim sistemi/sürüm, yönetici oturumu, paket, hedef yollar, boş disk, HTTPS portu, PostgreSQL TCP erişimi ve LocalMachine sertifikasının geçerliliğini/SAN alan adını inceler. Başarısız maddelerin yanında düzeltme önerisi verir. PostgreSQL sürümünü ayrıca doğrulayın. Sertifika özel anahtarına `LocalService` erişimi, DNS ve kurum güvenlik duvarı elle onaylanır; bu kontrol bunları değiştirmez.

Ön kontroldeki FAIL maddelerini giderdikten sonra aynı yönetici PowerShell oturumunda:

```powershell
./install.ps1 -PackagePath C:\Staging\publish
```

PostgreSQL'e önceden EnvanterTR rolü/veritabanı açılmadıysa paketteki `prepare-postgresql.ps1` betiğini yönetici oturumunda çalıştırın. Betik yalnızca `127.0.0.1` üzerindeki PostgreSQL 17'ye bağlanır, administrator parolasını gizli ister, mevcut adlara dokunmaz ve yalnız yeni bir veritabanı/rol oluşturur. Ürettiği bağlantı dizesini doğrudan `install.ps1` içindeki gizli bağlantı dizesi istemine yapıştırın; betik bunu DataRoot altında DPAPI ile şifreler ve ayar dosyasına düz metin yazmaz. Rol oluşturulup veritabanı oluşturulamazsa yeni rolü kaldırmayı dener. PostgreSQL kurulumunu, uzak veritabanı sunucusu veya kurumsal TLS ayarlarını otomatikleştirmez.

Betik HTTPS sertifika subject'ini ve PostgreSQL bağlantı dizesini gizli giriş olarak ister; bağlantı dizesini DPAPI ile şifreleyip SYSTEM/Administrators/LocalService ACL'li DataRoot sır klasörüne yazar, ayar dosyasında yalnızca şifreli sır yolunu saklar; servisi `NT AUTHORITY\LocalService` hesabıyla otomatik başlatılacak şekilde kaydeder. İlk kurulum sırasında HTTPS `/api/status` uç noktasını iki dakikaya kadar yoklar ve API'nin PostgreSQL'e erişebildiğini doğrulamadan bootstrap anahtarlarını göstermez. Servis başlatma veya bağlantı doğrulaması başarısız olursa oluşturduğu servisi ve uygulama dosyalarını geri alır; `DataRoot`'u silmez. İlk şirket ve platform bootstrap anahtarlarını yalnız başarılı kurulumdan sonra güvenli kasada saklayın. Yeni ve boş veritabanı için:

```powershell
./migrate.ps1 -InitialSetup
```

`InstallRoot` EnvanterTR'ye ayrılmış olmalı; `app` dışında dosya içermemelidir. `InstallRoot` ile `DataRoot` birbiri içine giremez veya junction/symlink olamaz. Bu koşullar kurulumun başka ürün dosyalarının ACL'lerini değiştirmesini ve hata rollback'inin başka bir dizine taşmasını önler.

### SMTP e-posta ayarları

Parola kurtarma ve şirket davet e-postaları için yönetici PowerShell oturumunda `./configure-email.ps1` komutunu çalıştırın. SMTP sunucusu, port, STARTTLS, hesap adı, gönderen adresi ve HTTPS site adresi sorulur; parola gizli giriş olarak alınır. Parola `DataRoot\secrets` altında makineye bağlı DPAPI ile korunur; `appsettings.Production.json` yalnızca sır dosyasının yolunu tutar. Ayarlar kaydedilirken çalışan servis durdurulur ve tekrar `/api/status` `ready` olana kadar doğrulanır. Başlatma/sağlık kontrolü başarısızsa eski ayar dosyası geri alınır. Güncelleme betiği bu ayarları mevcut üretim yapılandırmasından devralır. Gerçek e-posta teslimatını üretim adresiyle ayrıca doğrulayın; operasyon özeti seçilen bakım, denetim, lisans, garanti, beklenen/gecikmiş iade, onaylı rezervasyon, zimmet ve varlık ömrü bildirimlerini günlük gönderim sınırıyla destekler. Gerçek SMTP sağlayıcısında alıcı teslimatı ayrıca doğrulanmalıdır.

Sonra `/setup.html` ile şirket sahibini, `/platform-setup.html` ile platform yöneticisini oluşturun. İki hesap da hazır olduğunda, aynı paketteki `disable-bootstrap.ps1` anahtarları etkin ayar dosyasından kaldırır ve bunu yapmadan önce iki hesap durumunu doğrular. Ardından istemci bilgisayardan `https://sunucu-adı:8443/api/status` adresinin `ready` döndürdüğünü kontrol edin. `ready` değilse güvenlik duvarını açmak yerine servis olay günlüklerini, PostgreSQL bağlantısını, migration sayısını ve sertifika özel anahtar izinlerini inceleyin.

Kurulum rollback ve readiness kontrolünün PowerShell kaynak regresyon denetimi:

```powershell
./tests/install-safety-check.ps1
```

Bu kontrol servis açma, veritabanına bağlanma veya Windows Server kurulumunu çalıştırmaz; gerçek hedef sunucu kabulünün yerini tutmaz.

## Eski kurulumlarda veritabanı sırrını koruma

Önce doğrulanmış bir yedek alın. Eski sürümde bağlantı dizesi `appsettings.Production.json` içinde düz metinse, paketteki yönetici PowerShell komutunu çalıştırın:

```powershell
./protect-connection-string.ps1
```

Komut sırrı aynı makineye bağlı DPAPI LocalMachine ile şifreler, `DataRoot\secrets` ACL'lerini sınırlar, ayar dosyasını atomik değiştirir ve servisi yeniden başlatıp HTTPS `/api/status` yanıtının `ready` olmasını bekler. Sağlık kontrolü başarısız olursa eski ayar dosyasını geri koyar, yeni DPAPI dosyasını kaldırır ve servisi eski ayarla başlatmayı dener. Zaten korumalı kurulumlarda sırrı doğrular ve değişiklik yapmadan çıkar. Parolayı sunucu dışında parola kasasına kaydedin; DPAPI dosyası makine kaybında başka sunucuda açılamaz.

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

PostgreSQL yazılımının otomatik kurulumu, `prepare-postgresql.ps1` ile hazırlanan rol/veritabanı akışının gerçek Windows Server'da kabulü, migration/ilk yönetici sihirbazı, gerçek Windows Server güncelleme-kurtarma ve webhook hedefi kabul testi, imzalı paket/otomatik uygulama güncellemesi ve migration rollback'i henüz tamamlanmadı. SMB saklama temizliği yapılandırılabilir; SFTP hedefi için sunucu tarafı saklama/izleme ayrıca ayarlanmalıdır.
