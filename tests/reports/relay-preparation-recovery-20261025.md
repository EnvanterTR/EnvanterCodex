# SMTP hazırlığı öncesi güncel geri dönüş kapısı — 25 Ekim 2026

Önceki tur ilerleme olarak sınıflandırıldı: gerçek SMTP TLS bağlantısı ve Google Workspace yönetici erişimi doğrulandı; yeni relay yetkisi kaydedilmeden eylem anı onayı istendi. Bu tur onay gelmedi; taslak ve Coolify SMTP ayarları uygulanmadı.

Bugünkü Santa/Demo3 oluşturma, silme, geri alma ve yeniden etkinleştirme işlemleri önceki yayın yedeğinden sonra olduğundan yeni kurtarma noktası alındı.

- Canlı çalışan web/public konteynerleri `20261025T080842`, DB `20261025T080841` healthy; uygulama imajı değiştirilmedi.
- SQL: `/var/lib/postgresql/data/envantertr-backups/pre-web-v1-20261025T145446Z.dump`; pg_dump, pg_restore listesi ve SHA256 kontrolü başarılı.
- 140 MB paket: `/var/backups/envantertr/rollback-20261025T145517Z`; SQL, medya, veri koruma anahtarları, gerçek çalışan imajlar ve özel ortam ayarları. Tüm SHA kontrolleri başarılı, `BUNDLE_EXIT=0`.
- Ayrı dosya/imaj geri dönüş provası: `rollback-20261025T145517Z-drill-20261025T145609Z`; arşiv güvenliği, çıkartma/karşılaştırma ve gerçek imaj yükleme başarılı, `DRILL_EXIT=0`. Canlı konteyner yeniden oluşturulmadı.
- SQL, dış portu olmayan `envtr-stage-db-6198ef6` içinde yeni `envantertr_restorecheck_20261025_1455` DB'sine `--exit-on-error` ile geri yüklendi; `ISOLATED_SQL_RESTORE_OK`.
- Canlı ve restore kopyasının 95 public tablosu için sıralı satır sayımları özel dosyalarda karşılaştırıldı: `TABLE_COUNTS_EQUAL tables=95`, `COUNT_COMPARE_EXIT=0`. Bu içerik/satır alanı eşitliği veya uygulama uçtan uca kabulü iddiası değildir. Ham müşteri kayıtları ve sırlar yayımlanmadı.

Kanıtlar: `relay-preparation-fresh-bundle-20261025.png`, `relay-preparation-recovery-drill-20261025.png`, `relay-preparation-sql-restore-counts-20261025.png`.

SMTP relay onayı, gerçek e-posta teslimatı, iyzico ve tam Bölüm 20 kabulü açık. Bekleme sırasında yeni OTP istenmedi. Sonraki üretim yazımları bu kurtarma noktasından sonra olursa yedek tazeliği yeniden değerlendirilmelidir.

TAMAM dediğim 3 kapının (SQL yedeği, dosya/imaj provası, izole SQL restore/sayım) 3'ü gerçek sunucu komut çıktısıyla doğrulanmıştır.
