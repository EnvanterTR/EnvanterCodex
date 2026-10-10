# Ubuntu Server kurulum kitapçığı

> **Sürüm durumu:** Müşteriye yayımlanmış son kurulum kitapçığı değildir. Mevcut yönlendirmeli kurulum betiğinin gereksinimlerini açıklar. Gerçek müşteri Ubuntu sunucusunda ilk kurulum, yerel ağ erişimi, genel HTTPS, yedek ve geri yükleme kabulü ile sürümlü indirme paketi tamamlanmadan indirme bağlantısı olarak sunulmamalıdır.

## Hangi seçeneği kuruyorsunuz?

Bu yol **Ubuntu Server üzerinde ekip kullanımı** içindir. EnvanterTR uygulaması ve PostgreSQL Docker Compose ile aynı sunucuda çalışır; Caddy web tarayıcıları için HTTPS sağlar. Ekip üyeleri Windows/macOS/Linux bilgisayarlarından ve mobil cihazlardan tarayıcıyla bağlanır. Her çalışanın bilgisayarına uygulama kurulmaz.

Windows Server yolundan farkı, PostgreSQL'in ve web uygulamasının Docker bileşenleri olarak yönetilmesi ve güncelleme/yedek adımlarının Linux komutları ile systemd görevlerinden geçmesidir. Ekip üyelerinin gördüğü ürün ve rol erişimi aynıdır.

## Kurulumdan önce hazır olması gerekenler

- Ubuntu Server 24.04 LTS, x86-64 (`amd64`), systemd ve yönetici `sudo` erişimi.
- Sunucuya yönlenen genel DNS adı; örneğin `envanter.sirketiniz.com`. Sunucunun dış IP'si değişmiyorsa DNS A kaydı bu IP'ye yönlenmelidir. Kullanılmayan/yanlış IPv6 AAAA kaydı bırakmayın.
- İnternetten sunucuya TCP 80 ve 443 ile UDP 443 erişimi. Bulut sağlayıcı güvenlik duvarı, modem/NAT ve yerel güvenlik duvarı ayrı ayrı kontrol edilmelidir; kurulum betiği hiçbir güvenlik duvarı kuralını değiştirmez.
- Docker Hub ve .NET imajlarına ilk kurulum sırasında internet erişimi.
- Yedekler için yeterli disk alanı ve sunucu kaybına karşı ayrı/çevrim dışı yedek parolası saklama yöntemi.

> Caddy'nin otomatik TLS sertifikası alabilmesi için genel DNS ve internetten gerekli port erişimi gerekir. Bu kurulum profili yalnızca kapalı LAN'da çalışan, genel DNS/HTTPS'siz senaryoyu müşteri sürümü olarak desteklemiş sayılmaz. Docker'ın yayımladığı portlar UFW/firewalld kurallarını aşabildiğinden dış erişimi kurum veya sağlayıcı ağ duvarından ayrıca sınırlayın.

## Kurulum

1. EnvanterTR Ubuntu x64 kurulum arşivini indirin ve sağlayıcının yayın notundaki SHA-256 ile doğrulayın. Şimdiki geliştirme paketleri özel GitHub deposunda geçici Actions artifact'idir; herkese açık, kalıcı bir müşteri indirme adresi değildir.
2. Arşivi sunucuya aktarın ve boş bir çalışma klasörüne çıkarın. Kaynak ağacında `deploy/server/install-guided.sh` bulunduğunu doğrulayın.
3. Alan adınızın sunucunun genel IP'sine çözüldüğünü doğrulayın. Sunucuda şu kontrol bilgi verir:

   ```bash
   getent ahosts envanter.sirketiniz.com
   ```

4. Kaynak ağacının içinden yönlendirmeli yardımcıyı çalıştırın:

   ```bash
   sudo bash deploy/server/install-guided.sh envanter.sirketiniz.com
   ```

5. Yardımcı Ubuntu sürümünü/mimarisini, DNS'i ve mevcut EnvanterTR kurulumunu denetler. Başka Ubuntu sürümü veya mimari görürse durun; zorla devam ettirmeyin.
6. Docker Engine ve Compose v2 yoksa yardımcı, mevcut çakışan paketleri kaldırmadan durur. Docker'ın resmi Ubuntu deposunu ekleyip resmi paketleri kurmasına izin vermek için sorulduğunda `e` yazın. Var olan Docker deposu/anahtarı uyuşmuyorsa üzerine yazmadan durur; kurum IT sorumlusuna danışın.
7. DNS ve dış ağ portları hazır sorusuna, sağlayıcı ve ağ duvarı kontrolleri tamamlandıysa `e` yazın. Betik Docker servislerini başlatabilir; sistem güvenlik duvarını kendisi değiştirmez.
8. Kurulum sırasında PostgreSQL, şirket dosyaları ve Data Protection anahtarları için kalıcı Docker volume'ları hazırlanır. Var olan EnvanterTR kurulumu tespit edilirse yeni kurulum yapmayın; güncelleme/kurtarma akışını kullanın.
9. Sonunda `https://envanter.sirketiniz.com/api/status` adresinin `ready` olduğunu doğrulayın. İlk şirket sahibi hesabını `https://envanter.sirketiniz.com/setup.html` sayfasından oluşturun. Şirket anahtarını sunucuda şu komutla alın, yalnızca parola kasasına yazın ve sonrasında terminal oturumunu kapatın:

   ```bash
   sudo awk -F= '$1=="SETUP_BOOTSTRAP_TOKEN" {print $2}' /etc/envantertr/server.env
   ```

   Platform yöneticisi kurulumu gerekiyorsa platform anahtarını da aynı şekilde alın:

   ```bash
   sudo awk -F= '$1=="SETUP_PLATFORM_BOOTSTRAP_TOKEN" {print $2}' /etc/envantertr/server.env
   ```

10. Gerekli ilk hesaplar açıldıktan sonra `/etc/envantertr/server.env` dosyasını `sudoedit` ile açıp iki `SETUP_*_TOKEN` satırını kaldırın. Sonra web ve proxy servislerini yeniden oluşturun:

   ```bash
   sudo docker compose --project-name envantertr-server \
     --env-file /etc/envantertr/server.env \
     -f /opt/envantertr/current/deploy/server/compose.yaml up -d web proxy
   ```

   Bu dosya root erişimli sır dosyasıdır; içeriğini kimseye göndermeyin.
11. Aynı Wi-Fi/LAN'daki başka bir bilgisayar, tablet ve telefondan HTTPS adresini açıp sertifika, giriş, rol erişimi ve varlık listesini doğrulayın.

## E-posta ve yedek

- SMTP sağlayıcınız varsa değerleri yalnızca root erişimli `/etc/envantertr/server.env` dosyasına girin; dosyayı paylaşmayın. Önce test alıcısına parola kurtarma ve davet e-postasını doğrulayın.
- Kurulum günlük şifreli yedek ve ayrı yedek sağlık `systemd` zamanlayıcılarını etkinleştirir. İlk çalıştırmayı ve `ready` sağlık sonucunu kontrol edin:

  ```bash
  sudo systemctl start envantertr-backup.service
  sudo systemctl status envantertr-backup.service
  sudo systemctl start envantertr-backup-health.service
  sudo systemctl status envantertr-backup-health.service
  ```

- Şifreli arşiv parolası `/etc/envantertr/backup.pass` dosyasındadır. Dosyanın ayrı, çevrim dışı bir kopyasını saklayın; sunucu ve parola dosyası aynı anda kaybolursa yedek açılamaz.
- Yedek veritabanı, medya ve Data Protection anahtarlarını kapsar. Sunucu kaybı planı için `docs/DISASTER-RECOVERY.md` içindeki kurtarma malzemelerini de ayrı yerde saklayın.
- SFTP uzak kopyası isteğe bağlıdır; sunucuya SSH anahtarı ve `known_hosts` sabitlemesi hazırlanmalıdır. Gerçek kurum yedek hedefine aktarım ayrıca doğrulanmalıdır.

## Güncelleme ve hata halinde

Yeni sürümün doğrulanmış kaynak arşivini sunucuya aktarın ve o sürümün içinden `update.sh` akışını kullanın. Betik şema değişikliğinden önce şifreli yedek ve izole geri yükleme provası yapar. `docker compose down --volumes` komutunu çalıştırmayın; bu komut kalıcı verileri silebilir.

Kurulum `ready` durumuna ulaşmazsa aynı alan adıyla `install-guided.sh` komutunu tekrar deneyin; yarım ilk kurulum güvenli biçimde devam eder. DNS/port hatasında sağlayıcı ve kurum ağ duvarını kontrol edin. Parola, token, `server.env`, `backup.pass`, SSH özel anahtarı veya yedek arşivini destek için göndermeyin. Yalnızca UTC zamanı, komutun sansürlenmiş hata özeti, `systemctl status` sonucu ve sır içermeyen günlük satırlarını paylaşın.

## Müşteri sürümüne açılmadan önce gereken kabul

Bu kitapçık hedef sunucuda temiz Ubuntu 24.04 kurulumu, genel TLS ve LAN erişimi, ekip hesabı/rol oluşturma, SMTP, günlük yedek/sağlık görevi, dolu medya içeren arşivin ayrı hostta geri yüklenmesi ve güncelleme/kurtarma adımlarıyla gerçek ortamda doğrulanınca yayımlanabilir. Sürümlü, checksum'ı sabitlenmiş müşteri paketi ve bu host kabulü şu an tamamlanmış sayılmaz.
