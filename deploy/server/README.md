# Ubuntu sunucu kurulumu (Compose)

Bu profil `systemd` kullanan Ubuntu/Linux ana bilgisayarda Docker Compose ile EnvanterTR'yi üretim yapılandırmasında çalıştırır. Uygulama ve PostgreSQL konteynerleri dış porta bağlanmaz; Caddy 80/443 üzerinden HTTPS sağlar. PostgreSQL, şirket medyası, oturum anahtarları ve Caddy sertifika durumu sabit adlı kalıcı volume'larda tutulur. **`docker compose down --volumes` çalıştırmayın.**

Sunucu veya disk kaybı için platformlar arası çevrim dışı kurtarma malzemeleri ve adım adım prosedür: [`docs/DISASTER-RECOVERY.md`](../../docs/DISASTER-RECOVERY.md).

Bu, Linux sunucu kurulumunun ilk sürümüdür; imzalı, sürümlü müşteri kurulum paketi değildir. Sunucuya aktarılan kaynak ağacı ve Docker imajları kullanılmaktadır. İmzalı yayın kanalı, Windows Server paketi ve LAN için özel PKI/HTTPS akışı hâlâ ayrı teslimatlardır.

## CI sunucu paketi

Linux güvenlik/kabul işi, doğruladığı commit için kaynak ağacını sürümlü bir `.tar.gz` dosyası ve SHA-256 yan dosyası olarak GitHub Actions artifact'ine ekler. Artifact adı `envantertr-linux-server-<commit>` biçimindedir ve 14 gün saklanır. Paketi sunucuya aktarmadan önce checksum'ı doğrulayın:

```bash
sha256sum -c envantertr-linux-server-<commit>.tar.gz.sha256
```

Arşivi açıp `deploy/server/install.sh` adımlarını izleyin. Bu paket kaynak ve Compose tabanlıdır; Linux self-contained binary veya dijital imzalı kurulum paketi değildir. Müşteri kullanıma hazır imzalı sürüm ve gerçek Ubuntu sunucu kabulü hâlâ gereklidir.

## Gereksinimler

- Ubuntu/Linux sunucu, systemd, Docker Engine ve Docker Compose v2 eklentisi.
- Sunucuya yönlenen gerçek bir DNS adı. Caddy otomatik TLS sertifikası için alan adının genel DNS'te çözülmesi ve internetten TCP 80/443 erişimi gerekir. Güvenlik duvarını kurulum betiği değiştirmez; TCP 80/443 ve UDP 443'ü ağ politikanıza göre açın.
- İlk kurulum sırasında Docker Hub ve .NET imaj depolarına erişim.

## İlk kurulum

İlk kurulum Docker, migration, TLS/HTTPS veya sağlık kontrolünde kesilirse yapılandırma, ilk sürüm ve oluşturulmuş volume'lar korunur. Hata nedenini düzelttikten sonra aynı kaynak ağacından aynı alan adıyla `sudo bash deploy/server/install.sh envanter.sirketiniz.com` komutunu yeniden çalıştırın. Betik yalnızca root sahipli `chmod 600` yapılandırmayı, onun ilk sürümünü ve yedek parolasını doğrulayınca aynı kimlik bilgileriyle devam eder; ilk şirket/platform kurulum anahtarlarını veya PostgreSQL parolasını yenilemez. Farklı alan adı ya da değiştirilmiş/eksik sırlarla devam etmez. Başarılı kurulumdan sonra `current` sürüm bağlantısı oluştuğunda yeni kurulum yerine `update.sh` kullanılır.

Kaynak arşivini sunucuya aktarın ve komutu kaynak ağacının içinden çalıştırın:

```bash
sudo bash deploy/server/install.sh envanter.sirketiniz.com
```

Kurulum betiği root-only `/etc/envantertr/server.env` yapılandırması ile 256 bit rastgele PostgreSQL, ilk şirket sahibi, platform yöneticisi ve yedek parola sırları üretir; kaynak kopyasını `/opt/envantertr/releases/` altına yerleştirir; PostgreSQL'i başlatır; migration'ları açıkça uygular; uygulama ve Caddy'yi açar; HTTPS üzerinden `/api/status` yanıtının `ready` olmasını bekler. Uygulama konteyneri dış porta yayımlanmadığından Caddy tek HTTP girişidir; bu profilde tek bir forwarded hop güvenilir kabul edilir ve giriş deneme sınırı Caddy'nin aktardığı istemci IP'sine göre ayrılır. Kurulum veritabanı veya medya volume'larını silmez.

İlk şirket sahibi hesabını `https://envanter.sirketiniz.com/setup.html` üzerinden oluşturun. Kurulum betiğinin gösterdiği `awk` komutuyla yalnızca gerekli şirket anahtarını okuyup güvenli biçimde kullanın. İlk şirket/platform hesapları kurulduktan sonra bootstrap anahtarlarını yapılandırmadan kaldırın; ardından web hizmetini yeniden oluşturun:

```bash
sudoedit /etc/envantertr/server.env
sudo docker compose --project-name envantertr-server \
  --env-file /etc/envantertr/server.env \
  -f /opt/envantertr/current/deploy/server/compose.yaml up -d web proxy
```

### SMTP e-posta gönderimi

Parola yenileme ve şirket üyesi davet e-postaları için SMTP ayarlarını root-only `/etc/envantertr/server.env` dosyasında yapılandırın. İlk kurulum `EMAIL_PUBLIC_BASE_URL` alan adını otomatik yazar; SMTP sağlayıcınızın verdiği değerleri ekleyip web hizmetini yeniden oluşturun:

```dotenv
EMAIL_SMTP_HOST=smtp.saglayiciniz.example
EMAIL_SMTP_PORT=587
EMAIL_SMTP_ENABLE_SSL=true
EMAIL_SMTP_USERNAME=uygulama-kullanici-adi
EMAIL_SMTP_PASSWORD=uygulama-parolasi
EMAIL_FROM_ADDRESS=envanter@sirketiniz.com
EMAIL_FROM_NAME=EnvanterTR
EMAIL_PUBLIC_BASE_URL=https://envanter.sirketiniz.com
```

`EMAIL_SMTP_ENABLE_SSL=true` STARTTLS'i etkinleştirir; `false` yalnızca SMTP sağlayıcınız şifrelemesiz bağlantı gerektiriyorsa kullanılmalıdır. Değerleri `sudoedit` ile girin, dosyayı paylaşmayın ve `sudo docker compose ... up -d web` ile uygulamayı yeniden başlatın. Ayarlar eksikse parola kurtarma ve davet e-postaları gönderilemez; kullanıcıya hesap varlığını açıklamayan genel yanıt korunur ve operatör günlüğüne yapılandırma/teslim hatası yazılır. Üretime geçmeden önce gerçek alıcıya gönderim, spam/teslim durumu ve HTTPS bağlantı hedefini test edin. Zamanlanmış envanter hatırlatmaları henüz bu SMTP akışına bağlı değildir.

İlk kurulum her gün şifreli yedek alan systemd zamanlayıcısını ve ondan bağımsız yedek sağlık denetimi zamanlayıcısını kurar. Sağlık denetimi günlük olarak `BACKUP_ROOT` içindeki en yeni yerel `.etbackup` arşivinin 36 saatten eski olmadığını ve yanındaki SHA-256 dosyasıyla eşleştiğini kontrol eder. Başarılı/başarısız sonuç systemd journal'a yazılır. Şifreli yedeklerin açılması için gereken parola `/etc/envantertr/backup.pass` dosyasındadır; bu dosyanın kopyasını sunucunun dışında, çevrim dışı ve ayrı erişim denetimli bir yerde saklayın. Sunucu ve yedek birlikte kaybolursa parola olmadan `.etbackup` açılamaz.

## Şifreli yedek ve güncelleme

Elle yedek almak için:

```bash
sudo systemctl start envantertr-backup.service
sudo systemctl status envantertr-backup.service
sudo journalctl -u envantertr-backup.service
sudo systemctl start envantertr-backup-health.service
sudo systemctl status envantertr-backup-health.timer envantertr-backup-health.service
sudo journalctl -u envantertr-backup-health.service
```

`envantertr-backup-health.service` yalnızca yerel yedek klasörünü denetler; günlük SFTP gönderim adımındaki uzak kopya doğrulaması ayrıca çalışır. Bu sağlık kontrolü başarısız yedek veya 36 saati aşan yedek bulursa non-zero sonuç üretip journal'a nedenini yazar. Yedek işi veya sağlık kontrolü başarısız olduğunda systemd isteğe bağlı bir HTTPS JSON webhook'una bildirim gönderebilir. Kurulumda `BACKUP_ALERT_WEBHOOK_URL` boş gelir; hedefi root-only `/etc/envantertr/server.env` içine yazın. URL sır niteliğinde ele alınmalı ve günlüklere basılmaz. Adres tanımlanmazsa hata journal'da kalır. Bu webhook e-posta/SMS teslimatı değildir; gerçek bildirim hedefinde uçtan uca kabul ayrıca yapılmalıdır. Sağlık kontrolü uzaktaki eski arşivleri taramaz.

Webhook bildirimi için sunucuda `sudoedit /etc/envantertr/server.env` ile `BACKUP_ALERT_WEBHOOK_URL=https://...` satırını ayarlayın. Kurulum bu değişkeni ve `envantertr-backup-alert@.service` birimini hazırlar; güncelleme betiği de systemd birimlerini sürüm değişiminde yeniler. `sudo systemctl start envantertr-backup-health.service` ile sağlık kontrolünü elle başlatabilir, ardından `sudo journalctl -u 'envantertr-backup-alert@*'` ile bildirim sonucunu inceleyebilirsiniz.

Yedek betiği veritabanı dökümünü, şirket medya volume'unu (profil ve envanter görselleri dahil) ve ASP.NET Data Protection anahtar volume'unu geçici klasöre alır; SHA-256 manifesti ekler; AES-256-CBC/HMAC-SHA256 ile `.etbackup` oluşturup doğrular; parolayla yeniden açarak izole PostgreSQL/medya/anahtar kurtarma önizlemesini çalıştırır. Başarıda şifreli arşiv ve SHA-256 özeti bırakılır. Saklama süresi `BACKUP_RETENTION_DAYS` ile yönetilir.

Sunucudan ayrı SFTP kopyası isteğe bağlıdır. Dört `BACKUP_SFTP_*` ayarının tümünü `/etc/envantertr/server.env` içinde tanımlayın: hedef `user@host`, önceden oluşturulmuş ve yalnızca EnvanterTR yedeklerine ayrılmış uzak mutlak dizin, root sahipli `chmod 600` özel anahtar ve root sahipli, grup/diğer kullanıcıların yazamadığı, sunucu anahtarını önceden sabitleyen `known_hosts` dosyası. SFTP anahtarını çevrim dışı/başka sunucuda üretin; parolayı veya özel anahtarı bu depoya eklemeyin. Yedek tamamlanıp yerel kurtarma provası geçtikten sonra arşiv ve `.sha256` dosyası SFTP'ye geçici adla aktarılır, son ada çevrilir, geri indirilir ve byte-byte karşılaştırılır. SHA-256 yan dosyası arşiv adını göreli tutar; kurtarma makinesinde `sha256sum -c yedek.etbackup.sha256` ile doğrulanabilir. Uzak sunucuya erişim veya doğrulama başarısız olursa işlem hata verir, yerel yedek korunur ve yerel saklama temizliği atlanır. Uzak dizin listelenebiliyorsa varsayılan 90 günden eski, katı EnvanterTR ad biçimine uyan arşivler yeni kopya doğrulamasından sonra SFTP `rm` ile temizlenir; `BACKUP_SFTP_RETENTION_DAYS=0` temizliği kapatır. Uzak hedef için günlük bağımsız eski-arşiv taraması yoktur; son kopyanın sağlık doğrulaması her yedek çalışmasında yapılır. Gerçek SFTP hedefiyle uçtan uca kabul ayrıca yapılmalıdır.

Sunucu tamamen kaybolduysa aynı SFTP ayarlarını kurtarma sunucusundaki `/etc/envantertr/server.env` dosyasına ve yedek parola dosyasına güvenli biçimde sağlayın. Yedek adını uzak dizin listesinden alıp SHA-256, şifre ve izole kurtarma provasından geçirerek indirin:

```bash
sudo bash /opt/envantertr/current/deploy/server/fetch-sftp-backup.sh \
  envantertr-server-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup
```

Komut doğrulamadan önce yedeği canlı sisteme taşımaz veya var olan yerel dosyanın üzerine yazmaz. Başarılı indirmeden sonra normal `restore.sh` akışı için yeni sunucu kurulmuş ve `ready` durumda olmalıdır.

Yeni kaynak sürümünü sunucuya aktarın ve `update.sh`'yi o ağacın içinden çalıştırın:

```bash
sudo bash /yeni/kaynak/deploy/server/update.sh /yeni/kaynak
```

Güncelleme önce uygulamayı durdurur, şifreli yedek ve izole kurtarma önizlemesi tamamlanmadan veritabanı şemasına dokunmaz, yeni imajı ayrı sürüm etiketiyle derler, migration'ları açıkça uygular ve HTTPS `ready` yanıtını bekler. Başarıda atomik `current` bağlantısını yeni sürüme geçirir; eski kaynak sürümü ile veritabanı/medya volume'ları korunur. Başarısız migration sonrasında eski binary'nin şemayla uyumu doğrulanmadığı için güncelleme betiği otomatik olarak çalıştırmaz; oluşturulan `.etbackup` ve günlükler saklanır. Bir yedeği canlı sisteme geçirmek için aşağıdaki `restore.sh` ayrı işlemidir.

### Canlı geri yükleme

`restore.sh` önce verilen `.etbackup` parolasını doğrular ve PostgreSQL/medya/anahtar içeriğini izole kurtarma provasından geçirir. Sonra `RESTORE <alan-adı>` onayı alır, uygulamayı durdurur ve mevcut veriyi şifreli güvenlik yedeğine alır. Geri yüklemeyi **yeni ve ayrı** Docker volume'larına hazırlar, veritabanını canlı Compose veritabanına bağlamadan sorgular ve yalnızca doğrulama başarılıysa yapılandırmayı yeni volume adlarına geçirir. Migration sonrası HTTPS sağlık kontrolü geçmezse önceki yapılandırma/volume'lar yeniden bağlanır; yeni volume'lar inceleme için saklanır.

```bash
sudo bash /opt/envantertr/current/deploy/server/restore.sh /var/backups/envantertr/envantertr-server-YYYYMMDD-HHMMSS-XXXXXXXX.etbackup
```

Başarılı işlem eski veritabanı, medya ve anahtar volume'larını silmez. Eski volume'ları kaldırmadan önce uygulamayı ve geri yüklenen kayıtları kontrol edin ve ayrı yedek kopyasını doğrulayın. Bu akış Ubuntu sunucusunda henüz uçtan uca çalıştırılmadı; müşteri kurulumu öncesinde yalıtılmış deneme sunucusunda doğrulanmalıdır. Başarısız uygulama güncellemesinde önceki uygulama sürümüne geçiş ve migration'ı eski şemaya döndürme ayrıca desteklenmiyor.

Yedek parola dosyasını çevrim dışı doğrulama/açma ve salt okunur kurtarma önizlemesi için mevcut araçlar kullanılabilir. Canlı geri yüklemede daima yukarıdaki `restore.sh` akışını kullanın; üretim veritabanına elle `pg_restore` uygulamayın.

## Kontrol ve günlükler

```bash
sudo docker compose --project-name envantertr-server \
  --env-file /etc/envantertr/server.env \
  -f /opt/envantertr/current/deploy/server/compose.yaml ps
curl -fsS https://envanter.sirketiniz.com/api/status
sudo docker compose --project-name envantertr-server \
  --env-file /etc/envantertr/server.env \
  -f /opt/envantertr/current/deploy/server/compose.yaml logs --tail 200 web proxy database
```

`database` servisi dış porta yayımlanmaz. Uygulama ve DB yalnızca Compose iç ağında, HTTPS proxy'si ayrıca dışa erişebilen ağda çalışır. HSTS/firewall/anahtar rotasyonu, harici izleme, uzak yedek deposu ve gerçek üretim kurtarma tatbikatı ayrıca tamamlanmalıdır.
