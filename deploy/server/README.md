# Ubuntu sunucu kurulumu (Compose)

Bu profil `systemd` kullanan Ubuntu/Linux ana bilgisayarda Docker Compose ile EnvanterTR'yi üretim yapılandırmasında çalıştırır. Uygulama ve PostgreSQL konteynerleri dış porta bağlanmaz; Caddy 80/443 üzerinden HTTPS sağlar. PostgreSQL, şirket medyası, oturum anahtarları ve Caddy sertifika durumu sabit adlı kalıcı volume'larda tutulur. **`docker compose down --volumes` çalıştırmayın.**

Bu, Linux sunucu kurulumunun ilk sürümüdür; imzalı, sürümlü müşteri kurulum paketi değildir. Sunucuya aktarılan kaynak ağacı ve Docker imajları kullanılmaktadır. İmzalı yayın kanalı, Windows Server paketi, LAN için özel PKI/HTTPS akışı ve canlı yedekten geri yükleme aracı hâlâ ayrı teslimatlardır.

## Gereksinimler

- Ubuntu/Linux sunucu, systemd, Docker Engine ve Docker Compose v2 eklentisi.
- Sunucuya yönlenen gerçek bir DNS adı. Caddy otomatik TLS sertifikası için alan adının genel DNS'te çözülmesi ve internetten TCP 80/443 erişimi gerekir. Güvenlik duvarını kurulum betiği değiştirmez; TCP 80/443 ve UDP 443'ü ağ politikanıza göre açın.
- İlk kurulum sırasında Docker Hub ve .NET imaj depolarına erişim.

## İlk kurulum

Kaynak arşivini sunucuya aktarın ve komutu kaynak ağacının içinden çalıştırın:

```bash
sudo bash deploy/server/install.sh envanter.sirketiniz.com
```

Kurulum betiği root-only `/etc/envantertr/server.env` yapılandırması ile 256 bit rastgele PostgreSQL, ilk şirket sahibi, platform yöneticisi ve yedek parola sırları üretir; kaynak kopyasını `/opt/envantertr/releases/` altına yerleştirir; PostgreSQL'i başlatır; migration'ları açıkça uygular; uygulama ve Caddy'yi açar; HTTPS üzerinden `/api/status` yanıtının `ready` olmasını bekler. Kurulum veritabanı veya medya volume'larını silmez.

İlk şirket sahibi hesabını `https://envanter.sirketiniz.com/setup.html` üzerinden oluşturun. Kurulum betiğinin gösterdiği `awk` komutuyla yalnızca gerekli şirket anahtarını okuyup güvenli biçimde kullanın. İlk şirket/platform hesapları kurulduktan sonra bootstrap anahtarlarını yapılandırmadan kaldırın; ardından web hizmetini yeniden oluşturun:

```bash
sudoedit /etc/envantertr/server.env
sudo docker compose --project-name envantertr-server \
  --env-file /etc/envantertr/server.env \
  -f /opt/envantertr/current/deploy/server/compose.yaml up -d web proxy
```

İlk kurulum ayrıca her gün systemd zamanlayıcısı kurar. Şifreli yedeklerin açılması için gereken parola `/etc/envantertr/backup.pass` dosyasındadır; bu dosyanın kopyasını sunucunun dışında, çevrim dışı ve ayrı erişim denetimli bir yerde saklayın. Sunucu ve yedek birlikte kaybolursa parola olmadan `.etbackup` açılamaz.

## Şifreli yedek ve güncelleme

Elle yedek almak için:

```bash
sudo systemctl start envantertr-backup.service
sudo systemctl status envantertr-backup.service
sudo journalctl -u envantertr-backup.service
```

Yedek betiği veritabanı dökümünü, şirket medya volume'unu (profil ve envanter görselleri dahil) ve ASP.NET Data Protection anahtar volume'unu geçici klasöre alır; SHA-256 manifesti ekler; AES-256-CBC/HMAC-SHA256 ile `.etbackup` oluşturup doğrular; parolayla yeniden açarak izole PostgreSQL/medya/anahtar kurtarma önizlemesini çalıştırır. Başarıda yalnızca şifreli arşiv ve SHA-256 özeti bırakılır. Saklama süresi `BACKUP_RETENTION_DAYS` ile yönetilir. Şirketin değişen verileri için yedekler sunucudan ayrı bir konuma kopyalanmalıdır; systemd zamanlayıcısı kendi başına uzak kopya veya arıza alarmı sağlamaz.

Yeni kaynak sürümünü sunucuya aktarın ve `update.sh`'yi o ağacın içinden çalıştırın:

```bash
sudo bash /yeni/kaynak/deploy/server/update.sh /yeni/kaynak
```

Güncelleme önce uygulamayı durdurur, şifreli yedek ve izole kurtarma önizlemesi tamamlanmadan veritabanı şemasına dokunmaz, yeni imajı ayrı sürüm etiketiyle derler, migration'ları açıkça uygular ve HTTPS `ready` yanıtını bekler. Başarıda atomik `current` bağlantısını yeni sürüme geçirir; eski kaynak sürümü ile veritabanı/medya volume'ları korunur. Başarısız güncellemede eski uygulama otomatik başlatılmaz; migration geriye uyumluluğu varsayılmaz. Önce yeni uygulama günlükleri ve oluşturulan `.etbackup` incelenmelidir. Mevcut araç yalıtılmış kurtarılabilirlik önizlemesi sağlar; canlı veritabanına geri yüklemeyi otomatikleştirmez.

Yedek parola dosyasını doğrulama/açma ve kurtarma önizlemesi için mevcut araçlar kullanılabilir. **Canlı Compose veritabanı ve medya volume'una geri yükleme işlemi bu pakette yoktur**; üretim verisini ezebilecek el ile `pg_restore` komutu uygulamayın.

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
